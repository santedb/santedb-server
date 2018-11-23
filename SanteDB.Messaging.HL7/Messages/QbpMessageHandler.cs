﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-10-14
 */
using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.ParameterMap;
using System.Linq.Expressions;
using System.Collections;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Query by parameter messge handler
    /// </summary>
    public class QbpMessageHandler : MessageHandlerBase
    {

        // Loaded query parameter map
        private static Hl7QueryParameterMap s_map;

        /// <summary>
        /// Qbp Message handler CTOR
        /// </summary>
        static QbpMessageHandler()
        {
            OpenMapping(typeof(QbpMessageHandler).Assembly.GetManifestResourceStream("SanteDB.Messaging.HL7.ParameterMap.xml"));

            if (!String.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location))
            {
                var externMap = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ParameterMap.Hl7.xml");

                if (File.Exists(externMap))
                    using (var s = File.OpenRead(externMap))
                        OpenMapping(s);
            }
        }

        /// <summary>
        /// Open the specified mapping
        /// </summary>
        private static void OpenMapping(Stream stream)
        {
            XmlSerializer xsz = new XmlSerializer(typeof(Hl7QueryParameterMap));

            if (s_map == null)
                s_map = xsz.Deserialize(stream) as Hl7QueryParameterMap;
            else
            {
                // Merge
                var map = xsz.Deserialize(stream) as Hl7QueryParameterMap;
                s_map.Merge(map);
            }

        }

        /// <summary>
        /// Handle message internally
        /// </summary>
        protected override IMessage HandleMessageInternal(Hl7MessageReceivedEventArgs e, Bundle parsed)
        {
            // First we want to get the map
            var msh = e.Message.GetStructure("MSH") as MSH;
            var trigger = msh.MessageType.TriggerEvent.Value;
            var map = s_map.Map.First(o => o.Trigger == trigger);
            
            try
            {
                if (map.ResponseType == null)
                    throw new NotSupportedException($"Response type not found");

                // First, process the query parameters
                var qpd = e.Message.GetStructure("QPD") as QPD;
                var query = this.RewriteQuery(qpd, map);

                // Control?
                var rcp = e.Message.GetStructure("RCP") as RCP;
                int? count = 0, offset = 0;
                Guid queryId = Guid.NewGuid();
                if (!String.IsNullOrEmpty(rcp.QuantityLimitedRequest.Quantity.Value))
                    count = Int32.Parse(rcp.QuantityLimitedRequest.Quantity.Value);

                // Continuation?
                var dsc = e.Message.GetStructure("DSC") as DSC;
                if (!String.IsNullOrEmpty(dsc.ContinuationPointer.Value))
                {
                    if (!Guid.TryParse(dsc.ContinuationPointer.Value, out queryId))
                        throw new InvalidOperationException($"DSC^1 must be UUID provided by this service.");
                }

                // Get the query tag which is the current offset
                if (ApplicationContext.Current.GetService<Core.Services.IQueryPersistenceService>()?.IsRegistered(queryId) == true)
                {
                    var tag = ApplicationContext.Current.GetService<Core.Services.IQueryPersistenceService>().GetQueryTag(queryId);
                    if (tag is int)
                        offset = (int)tag;
                }

                // Next, we want to get the repository for the bound type
                var repoService = ApplicationContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(map.QueryTarget)) as IPersistableQueryRepositoryService;
                if (repoService == null)
                    throw new InvalidOperationException($"Cannot find repository service for {map.QueryTargetXml}");

                // Build query
                var queryMethod = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression),
                    new Type[] { map.QueryTarget },
                    new Type[] { typeof(NameValueCollection) });
                var filterQuery = queryMethod.Invoke(null, new object[] { query }) as Expression;

                // Now we want to query
                object[] parameters = { filterQuery, offset.Value, (int?)count, null, queryId };
                var findMethod = repoService.GetType().GetGenericMethod(nameof(IPersistableQueryRepositoryService.Find),
                    new Type[] { map.QueryTarget },
                    new Type[] { filterQuery.GetType(), typeof(int), typeof(int?), typeof(int), typeof(Guid) }
                );
                IEnumerable results = findMethod.Invoke(repoService, parameters) as IEnumerable;
                int totalResults = (int)parameters[3];

                // Save the tag
                if (dsc.ContinuationPointer.Value != queryId.ToString())
                    ApplicationContext.Current.GetService<Core.Services.IQueryPersistenceService>()?.SetQueryTag(queryId, count);

                // Query basics
                var retVal = this.CreateACK(map.ResponseType, e.Message, "AA", "Query Success");
                var omsh = retVal.GetStructure("MSH") as MSH;
                var qak = retVal.GetStructure("QAK") as QAK;
                var odsc = retVal.GetStructure("DSC") as DSC;
                var oqpd = retVal.GetStructure("QPD") as QPD;
                DeepCopy.copy(qpd, oqpd);
                omsh.MessageType.TriggerEvent.Value = map.ResponseTrigger;
                qak.HitCount.Value = totalResults.ToString();
                qak.HitsRemaining.Value = (totalResults - offset - count > 0 ? totalResults - offset - count : 0).ToString();
                qak.QueryResponseStatus.Value = "AA";
                qak.ThisPayload.Value = results.OfType<Object>().Count().ToString();

                if (ApplicationContext.Current.GetService<Core.Services.IQueryPersistenceService>() != null)
                {
                    odsc.ContinuationPointer.Value = queryId.ToString();
                    odsc.ContinuationStyle.Value = "RD";
                }

                // Process results
                retVal = map.ResultHandler.AppendQueryResult(results, filterQuery, retVal, e, map.ScoreConfiguration, offset.GetValueOrDefault());
                return retVal;
            }
            catch(Exception ex)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "Error executing query: {0}", ex);

                // Now we construct the response
                return this.CreateNACK(map.ResponseType, e.Message, ex, e);
            }
        }


        /// <summary>
        /// Rewrite a QPD query to an HDSI query
        /// </summary>
        private NameValueCollection RewriteQuery(QPD qpd, Hl7QueryParameterType map)
        {
            NameValueCollection retVal = new NameValueCollection();

            // Control of strength
            String strStrength = (qpd.GetField(4, 0) as Varies)?.Data.ToString(),
                algorithm = (qpd.GetField(5, 0) as Varies)?.Data.ToString();
            Double? strength = String.IsNullOrEmpty(strStrength) ? null : (double?)Double.Parse(strStrength);

            // Query parameters
            foreach (var qp in qpd.GetField(3).OfType<Varies>())
            {
                var composite = qp.Data as GenericComposite;

                // Parse the parameters
                var qfield = (composite.Components[0] as Varies)?.Data?.ToString();
                var qvalue = (composite.Components[1] as Varies)?.Data?.ToString();

                // Attempt to find the query parameter and map
                var parm = map.Parameters.Where(o => o.Hl7Name == qfield || o.Hl7Name == qfield + ".1" || o.Hl7Name == qfield + ".1.1").OrderBy(o=>o.Hl7Name.Length - qfield.Length).FirstOrDefault();
                if (parm == null)
                    throw new ArgumentOutOfRangeException($"{qfield} not mapped to query parameter");

                switch(parm.ParameterType)
                {
                    case "concept":
                        retVal.Add($"{parm.ModelName}.referenceTerm.term.mnemonic", qvalue);
                        break;
                    case "string": // Enables phonetic matching
                        String transform = null;
                        switch(algorithm.ToLower())
                        {
                            case "exact":
                                transform = "{0}";
                                break;
                            case "soundex":
                                if (strength.HasValue)
                                    transform = ":(soundex){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},soundex)<={strength * qvalue.Length}";
                                break;
                            case "metaphone":
                                if (strength.HasValue)
                                    transform = ":(metaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},metaphone)<={strength * qvalue.Length}";
                                break;
                            case "dmetaphone":
                                if (strength.HasValue)
                                    transform = ":(dmetaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},dmetaphone)<={strength * qvalue.Length}";
                                break;
                            case "alias":
                                transform = $":(alias|{{0}})>={strength ?? 3}";
                                break;
                            default:
                                transform = $":(phonetic_diff|{{0}})<={strength * qvalue.Length},:(alias|{{0}})>={strength ?? 3}";
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

            // Return domains
            foreach(var rt in qpd.GetField(8).OfType<Varies>())
            {
                var rid = new CX(qpd.Message);
                DeepCopy.copy(rt.Data as GenericComposite, rid);

                if (String.IsNullOrEmpty(rid.AssigningAuthority.NamespaceID.Value)) // lookup by AA 
                {
                    var aa = ApplicationContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>().Query(o => o.Oid == rid.AssigningAuthority.UniversalID.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    if (aa == null)
                        throw new InvalidOperationException($"Domain {rid.AssigningAuthority.UniversalID.Value} is unknown");
                    else
                        retVal.Add($"identifier[{aa.DomainName}]", "!null");
                }
                else
                    retVal.Add($"identifier[{rid.AssigningAuthority.NamespaceID.Value}]", "!null");
            }

            
            return retVal;
        }

        /// <summary>
        /// Validate that this message can be processed
        /// </summary>
        protected override bool Validate(IMessage message)
        {
            // Get the 
            var msh = message.GetStructure("MSH") as MSH;
            var trigger = msh.MessageType.TriggerEvent.Value;

            if (!s_map.Map.Any(m => m.Trigger == trigger))
                throw new NotSupportedException($"{trigger} not understood or mapped");
            return true;
        }
    }
}
