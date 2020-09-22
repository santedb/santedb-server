/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Datatype;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Data converter for base types
    /// </summary>
    public static class DataConverter
    {

        private const string AddressUseCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.190";
        private const string NameUseCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.200";
        private const string TelecomUseCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.201";
        private const string TelecomTypeCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.202";
        private const string IdentifierTypeCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.203";


        // configuration 
        private static Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current?.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        /// <summary>
        /// Convert the message to v2.5
        /// </summary>
        public static IMessage ToVersion25(IMessage nonMessage)
        {
            if (nonMessage.Version == "2.5")
                return nonMessage;
            else
            {
                ((nonMessage.GetStructure("MSH") as ISegment).GetField(12, 0) as AbstractPrimitive).Value = "2.5";
                return new PipeParser().Parse(new PipeParser().Encode(nonMessage));
            }
        }

        /// <summary>
        /// Converts the specified HL7v2 XAD to an entity address
        /// </summary>
        /// <param name="address">The address to be converted</param>
        /// <returns>The converted entity address</returns>
        public static EntityAddress ToModel(this XAD address)
        {
            return new XAD[] { address }.ToModel().FirstOrDefault();
        }

        /// <summary>
		/// Converts a list of <see cref="XAD"/> addresses to a list of <see cref="EntityAddress"/> addresses.
		/// </summary>
		/// <param name="addresses">The addresses to be converted.</param>
		/// <returns>Returns a list of entity addresses.</returns>
		public static IEnumerable<EntityAddress> ToModel(this XAD[] addresses)
        {
            var entityAddresses = new List<EntityAddress>();
            var conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();

            if (addresses.Length == 0)
                return entityAddresses.AsEnumerable();

            foreach (var xad in addresses)
            {
                var entityAddress = new EntityAddress();
                var addressUse = AddressUseKeys.TemporaryAddress;

                if (!string.IsNullOrEmpty(xad.AddressType.Value) && !string.IsNullOrWhiteSpace(xad.AddressType.Value))
                {
                    var concept = conceptService?.FindConceptsByReferenceTerm(xad.AddressType.Value, AddressUseCodeSystem).FirstOrDefault();
                    if (concept == null)
                        throw new HL7DatatypeProcessingException($"Error processing XAD", 6, new KeyNotFoundException($"Address use code {xad.AddressType.Value} not known"));

                    addressUse = concept.Key.Value;
                }
                entityAddress.AddressUseKey = addressUse;

                // Map the parts
                var mappedFields = new Dictionary<String, Guid>()
                {
                    {  nameof(XAD.CensusTract), AddressComponentKeys.CensusTract },
                    {  nameof(XAD.City), AddressComponentKeys.City },
                    {  nameof(XAD.Country), AddressComponentKeys.Country },
                    {  nameof(XAD.CountyParishCode), AddressComponentKeys.County },
                    {  nameof(XAD.OtherDesignation), AddressComponentKeys.AdditionalLocator },
                    {  nameof(XAD.StateOrProvince), AddressComponentKeys.State },
                    {  nameof(XAD.StreetAddress), AddressComponentKeys.StreetAddressLine },
                    {  nameof(XAD.OtherGeographicDesignation), AddressComponentKeys.Precinct },
                    {  nameof(XAD.ZipOrPostalCode), AddressComponentKeys.PostalCode }
                };

                foreach (var kv in mappedFields)
                {
                    var rawValue = typeof(XAD).GetRuntimeProperty(kv.Key).GetValue(xad);
                    if (rawValue is SAD)
                    {
                        var value = rawValue as SAD;
                        if (!String.IsNullOrEmpty(value.StreetOrMailingAddress.Value))
                            entityAddress.Component.Add(new EntityAddressComponent(kv.Value, value.StreetOrMailingAddress.Value));
                        if (!String.IsNullOrEmpty(value.StreetName.Value))
                            entityAddress.Component.Add(new EntityAddressComponent(AddressComponentKeys.StreetName, value.StreetName.Value));
                        if (!String.IsNullOrEmpty(value.DwellingNumber.Value))
                            entityAddress.Component.Add(new EntityAddressComponent(AddressComponentKeys.UnitIdentifier, value.DwellingNumber.Value));
                    }
                    else
                    {
                        var value = rawValue as AbstractPrimitive;
                        if (!string.IsNullOrEmpty(value?.Value))
                            entityAddress.Component.Add(new EntityAddressComponent(kv.Value, value.Value));
                    }
                }
                entityAddresses.Add(entityAddress);
            }

            return entityAddresses.AsEnumerable();
        }


        /// <summary>
        /// Convert a telecom address to an XTN v2 structure
        /// </summary>
        public static XTN FromModel(this XTN me, EntityTelecomAddress tel)
        {

            if (tel.AddressUseKey != NullReasonKeys.NoInformation)
            {
                var useTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(tel.AddressUseKey.GetValueOrDefault(), TelecomUseCodeSystem);
                me.TelecommunicationUseCode.Value = useTerm.Mnemonic;
            }

            if(tel.TypeConceptKey.HasValue)
            {
                var typeTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(tel.TypeConceptKey.GetValueOrDefault(), TelecomTypeCodeSystem);
                me.TelecommunicationEquipmentType.Value = typeTerm.Mnemonic;
            }

            me.AnyText.Value = tel.Value;
            return me;

        }

        /// <summary>
        /// Convert address to XAD
        /// </summary>
        public static XAD FromModel(this XAD me, EntityAddress addr)
        {
            var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(addr.AddressUseKey.GetValueOrDefault(), AddressUseCodeSystem);
            if (refTerm != null)
                me.AddressType.Value = refTerm.Mnemonic;

            var mappedFields = new Dictionary<Guid, String>()
                {
                    {  AddressComponentKeys.CensusTract, nameof(XAD.CensusTract) },
                    {  AddressComponentKeys.City , nameof(XAD.City) },
                    {  AddressComponentKeys.Country, nameof(XAD.Country)  },
                    {  AddressComponentKeys.County, nameof(XAD.CountyParishCode) },
                    {  AddressComponentKeys.AdditionalLocator, nameof(XAD.OtherDesignation)  },
                    {  AddressComponentKeys.State , nameof(XAD.StateOrProvince) },
                    {  AddressComponentKeys.StreetAddressLine , nameof(XAD.StreetAddress) },
                    {  AddressComponentKeys.Precinct, nameof(XAD.OtherGeographicDesignation) },
                    {  AddressComponentKeys.StreetName, nameof(XAD.StreetAddress) },
                    {  AddressComponentKeys.PostalCode, nameof(XAD.ZipOrPostalCode) },
                    {  AddressComponentKeys.UnitIdentifier, nameof(XAD.StreetAddress) }
                };

            foreach (var itm in addr.LoadCollection<EntityAddressComponent>("Component"))
            {
                String propertyName = null;
                if (itm.ComponentTypeKey.HasValue && mappedFields.TryGetValue(itm.ComponentTypeKey.Value, out propertyName))
                {
                    var valueItem = typeof(XAD).GetRuntimeProperty(propertyName).GetValue(me);
                    if (valueItem is SAD) // Street address {
                    {
                        var sadItem = valueItem as SAD;
                        if (itm.ComponentTypeKey == AddressComponentKeys.UnitIdentifier)
                            sadItem.DwellingNumber.Value = itm.Value;
                        else if (itm.ComponentTypeKey == AddressComponentKeys.StreetName)
                            sadItem.StreetName.Value = itm.Value;
                        else if (itm.ComponentTypeKey == AddressComponentKeys.StreetAddressLine)
                            sadItem.StreetOrMailingAddress.Value = itm.Value;
                    }
                    else if (valueItem is AbstractPrimitive)
                        (valueItem as AbstractPrimitive).Value = itm.Value;
                }
            }

            return me;
        }


        /// <summary>
        /// Converts the specified XPN instance to an entity name
        /// </summary>
        /// <param name="name">The name to be converted</param>
        /// <returns>The name</returns>
        public static EntityName ToModel(this XPN name)
        {
            return new XPN[] { name }.ToModel().FirstOrDefault();
        }

        /// <summary>
        /// Converts a list of <see cref="XPN"/> instance to <see cref="EntityName"/> instances
        /// </summary>
        /// <param name="names">The names to be converted</param>
        /// <returns>The converted list of names</returns>
		public static IEnumerable<EntityName> ToModel(this XPN[] names, Guid? nameUseKey = null )
        {
            var entityNames = new List<EntityName>();
            var conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();

            if (names.Length == 0)
                return entityNames.AsEnumerable();

            foreach (var xpn in names)
            {
                var entityName = new EntityName();
                var nameUse = nameUseKey ?? NameUseKeys.Search;

                if (!string.IsNullOrEmpty(xpn.NameTypeCode.Value))
                {
                    var concept = conceptService?.FindConceptsByReferenceTerm(xpn.NameTypeCode.Value, NameUseCodeSystem).FirstOrDefault();
                    if (concept == null)
                        throw new HL7DatatypeProcessingException("Error processing XPN", 6, new KeyNotFoundException($"Entity name use code {xpn.NameTypeCode.Value} not known"));

                    nameUse = concept.Key.Value;
                }
                entityName.NameUseKey = nameUse;

                // Map the parts
                var mappedFields = new Dictionary<String, Guid>()
                {
                    {  nameof(XPN.FamilyName), NameComponentKeys.Family },
                    {  nameof(XPN.GivenName), NameComponentKeys.Given },
                    {  nameof(XPN.SecondAndFurtherGivenNamesOrInitialsThereof), NameComponentKeys.Given },
                    {  nameof(XPN.PrefixEgDR), NameComponentKeys.Prefix },
                    {  nameof(XPN.SuffixEgJRorIII), NameComponentKeys.Suffix }
                };

                // Mapped fields
                foreach (var kv in mappedFields)
                {
                    var rawValue = typeof(XPN).GetRuntimeProperty(kv.Key).GetValue(xpn);
                    if (rawValue is FN) // Family name components are just added to the name as FamilyNAme components
                    {
                        var value = rawValue as FN;
                        foreach (var c in value.Components.Take(5))
                        {
                            var cValue = c as AbstractPrimitive;
                            if (!String.IsNullOrEmpty(cValue?.Value))
                                entityName.Component.Add(new EntityNameComponent(kv.Value, cValue.Value));
                        }
                    }
                    else
                    {
                        var value = rawValue as AbstractPrimitive;
                        if (!string.IsNullOrEmpty(value?.Value))
                            entityName.Component.Add(new EntityNameComponent(kv.Value, value.Value));
                    }
                }
                entityNames.Add(entityName);
            }

            return entityNames.AsEnumerable();
        }

        /// <summary>
        /// From a model
        /// </summary>
        public static IS FromModel(this IS me, Concept concept, String domain)
        {
            if (concept == null) return me;
            var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(concept.Key.Value, domain);
            me.Value = refTerm?.Mnemonic;
            return me;
        }

        /// <summary>
        /// From a model
        /// </summary>
        public static ID FromModel(this ID me, Concept concept, String domain)
        {
            if (concept == null) return me;
            var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(concept.Key.Value, domain);
            me.Value = refTerm?.Mnemonic;
            return me;
        }

        /// <summary>
        /// Convert name to XPN
        /// </summary>
        public static XPN FromModel(this XPN me, EntityName name)
        {
            var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(name.NameUseKey.GetValueOrDefault(), NameUseCodeSystem);
            if (refTerm != null)
                me.NameTypeCode.Value = refTerm.Mnemonic;

            // Convert components
            foreach (var itm in name.LoadCollection<EntityNameComponent>("Component"))
                if (itm.ComponentTypeKey == NameComponentKeys.Family)
                {
                    if (string.IsNullOrEmpty(me.FamilyName.Surname.Value))
                        me.FamilyName.Surname.Value = itm.Value;
                    else if (string.IsNullOrEmpty(me.FamilyName.OwnSurname.Value))
                        me.FamilyName.OwnSurname.Value = itm.Value;
                }
                else if (itm.ComponentTypeKey == NameComponentKeys.Given)
                {
                    if (String.IsNullOrEmpty(me.GivenName.Value))
                        me.GivenName.Value = itm.Value;
                    else
                        me.SecondAndFurtherGivenNamesOrInitialsThereof.Value += itm.Value + " ";
                }
                else if (itm.ComponentTypeKey == NameComponentKeys.Suffix)
                    me.SuffixEgJRorIII.Value = itm.Value;
                else if (itm.ComponentTypeKey == NameComponentKeys.Prefix)
                    me.PrefixEgDR.Value = itm.Value;

            return me;
        }

        /// <summary>
		/// Converts an <see cref="HD"/> instance to an <see cref="AssigningAuthority"/> instance.
		/// </summary>
		/// <param name="id">The id value to be converted.</param>
		/// <returns>Returns the converted assigning authority.</returns>
		public static AssigningAuthority ToModel(this HD id)
        {
            var assigningAuthorityRepositoryService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            AssigningAuthority assigningAuthority = null;

            if (id == null)
                throw new ArgumentNullException(nameof(id), "Missing ID parameter");

            // Internal key identifier?
            if (!String.IsNullOrEmpty(id.NamespaceID.Value) && id.NamespaceID.Value == m_configuration.LocalAuthority.DomainName ||
                !String.IsNullOrEmpty(id.UniversalID.Value) && id.UniversalID.Value == m_configuration.LocalAuthority.Oid)
                return m_configuration.LocalAuthority;

            if (!string.IsNullOrEmpty(id.NamespaceID.Value))
            {
                assigningAuthority = assigningAuthorityRepositoryService.Get(id.NamespaceID.Value);
                if (assigningAuthority == null)
                    throw new HL7DatatypeProcessingException("Error processing HD", 1, new KeyNotFoundException($"Authority {id.NamespaceID.Value} not found"));
            }

            if (!string.IsNullOrEmpty(id.UniversalID.Value))
            {
                var tAssigningAuthority = assigningAuthorityRepositoryService.Get(new Uri($"urn:oid:{id.UniversalID.Value}"));

                if (tAssigningAuthority == null)
                    throw new HL7DatatypeProcessingException("Error processing HD", 2, new KeyNotFoundException($"Authority {id.UniversalID.Value} not found"));
                else if (assigningAuthority != null && tAssigningAuthority?.Key != assigningAuthority.Key) // Must agree
                    throw new HL7DatatypeProcessingException("Error processing HD", 2, new ArgumentException("When both NamespaceID and UniversalID are specified, both must agree with configured values"));
                else
                    assigningAuthority = tAssigningAuthority;
            }

            return assigningAuthority;
        }

        /// <summary>
		/// Converts an <see cref="CX"/> instance to an <see cref="EntityIdentifier"/> instance.
		/// </summary>
		/// <param name="identifier">The identifier to be converted.</param>
		/// <returns>Returns the converted identifier.</returns>
		public static EntityIdentifier ToModel(this CX identifier)
        {
            return new CX[] { identifier }.ToModel().FirstOrDefault();
        }

        /// <summary>
        /// Converts a list of <see cref="CX"/> identifiers to a list of <see cref="EntityIdentifier"/> identifiers.
        /// </summary>
        /// <param name="identifiers">The list of identifiers to be converted.</param>
        /// <returns>Returns a list of entity identifiers.</returns>
        public static IEnumerable<EntityIdentifier> ToModel(this CX[] identifiers)
        {
            var entityIdentifiers = new List<EntityIdentifier>();

            if (identifiers.Length == 0)
                return entityIdentifiers.AsEnumerable();

            foreach (var cx in identifiers)
            {
                try
                {
                    var assigningAuthority = cx.AssigningAuthority.ToModel();
                    if (assigningAuthority != null && assigningAuthority.Key != m_configuration.LocalAuthority.Key)
                    {
                        var id = new EntityIdentifier(assigningAuthority, cx.IDNumber.Value);

                        if (!String.IsNullOrEmpty(cx.IdentifierTypeCode.Value))
                        {
                            int tr = 0;
                            var idType = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentifierType>>().Query(o => o.TypeConcept.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == cx.IdentifierTypeCode.Value && r.ReferenceTerm.CodeSystem.Oid == IdentifierTypeCodeSystem), 0, 1, out tr, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                            id.IdentifierTypeKey = idType?.Key;
                        }

                        if (!String.IsNullOrEmpty(cx.ExpirationDate.Value)) {

                            id.ExpiryDate = new DateTime(cx.ExpirationDate.Year, cx.ExpirationDate.Month, cx.ExpirationDate.Day);
                            // Is the value already expired? If so we can obsolete the identifier 
                            if (id.ExpiryDate <= DateTime.Now.Date) // Value is expired - indicate this
                                id.ObsoleteVersionSequenceId = Int32.MaxValue;
                        }
                        if (!String.IsNullOrEmpty(cx.EffectiveDate.Value)) {

                            id.IssueDate = new DateTime(cx.EffectiveDate.Year, cx.EffectiveDate.Month, cx.EffectiveDate.Day);

                            if (id.IssueDate <= DateTime.Now.Date) // Value is being actively changed - indicate this
                                id.EffectiveVersionSequenceId = Int32.MaxValue;
                        }

                        entityIdentifiers.Add(id);
                    }
                }
                catch(Exception e)
                {
                    throw new HL7DatatypeProcessingException(e.Message, 3, e);
                }
            }

            return entityIdentifiers.AsEnumerable();
        }

        private static Dictionary<int, string> m_precisionFormats = new Dictionary<int, string>()
        {
            { 8, "yyyyMMdd" },
            { 23, "yyyyMMddHHmmss.fffzzzz" },
            { 18, "yyyyMMddHHmmss.fff" },
            { 15, "yyyyMMddHHzzzz" },
            { 10, "yyyyMMddHH" },
            { 17, "yyyyMMddHHmmzzzz" },
            { 12, "yyyyMMddHHmm" },
            { 6, "yyyyMM" },
            { 19, "yyyyMMddHHmmsszzzz" },
            { 14, "yyyyMMddHHmmss" },
            { 4, "yyyy" }
        };

        /// <summary>
		/// Converts a <see cref="TS"/> instance to a <see cref="DateTime"/> instance.
		/// </summary>
		/// <param name="timestamp">The TS instance to be converted.</param>
		/// <returns>Returns the converted date time instance or null.</returns>
		public static DateTime? ToModel(this TS timestamp)
        {
            DateTime? result = null;

            if (timestamp.Time.Value == null)
                return result;
            else
            {

                string value = timestamp.Time.Value;
                int datePrecision = value.Length;

                // Parse a correct precision 
                try
                {

                    // HACK: Correct timezone as some CDA instances instances have only 3
                    if (value.Contains("+") || value.Contains("-"))
                    {
                        int sTz = value.Contains("+") ? value.IndexOf("+") : value.IndexOf("-");
                        string tzValue = value.Substring(sTz + 1);
                        int iTzValue = 0;
                        if (!Int32.TryParse(tzValue, out iTzValue)) // Invalid timezone can't even fix this
                            throw new HL7DatatypeProcessingException("Error processing TS", 1, new ArgumentOutOfRangeException($"Invalid timezone {tzValue}!"));
                        else if (iTzValue < 24)
                            value = value.Substring(0, sTz + 1) + iTzValue.ToString("00") + "00";
                        else
                            value = value.Substring(0, sTz + 1) + iTzValue.ToString("0000");
                    }


                    // HACK: Correct the milliseonds to be three digits if four are passed into the parse function
                    if (datePrecision == 24 || datePrecision == 19 && value.Contains("."))
                    {
                        int eMs = value.Contains("-") ? value.IndexOf("-") - 1 : value.Contains("+") ? value.IndexOf("+") - 1 : value.Length - 1;
                        value = value.Remove(eMs, 1);
                    }
                    else if (datePrecision > 19 && value.Contains(".")) // HACK: sometimes milliseconds can be one or two digits
                    {
                        int eMs = value.Contains("-") ? value.IndexOf("-") - 1 : value.Contains("+") ? value.IndexOf("+") : value.Length - 1;
                        int sMs = value.IndexOf(".");
                        value = value.Insert(eMs + 1, new string('0', 3 - (eMs - sMs)));
                    }
                    datePrecision = value.Length;

                }
                catch (Exception) { datePrecision = 23; }

                string flavorFormat = null;
                if (!m_precisionFormats.TryGetValue(datePrecision, out flavorFormat))
                    flavorFormat = "yyyyMMddHHmmss.fffzzzz";


                // Now parse the date string
                try
                {
                    if (value.Length > flavorFormat.Length)
                        return DateTime.ParseExact(value.Substring(0, flavorFormat.Length + (flavorFormat.Contains("z") ? 1 : 0)), flavorFormat, CultureInfo.InvariantCulture);
                    else
                        return DateTime.ParseExact(value, flavorFormat.Substring(0, value.Length), CultureInfo.InvariantCulture);
                }
                catch(Exception e)
                {
                    throw new HL7DatatypeProcessingException("Error processing TS", 1, new FormatException($"Date {value} was not valid according to format {flavorFormat}", e));
                }
            }
        }

        /// <summary>
        /// Return as a model
        /// </summary>
        public static IEnumerable<EntityTelecomAddress> ToModel(this XTN[] xtn)
        {
            return xtn.Select(x => x.ToModel());
        }

        /// <summary>
		/// Converts a <see cref="XTN"/> instance to a <see cref="EntityTelecomAddress"/> instance.
		/// </summary>
		/// <param name="xtn">The v2 XTN instance to be converted.</param>
		/// <returns>Returns the converted entity telecom address instance.</returns>
		public static EntityTelecomAddress ToModel(this XTN xtn)
        {
            var re = new Regex(@"([+0-9A-Za-z]{1,4})?\((\d{3})\)?(\d{3})\-(\d{4})X?(\d{1,6})?");
            var retVal = new EntityTelecomAddress();

            if (!String.IsNullOrEmpty(xtn.EmailAddress?.Value))
                retVal.IETFValue = $"mailto:{xtn.EmailAddress.Value}";
            else if (xtn.AnyText.Value == null)
            {
                var sb = new StringBuilder("tel:");

                try
                {
                    if (xtn.CountryCode.Value != null)
                        sb.AppendFormat("{0}{1}-", xtn.CountryCode.Value.Contains("+") ? "" : "+",  xtn.CountryCode);

                    if (!String.IsNullOrEmpty(xtn.TelephoneNumber?.Value))
                    {
                        if (xtn.TelephoneNumber?.Value != null && !xtn.TelephoneNumber.Value.Contains("-"))
                            xtn.TelephoneNumber.Value = xtn.TelephoneNumber.Value.Insert(3, "-");
                        sb.AppendFormat("{0}-{1}", xtn.AreaCityCode, xtn.TelephoneNumber.Value);
                    }
                    else
                        sb.AppendFormat("{0}-{1}", xtn.AreaCityCode, xtn.LocalNumber.Value.Contains("-") ? xtn.LocalNumber.Value : xtn.LocalNumber.Value.Replace(" ","-").Insert(3, "-"));

                    if (xtn.Extension.Value != null)
                        sb.AppendFormat(";ext={0}", xtn.Extension);
                }
                catch
                {
                    // ignored
                }

                if (sb.ToString().EndsWith("tel:") || sb.ToString() == "tel:-")
                    retVal.IETFValue = "tel:" + xtn.AnyText.Value;
                else
                    retVal.IETFValue = sb.ToString();
            }
            else
            {
                var match = re.Match(xtn.UnformattedTelephoneNumber.Value);
                var sb = new StringBuilder("tel:");

                for (var i = 1; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(match.Groups[i].Value))
                        sb.AppendFormat("{0}{1}", match.Groups[i].Value, i == 4 ? "" : "-");
                }

                if (!string.IsNullOrEmpty(match.Groups[5].Value))
                {
                    sb.AppendFormat(";ext={0}", match.Groups[5].Value);
                }

                retVal.IETFValue = sb.ToString();
            }

            // Use code conversion
            Guid use = NullReasonKeys.NoInformation;

            if (!string.IsNullOrEmpty(xtn.TelecommunicationUseCode.Value))
            {
                var concept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(xtn.TelecommunicationUseCode.Value, TelecomUseCodeSystem).FirstOrDefault();

                if (concept == null)
                    throw new HL7DatatypeProcessingException("Error processing XTN", 1, new KeyNotFoundException($"Telecom use code {xtn.TelecommunicationUseCode.Value} not known"));

                use = concept.Key.Value;
            }

            retVal.AddressUseKey = use;

            // Type code conversion
            Guid? type = null;
            if(!string.IsNullOrEmpty(xtn.TelecommunicationEquipmentType.Value))
            {
                var concept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(xtn.TelecommunicationEquipmentType.Value, TelecomTypeCodeSystem).FirstOrDefault();
                if (concept == null)
                    throw new HL7DatatypeProcessingException("Error processing XTN", 2, new KeyNotFoundException($"Telecom equipment type {xtn.TelecommunicationEquipmentType.Value} not known"));
                type = concept.Key.Value;
            }
            retVal.TypeConceptKey = type;
            return retVal;
        }

        /// <summary>
        /// Convert to date precision
        /// </summary>
        public static DatePrecision ToDatePrecision(this TS me)
        {
            if (!me.DegreeOfPrecision.IsEmpty())
                return me.DegreeOfPrecision.Value == "Y" ? Core.Model.DataTypes.DatePrecision.Year :
                                me.DegreeOfPrecision.Value == "L" ? Core.Model.DataTypes.DatePrecision.Month :
                                me.DegreeOfPrecision.Value == "D" ? Core.Model.DataTypes.DatePrecision.Day :
                                me.DegreeOfPrecision.Value == "H" ? Core.Model.DataTypes.DatePrecision.Hour :
                                me.DegreeOfPrecision.Value == "M" ? Core.Model.DataTypes.DatePrecision.Minute :
                                me.DegreeOfPrecision.Value == "S" ? Core.Model.DataTypes.DatePrecision.Year :
                                Core.Model.DataTypes.DatePrecision.Full;
            else
                switch (me.Time.Value.Length)
                {
                    case 4:
                        return DatePrecision.Year;
                    case 6:
                        return DatePrecision.Month;
                    case 8:
                        return DatePrecision.Day;
                    case 10:
                        return DatePrecision.Hour;
                    case 12:
                        return DatePrecision.Minute;
                    case 14:
                        return DatePrecision.Second;
                    default:
                        throw new InvalidOperationException($"Cannot determine degree of precision of date {me.Time.Value}");
                }
        }

        /// <summary>
        /// Convert a simple string to a concept in the specified domain
        /// </summary>
        public static Concept ToConcept(this IS me, String domain, bool throwIfNotFound = true)
        {
            var concept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(me.Value, domain).FirstOrDefault();
            if (concept == null && throwIfNotFound)
                throw new KeyNotFoundException($"Concept {me.Value} is not registered in {domain}");
            else
                return concept;
        }

        /// <summary>
        /// Convert a simple string to a concept in the specified domain
        /// </summary>
        public static Concept ToConcept(this ID me, String domain)
        {
            return ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(me.Value, domain).FirstOrDefault();
        }

        /// <summary>
        /// Convert a CE to a concept
        /// </summary>
        public static Concept ToModel(this CE me, String preferredDomain = null, bool throwIfNotFound = true)
        {
            return new CE[] { me }.ToModel(preferredDomain, throwIfNotFound).FirstOrDefault();
        }

        /// <summary>
        /// Convert <see cref="CE"/> to <see cref="Concept"/>
        /// </summary>
        public static IEnumerable<Concept> ToModel(this CE[] me, String preferredDomain = null, bool throwIfNotFound = true)
        {
            var termService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
            List<Concept> retval = new List<Concept>();

            foreach (var code in me)
            {
                Concept concept = null;
                if (!String.IsNullOrEmpty(preferredDomain))
                    concept = termService.FindConceptsByReferenceTerm(code.Identifier.Value, preferredDomain).FirstOrDefault();
                if (concept == null)
                    concept = termService.FindConceptsByReferenceTerm(code.Identifier.Value, code.NameOfCodingSystem.Value).FirstOrDefault();

                if (concept == null && throwIfNotFound)
                    throw new HL7DatatypeProcessingException("Error processing CE", 1, new KeyNotFoundException($"Reference term {code.Identifier.Value} not found in {preferredDomain} or {code.NameOfCodingSystem.Value}"));
                retval.Add(concept);
            }
            return retval;
        }

        /// <summary>
        /// Convert entity identifier to a CX
        /// </summary>
        public static CX FromModel<TBind>(this CX me, IdentifierBase<TBind> id)
            where TBind : VersionedEntityData<TBind>, new()
        {
            me.IDNumber.Value = id.Value;
            me.AssigningAuthority.FromModel(id.LoadProperty<AssigningAuthority>("Authority"));

            if (id.ExpiryDate.HasValue)
                me.ExpirationDate.setYearMonthDayPrecision(id.ExpiryDate.Value.Year, id.ExpiryDate.Value.Month, id.ExpiryDate.Value.Day);
            if(id.IssueDate.HasValue)
                me.EffectiveDate.setYearMonthDayPrecision(id.IssueDate.Value.Year, id.IssueDate.Value.Month, id.IssueDate.Value.Day);

            me.CheckDigit.Value = id.CheckDigit;
            me.CheckDigitScheme.Value = id.LoadProperty<AssigningAuthority>("Authority").GetCustomValidator()?.Name;

            // Identifier type
            if (id.IdentifierType?.TypeConceptKey.HasValue == true)
            {
                var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(id.IdentifierType.TypeConceptKey.Value, IdentifierTypeCodeSystem);
                me.IdentifierTypeCode.Value = refTerm?.Mnemonic;
            }
            else
                me.IdentifierTypeCode.Value = "PT"; // EXTERNAL ID

            return me;
        }

        /// <summary>
        /// Convert assigning authortiy to v2
        /// </summary>
        public static HD FromModel(this HD me, AssigningAuthority authority)
        {
            me.NamespaceID.Value = authority.DomainName;
            me.UniversalID.Value = authority.Oid;
            me.UniversalIDType.Value = !String.IsNullOrEmpty(authority.Oid) ? "ISO" : null;
            return me;
        }

        /// <summary>
        /// Convert entity identifier to a CX
        /// </summary>
        public static CE FromModel(this CE me, Concept concept, String domain)
        {
            if (concept == null) return me;
            var refTerm = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(concept.Key.Value, domain);
            me.Identifier.Value = refTerm?.Mnemonic;
            me.NameOfCodingSystem.Value = refTerm.LoadProperty<CodeSystem>(nameof(ReferenceTerm.CodeSystem))?.Name;
            return me;
        }


    }
}
