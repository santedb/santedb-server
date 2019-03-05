/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.ParameterMap;
using SanteDB.Messaging.HL7.Segments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Utils
{
    /// <summary>
    /// Represents a query parsers utils
    /// </summary>
    public static class MessageUtils
    {

        // Entry ASM HASH
        private static string s_entryAsmHash;
        // Install date
        private static DateTime s_installDate;

        /// <summary>
        /// Add software information
        /// </summary>
        public static void SetDefault(this SFT sftSegment)
        {
            if (Assembly.GetEntryAssembly() == null) return;
            sftSegment.SoftwareVendorOrganization.OrganizationName.Value = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            sftSegment.SoftwareVendorOrganization.OrganizationNameTypeCode.Value = "D";
            sftSegment.SoftwareCertifiedVersionOrReleaseNumber.Value = Assembly.GetEntryAssembly().GetName().Version.ToString();
            sftSegment.SoftwareProductName.Value = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product;

            // SFT info
            if (!String.IsNullOrEmpty(Assembly.GetEntryAssembly().Location) && File.Exists(Assembly.GetEntryAssembly().Location))
            {
                if (s_entryAsmHash == null)
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(Assembly.GetEntryAssembly().Location))
                        s_entryAsmHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                }

                if (s_installDate == null)
                    s_installDate = new FileInfo(Assembly.GetEntryAssembly().Location).CreationTime;

                sftSegment.SoftwareBinaryID.Value = s_entryAsmHash;
                sftSegment.SoftwareInstallDate.Time.SetLongDate(s_installDate);
            }

        }

        /// <summary>
        /// Update the MSH on the specified MSH segment
        /// </summary>
        /// <param name="msh">The message header to be updated</param>
        /// <param name="inbound">The inbound message</param>
        public static void SetDefault(this MSH msh, String receivingApplication, String receivingFacility, String security)
        {
            var config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();
            msh.MessageControlID.Value = Guid.NewGuid().ToString();
            msh.SendingApplication.NamespaceID.Value = ApplicationServiceContext.Current.GetService<INetworkInformationService>()?.GetMachineName();
            msh.SendingFacility.NamespaceID.Value = config.LocalFacility.ToString();
            msh.ReceivingApplication.NamespaceID.Value = receivingApplication;
            msh.ReceivingFacility.NamespaceID.Value = receivingFacility;
            msh.Security.Value = security;
            msh.ProcessingID.ProcessingID.Value = "P";
            msh.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss");

        }

        /// <summary>
        /// Represents a message parser to a bundle
        /// </summary>
        internal static Bundle Parse(IGroup message)
        {
            Bundle retVal = new Bundle();
            var finder = new SegmentFinder(message);
            while (finder.hasNextChild())
            {
                finder.nextChild();
                foreach (var current in finder.CurrentChildReps)
                {
                    if (current is AbstractGroupItem)
                        foreach (var s in (current as AbstractGroupItem)?.Structures.OfType<IGroup>())
                        {
                            var parsed = Parse(s);
                            retVal.Item.AddRange(parsed.Item.Select(i =>
                            {
                                var ret = i.Clone();
                                (ret as ITaggable)?.AddTag(".v2.group", current.GetStructureName());
                                return ret;
                            }));
                            retVal.ExpansionKeys.AddRange(parsed.ExpansionKeys);
                        }
                    else if (current is AbstractSegment)
                    {
                        // Empty, don't parse
                        if (PipeParser.Encode(current as AbstractSegment, new EncodingCharacters('|', "^~\\&")).Length == 3)
                            continue;
                        var handler = SegmentHandlers.GetSegmentHandler(current.GetStructureName());
                        if (handler != null)
                        {
                            var parsed = handler.Parse(current as AbstractSegment, retVal.Item);
                            if (parsed.Any())
                            {
                                retVal.ExpansionKeys.Add(parsed.First().Key.GetValueOrDefault());
                                retVal.Item.AddRange(parsed.Select(i =>
                                {
                                    var ret = i.Clone();
                                    (ret as ITaggable)?.AddTag(".v2.segment", current.GetStructureName());
                                    return ret;
                                }));
                            }
                        }
                    }
                    else if (current is AbstractGroup)
                    {
                        var subObject = Parse(current as AbstractGroup);
                        retVal.Item.AddRange(subObject.Item.Select(i =>
                        {
                            var ret = i.Clone();
                            (ret as ITaggable)?.AddTag(".v2.group", current.GetStructureName());
                            return ret;
                        }));
                        retVal.ExpansionKeys.AddRange(subObject.ExpansionKeys);
                    }

                    // Tag the objects 
                }
            }
            return retVal;

    }

    /// <summary>
    /// Update the MSH on the specified MSH segment
    /// </summary>
    /// <param name="msh">The message header to be updated</param>
    /// <param name="inbound">The inbound message</param>
    public static void SetDefault(this MSH msh, MSH inbound)
        {
            var config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();
            msh.MessageControlID.Value = Guid.NewGuid().ToString();
            msh.SendingApplication.NamespaceID.Value = ApplicationServiceContext.Current.GetService<INetworkInformationService>()?.GetMachineName();
            msh.SendingFacility.UniversalID.Value = config.LocalFacility.ToString();
            msh.SendingFacility.UniversalIDType.Value = "GUID";
            msh.ReceivingApplication.NamespaceID.Value = inbound.SendingApplication.NamespaceID.Value;
            msh.ReceivingFacility.NamespaceID.Value = inbound.SendingFacility.NamespaceID.Value;
            msh.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss");

        }

        /// <summary>
        /// Parse a message
        /// </summary>
        public static IMessage ParseMessage(String messageData, out string originalVersion)
        {
            Regex versionRegex = new Regex(@"^(MSH\|.*?)(2\.[0-9\.]+)(.*)$", RegexOptions.Multiline);
            var match = versionRegex.Match(messageData);
            if (!match.Success)
                throw new InvalidOperationException("Message appears to be invalid");
            else
            {
                originalVersion = match.Groups[2].Value;
                PipeParser parser = new PipeParser();
                return parser.Parse(messageData, "2.5");
            }
        }

        /// <summary>
        /// Rewrite a QPD query to an HDSI query
        /// </summary>
        public static NameValueCollection ParseQueryElement(IEnumerable<Varies> varies, Hl7QueryParameterType map, String matchAlgorithm = "pattern", double? matchStrength = null )
        {
            NameValueCollection retVal = new NameValueCollection();
            var config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

            // Query parameters
            foreach (var qp in varies)
            {
                var composite = qp.Data as GenericComposite;

                // Parse the parameters
                var qfield = (composite.Components[0] as Varies)?.Data?.ToString();
                var qvalue = (composite.Components[1] as Varies)?.Data?.ToString();

                // Attempt to find the query parameter and map
                var parm = map.Parameters.Where(o => o.Hl7Name == qfield || o.Hl7Name == qfield + ".1" || o.Hl7Name == qfield + ".1.1").OrderBy(o => o.Hl7Name.Length - qfield.Length).FirstOrDefault();
                if (parm == null)
                    throw new ArgumentOutOfRangeException($"{qfield} not mapped to query parameter");

                switch (parm.ParameterType)
                {
                    case "concept":
                        retVal.Add($"{parm.ModelName}.referenceTerm.term.mnemonic", qvalue);
                        break;
                    case "string": // Enables phonetic matching
                        String transform = null;
                        switch ((matchAlgorithm ?? "pattern").ToLower())
                        {
                            case "exact":
                                transform = "{0}";
                                break;
                            case "pattern":
                                transform = "~*{0}*";
                                break;
                            case "soundex":
                                if (matchStrength.HasValue)
                                    transform = ":(soundex){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},soundex)<={matchStrength * qvalue.Length}";
                                break;
                            case "metaphone":
                                if (matchStrength.HasValue)
                                    transform = ":(metaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},metaphone)<={matchStrength * qvalue.Length}";
                                break;
                            case "dmetaphone":
                                if (matchStrength.HasValue)
                                    transform = ":(dmetaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},dmetaphone)<={matchStrength * qvalue.Length}";
                                break;
                            case "alias":
                                transform = $":(alias|{{0}})>={matchStrength ?? 3}";
                                break;
                            default:
                                transform = $":(phonetic_diff|{{0}})<={matchStrength * qvalue.Length},:(alias|{{0}})>={matchStrength ?? 3}";
                                break;
                        }
                        retVal.Add(parm.ModelName, transform.Split(',').Select(tx => String.Format(tx, qvalue)).ToList());
                        break;
                    default:
                        var txv = parm.ValueTransform ?? "{0}";
                        retVal.Add(parm.ModelName, txv.Split(',').Select(tx => String.Format(tx, qvalue)).ToList());
                        break;
                }
            }

            // HACK: Are they asking for the @PID.3.4.1 of our local auth?
            List<String> localId = null;
            if(retVal.TryGetValue("identifier.authority.domainName", out localId) &&
                localId.Contains(config.LocalAuthority.DomainName)) { 
                retVal.Remove("identifier.authority.domainName");
                localId = retVal["identifier.value"];
                retVal.Remove("identifier.value");
                retVal.Add("_id", localId);
            }

            return retVal;
        }

        /// <summary>
        /// Encode the specified message
        /// </summary>
        public static String EncodeMessage(IMessage response, string originalVersion)
        {
            // Rewrite back to original version
            (response.GetStructure("MSH") as MSH).VersionID.VersionID.Value = originalVersion;
            return new PipeParser().Encode(response);
        }
    }
}
