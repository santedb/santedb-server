/*
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
 * Date: 2018-9-25
 */
using SanteDB.Core.Model;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Datatype;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        private const string IdentifierTypeCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.203";

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
            var conceptService = ApplicationContext.Current.GetService<IConceptRepositoryService>();

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
                        throw new ArgumentException($"Address use code {xad.AddressType.Value} not known");

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
                    {  nameof(XAD.OtherGeographicDesignation), AddressComponentKeys.Precinct }
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
        /// Convert address to XAD
        /// </summary>
        public static XAD FromModel(this XAD me, EntityAddress addr)
        {
            var refTerm = ApplicationContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(addr.AddressUseKey.GetValueOrDefault(), AddressUseCodeSystem);
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
		public static IEnumerable<EntityName> ToModel(this XPN[] names)
        {
            var entityNames = new List<EntityName>();
            var conceptService = ApplicationContext.Current.GetService<IConceptRepositoryService>();

            if (names.Length == 0)
                return entityNames.AsEnumerable();

            foreach (var xpn in names)
            {
                var entityName = new EntityName();
                var nameUse = NameUseKeys.Search;

                if (!string.IsNullOrEmpty(xpn.NameTypeCode.Value))
                {
                    var concept = conceptService?.FindConceptsByReferenceTerm(xpn.NameTypeCode.Value, NameUseCodeSystem).FirstOrDefault();
                    if (concept == null)
                        throw new ArgumentException($"Entity name use code {xpn.NameTypeCode.Value} not known");

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
        /// Convert name to XPN
        /// </summary>
        public static XPN FromModel(this XPN me, EntityName name)
        {
            var refTerm = ApplicationContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(name.NameUseKey.GetValueOrDefault(), NameUseCodeSystem);
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
		public static AssigningAuthority ToModel(this HD id, bool throwIfNotFound = true)
        {
            var assigningAuthorityRepositoryService = ApplicationContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            AssigningAuthority assigningAuthority = null;

            if (id == null)
                throw new ArgumentNullException(nameof(id), "Missing ID parameter");

            if (!string.IsNullOrEmpty(id.NamespaceID.Value))
            {
                assigningAuthority = assigningAuthorityRepositoryService.Get(id.NamespaceID.Value);
                if (assigningAuthority == null && throwIfNotFound)
                    throw new KeyNotFoundException($"Cannot find assigning authority {id.NamespaceID.Value}");
            }

            if (!string.IsNullOrEmpty(id.UniversalID.Value))
            {
                var tAssigningAuthority = assigningAuthorityRepositoryService.Get(new Uri($"urn:oid:{id.UniversalID.Value}"));

                if (tAssigningAuthority == null && throwIfNotFound)
                    throw new KeyNotFoundException($"Cannot find assigning authority {id.UniversalID.Value}");
                else if (assigningAuthority != null && tAssigningAuthority?.Key != assigningAuthority.Key) // Must agree
                    throw new InvalidOperationException("When both NamespaceID and UniversalID are specified, both must agree with configured values");
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
                var assigningAuthority = cx.AssigningAuthority.ToModel();
                var id = new EntityIdentifier(assigningAuthority, cx.IDNumber.Value);

                if (!String.IsNullOrEmpty(cx.IdentifierTypeCode.Value))
                {
                    var idType = ApplicationContext.Current.GetService<IRepositoryService<IdentifierType>>().Find(o => o.TypeConcept.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == cx.IdentifierTypeCode.Value && r.ReferenceTerm.CodeSystem.Oid == IdentifierTypeCodeSystem)).FirstOrDefault();
                    id.IdentifierTypeKey = idType?.Key;
                }

                entityIdentifiers.Add(id);
            }

            return entityIdentifiers.AsEnumerable();
        }


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

            object dateTime = null;
            if (Util.TryFromWireFormat(timestamp.Time.Value, typeof(MARC.Everest.DataTypes.TS), out dateTime))
                result = ((MARC.Everest.DataTypes.TS)dateTime).DateValue;
            else
                throw new InvalidOperationException($"Timestamp {timestamp.Time.Value} is not in a recognizable format");
            return result;
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
                retVal.Value = $"mailto:{xtn.EmailAddress.Value}";
            else if (xtn.AnyText.Value == null)
            {
                var sb = new StringBuilder("tel:");

                try
                {
                    if (xtn.CountryCode.Value != null)
                        sb.AppendFormat("{0}-", xtn.CountryCode);

                    if (!String.IsNullOrEmpty(xtn.TelephoneNumber?.Value))
                    {
                        if (xtn.TelephoneNumber?.Value != null && !xtn.TelephoneNumber.Value.Contains("-"))
                            xtn.TelephoneNumber.Value = xtn.TelephoneNumber.Value.Insert(3, "-");
                        sb.AppendFormat("{0}-{1}", xtn.AreaCityCode, xtn.TelephoneNumber.Value);
                    }
                    else
                        sb.AppendFormat("{0}-{1}", xtn.AreaCityCode, xtn.LocalNumber);

                    if (xtn.Extension.Value != null)
                        sb.AppendFormat(";ext={0}", xtn.Extension);
                }
                catch
                {
                    // ignored
                }

                if (sb.ToString().EndsWith("tel:") || sb.ToString() == "tel:-")
                    retVal.Value = "tel:" + xtn.AnyText.Value;
                else
                    retVal.Value = sb.ToString();
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

                retVal.Value = sb.ToString();
            }

            // Use code conversion
            var use = Guid.Empty;

            if (!string.IsNullOrEmpty(xtn.TelecommunicationUseCode.Value))
            {
                var concept = ApplicationContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(xtn.TelecommunicationUseCode.Value, TelecomUseCodeSystem).FirstOrDefault();

                if (concept == null)
                    throw new ArgumentException($"Telecom use code {xtn.TelecommunicationUseCode.Value} not known");

                use = concept.Key.Value;
            }

            retVal.AddressUseKey = use;

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
        public static Concept ToConcept(this IS me, String domain)
        {
            return ApplicationContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(me.Value, domain).FirstOrDefault();
        }

        /// <summary>
        /// Convert a simple string to a concept in the specified domain
        /// </summary>
        public static Concept ToConcept(this ID me, String domain)
        {
            return ApplicationContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(me.Value, domain).FirstOrDefault();
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
            var termService = ApplicationContext.Current.GetService<IConceptRepositoryService>();
            List<Concept> retval = new List<Concept>();

            foreach (var code in me)
            {
                Concept concept = null;
                if (!String.IsNullOrEmpty(preferredDomain))
                    concept = termService.FindConceptsByReferenceTerm(code.Identifier.Value, preferredDomain).FirstOrDefault();
                if (concept == null)
                    concept = termService.FindConceptsByReferenceTerm(code.Identifier.Value, code.NameOfCodingSystem.Value).FirstOrDefault();

                if (concept == null && throwIfNotFound)
                    throw new KeyNotFoundException($"Reference term {code.Identifier.Value} not found in {preferredDomain} or {code.NameOfCodingSystem.Value}");
                retval.Add(concept);
            }
            return retval;
        }

        /// <summary>
        /// Convert entity identifier to a CX
        /// </summary>
        public static CX FromModel(this CX me, EntityIdentifier id)
        {
            me.IDNumber.Value = id.Value;
            me.AssigningAuthority.NamespaceID.Value = id.LoadProperty<AssigningAuthority>("Authority").DomainName;
            me.AssigningAuthority.UniversalID.Value = id.Authority.Oid;
            me.AssigningAuthority.UniversalIDType.Value = !String.IsNullOrEmpty(id.Authority.Oid) ? "ISO" : null;

            // Identifier type
            if (id.IdentifierType?.TypeConceptKey.HasValue == true)
            {
                var refTerm = ApplicationContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(id.IdentifierType.TypeConceptKey.Value, IdentifierTypeCodeSystem);
                me.IdentifierTypeCode.Value = refTerm?.Mnemonic;
            }
            return me;
        }

    }
}
