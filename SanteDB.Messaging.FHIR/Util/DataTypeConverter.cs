/*
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
using Hl7.Fhir.Model;
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Represents a data type converter.
    /// </summary>
    public static class DataTypeConverter
    {
        /// <summary>
        /// The trace source.
        /// </summary>
        private static readonly Tracer traceSource = new Tracer(FhirConstants.TraceSourceName);

        /// <summary>
        /// Creates a FHIR reference.
        /// </summary>
        /// <typeparam name="TResource">The type of the t resource.</typeparam>
        /// <param name="targetEntity">The target entity.</param>
        /// <returns>Returns a reference instance.</returns>
        public static ResourceReference CreateVersionedReference<TResource>(IVersionedEntity targetEntity, RestOperationContext context)
            where TResource : DomainResource, new()
        {
            if (targetEntity == null)
                throw new ArgumentNullException(nameof(targetEntity));
            else if (context == null)
                throw new ArgumentNullException(nameof(context));

            var fhirType = Hl7.Fhir.Utility.EnumUtility.ParseLiteral<ResourceType>(typeof(TResource).Name);

            var refer = new ResourceReference($"/{fhirType}/{targetEntity.Key}/_history/{targetEntity.VersionKey}");

            refer.Display = targetEntity.ToString();
            return refer;
        }

        /// <summary>
        /// Creates a FHIR reference.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <param name="targetEntity">The target entity.</param>
        /// <returns>Returns a reference instance.</returns>
        public static ResourceReference CreateNonVersionedReference<TResource>(IIdentifiedEntity targetEntity, RestOperationContext context) where TResource : DomainResource, new()
        {
            if (targetEntity == null)
                throw new ArgumentNullException(nameof(targetEntity));
            else if (context == null)
                throw new ArgumentNullException(nameof(context));

            var fhirType = Hl7.Fhir.Utility.EnumUtility.ParseLiteral<ResourceType>(typeof(TResource).Name);

            var refer = new ResourceReference($"/{fhirType}/{targetEntity.Key}");
            refer.Display = targetEntity.ToString();
            return refer;

        }

        /// <summary>
        /// To quantity 
        /// </summary>
        public static SimpleQuantity ToQuantity(decimal? quantity, Concept unitConcept)
        {
            return new SimpleQuantity()
            {
                Value = quantity,
                Unit = DataTypeConverter.ToFhirCodeableConcept(unitConcept, "http://hl7.org/fhir/sid/ucum")?.GetCoding().Code
            };
        }

        /// <summary>
        /// Creates a FHIR reference.
        /// </summary>
        /// <typeparam name="TResource">The type of the resource.</typeparam>
        /// <returns>Returns a reference instance.</returns>
        public static ResourceReference CreateNonVersionedReference<TResource>(Guid? targetKey, RestOperationContext context) where TResource : DomainResource, new()
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var fhirType = Hl7.Fhir.Utility.EnumUtility.ParseLiteral<ResourceType>(typeof(TResource).Name);

            var refer = new ResourceReference($"/{fhirType}/{targetKey}");
            return refer;

        }

        /// <summary>
        /// Creates the resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the t resource.</typeparam>
        /// <param name="resource">The resource.</param>
        /// <returns>TResource.</returns>
        public static TResource CreateResource<TResource>(IVersionedEntity resource, RestOperationContext context) where TResource : Resource, new()
        {
            var retVal = new TResource();

            // Add annotations 
            retVal.Id = resource.Key.ToString();
            retVal.VersionId = resource.VersionKey.ToString();

            // metadata
            retVal.Meta = new Meta()
            {
                LastUpdated = (resource as IdentifiedData).ModifiedOn.DateTime,
                VersionId = resource.VersionKey?.ToString(),
            };

            if (resource is ITaggable taggable)
            {

                retVal.Meta.Tag = taggable.Tags.Where(tag => !tag.TagKey.StartsWith("$")).Select(tag => new Coding("http://santedb.org/fhir/tags", $"{tag.TagKey}:{tag.Value}")).ToList();
                foreach (var tag in taggable.Tags)
                {
                    retVal.AddAnnotation(tag);
                }
            }

            // TODO: Configure this namespace / coding scheme
            if (resource is ISecurable securable)
            {
                retVal.Meta.Security = securable?.Policies?.Where(o => o.GrantType == Core.Model.Security.PolicyGrantType.Grant).Select(o => new Coding("http://santedb.org/security/policy", o.Policy.Oid)).ToList();
                retVal.Meta.Security.Add(new Coding("http://santedb.org/security/policy", PermissionPolicyIdentifiers.ReadClinicalData));

            }

            if (retVal is Hl7.Fhir.Model.IExtendable fhirExtendable)
            {
                if (resource is Core.Model.Interfaces.IExtendable extendableModel)
                {
                    // TODO: Do we want to expose all internal extensions as external ones? Or do we just want to rely on the IFhirExtensionHandler?
                    fhirExtendable.Extension = extendableModel?.Extensions.Where(o => o.ExtensionTypeKey != ExtensionTypeKeys.JpegPhotoExtension).Select(o => DataTypeConverter.ToExtension(o, context)).ToList();
                }

                fhirExtendable.Extension = fhirExtendable.Extension.Union(ProfileUtil.CreateExtensions(resource, retVal.ResourceType)).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// To FHIR date
        /// </summary>
        public static Date ToFhirDate(DateTime? date)
        {
            if (date.HasValue)
                return new Date(date.Value.Year, date.Value.Month, date.Value.Day);
            else
                return null;
        }

        /// <summary>
        /// Convert two date ranges to a period
        /// </summary>
        public static Period ToPeriod(DateTimeOffset? startTime, DateTimeOffset? stopTime)
        {
            return new Period(
                startTime.HasValue ? new FhirDateTime(startTime.Value) : null,
                stopTime.HasValue ? new FhirDateTime(stopTime.Value) : null
                );
        }

        /// <summary>
        /// Converts an <see cref="FhirExtension"/> instance to an <see cref="ActExtension"/> instance.
        /// </summary>
        /// <param name="fhirExtension">The FHIR extension.</param>
        /// <returns>Returns the converted act extension instance.</returns>
        /// <exception cref="System.ArgumentNullException">fhirExtension - Value cannot be null</exception>
        public static ActExtension ToActExtension(Extension fhirExtension)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR extension");

            var extension = new ActExtension();

            if (fhirExtension == null)
            {
                throw new ArgumentNullException(nameof(fhirExtension), "Value cannot be null");
            }

            var extensionTypeService = ApplicationServiceContext.Current.GetService<IExtensionTypeRepository>();

            extension.ExtensionType = extensionTypeService.Get(new Uri(fhirExtension.Url));
            //extension.ExtensionValue = fhirExtension.Value;
            if (extension.ExtensionType.ExtensionHandler == typeof(DecimalExtensionHandler))
                extension.ExtensionValue = (fhirExtension.Value as FhirDecimal).Value;
            else if (extension.ExtensionType.ExtensionHandler == typeof(StringExtensionHandler))
                extension.ExtensionValue = (fhirExtension.Value as FhirString).Value;
            else if (extension.ExtensionType.ExtensionHandler == typeof(DateExtensionHandler))
                extension.ExtensionValue = (fhirExtension.Value as FhirDateTime).Value;
            // TODO: Implement binary incoming extensions
            //else if(extension.ExtensionType.ExtensionHandler == typeof(BinaryExtensionHandler))
            //    extension.ExtensionValueXml = (fhirExtension.Value as FhirBase64Binary).Value;
            else
                throw new NotImplementedException($"Extension type is not understood");
            // Now will 
            return extension;
        }

        /// <summary>
        /// Converts an <see cref="FhirExtension"/> instance to an <see cref="ActExtension"/> instance.
        /// </summary>
        /// <param name="fhirExtension">The FHIR extension.</param>
        /// <returns>Returns the converted act extension instance.</returns>
        /// <exception cref="System.ArgumentNullException">fhirExtension - Value cannot be null</exception>
        public static EntityExtension ToEntityExtension(Extension fhirExtension, IdentifiedData context)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR extension");

            var extension = new EntityExtension();

            if (fhirExtension == null)
            {
                throw new ArgumentNullException(nameof(fhirExtension), "Value cannot be null");
            }

            // First attempt to parse the extension using a parser
            if (!fhirExtension.TryApplyExtension(context))
            {
                var extensionTypeService = ApplicationServiceContext.Current.GetService<IExtensionTypeRepository>();

                extension.ExtensionType = extensionTypeService.Get(new Uri(fhirExtension.Url));
                if (extension.ExtensionType == null)
                    return null;

                //extension.ExtensionValue = fhirExtension.Value;
                if (extension.ExtensionType.ExtensionHandler == typeof(DecimalExtensionHandler))
                    extension.ExtensionValue = (fhirExtension.Value as FhirDecimal).Value;
                else if (extension.ExtensionType.ExtensionHandler == typeof(StringExtensionHandler))
                    extension.ExtensionValue = (fhirExtension.Value as FhirString).Value;
                else if (extension.ExtensionType.ExtensionHandler == typeof(DateExtensionHandler))
                    extension.ExtensionValue = (fhirExtension.Value as FhirDateTime).Value;
                // TODO: Implement binary incoming extensions
                else if (extension.ExtensionType.ExtensionHandler == typeof(BinaryExtensionHandler))
                    extension.ExtensionValueXml = (fhirExtension.Value as Base64Binary).Value;
                else
                    throw new NotImplementedException($"Extension type is not understood");

                // Now will 
                return extension;
            }
            return null;
        }

        /// <summary>
        /// Convert to language of communication
        /// </summary>
        public static PersonLanguageCommunication ToLanguageCommunication(Patient.CommunicationComponent lang)
        {
            return new PersonLanguageCommunication(lang.Language.GetCoding().Code, lang.Preferred.GetValueOrDefault());
        }

        /// <summary>
        /// Convert to language of communication
        /// </summary>
        public static Patient.CommunicationComponent ToFhirCommunicationComponent(PersonLanguageCommunication lang)
        {
            return new Patient.CommunicationComponent()
            {
                Language = new CodeableConcept("urn:ietf:bcp:47", lang.LanguageCode),
                Preferred = lang.IsPreferred
            };
        }

        /// <summary>
        /// Converts a <see cref="FhirIdentifier"/> instance to an <see cref="ActIdentifier"/> instance.
        /// </summary>
        /// <param name="fhirIdentifier">The FHIR identifier.</param>
        /// <returns>Returns the converted act identifier instance.</returns>
        public static ActIdentifier ToActIdentifier(Identifier fhirIdentifier)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR identifier");

            if (fhirIdentifier == null)
            {
                return null;
            }

            ActIdentifier retVal;

            if (fhirIdentifier.System != null)
            {
                retVal = new ActIdentifier(DataTypeConverter.ToAssigningAuthority(fhirIdentifier.System), fhirIdentifier.Value);
            }
            else
            {
                throw new InvalidOperationException("Identifier must carry a coding system");
            }

            // TODO: Fill in use
            return retVal;
        }

        /// <summary>
        /// Convert to assigning authority
        /// </summary>
        /// <param name="fhirSystem">The FHIR system.</param>
        /// <returns>AssigningAuthority.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to locate service</exception>
        public static AssigningAuthority ToAssigningAuthority(FhirUri fhirSystem)
        {
            if (fhirSystem == null)
            {
                return null;
            }

            return DataTypeConverter.ToAssigningAuthority(fhirSystem.Value);
        }

        /// <summary>
        /// Convert to assigning authority
        /// </summary>
        public static AssigningAuthority ToAssigningAuthority(String fhirSystem)
        {

            traceSource.TraceEvent(EventLevel.Verbose, "Mapping assigning authority");

            var oidRegistrar = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            var oid = oidRegistrar.Get(fhirSystem);

            if (oid == null)
                throw new KeyNotFoundException($"Could not find identity domain {fhirSystem}");
            return oid;
        }

        /// <summary>
        /// Converts a <see cref="ReferenceTerm"/> instance to a <see cref="FhirCoding"/> instance.
        /// </summary>
        /// <param name="referenceTerm">The reference term.</param>
        /// <returns>Returns a FHIR coding instance.</returns>
        public static Coding ToCoding(ReferenceTerm referenceTerm)
        {
            if (referenceTerm == null)
            {
                return null;
            }

            traceSource.TraceEvent(EventLevel.Verbose, "Mapping reference term");

            var cs = referenceTerm.LoadProperty<Core.Model.DataTypes.CodeSystem>(nameof(ReferenceTerm.CodeSystem));
            return new Coding(cs.Url ?? String.Format("urn:oid:{0}", cs.Oid), referenceTerm.Mnemonic);
        }

        /// <summary>
        /// Act Extension to Fhir Extension
        /// </summary>
        public static Extension ToExtension(IModelExtension ext, RestOperationContext context)
        {

            var extensionTypeService = ApplicationServiceContext.Current.GetService<IExtensionTypeRepository>();
            var eType = extensionTypeService.Get(ext.ExtensionTypeKey);

            var retVal = new Extension()
            {
                Url = eType.Name
            };

            if (ext.Value is Decimal || eType.ExtensionHandler == typeof(DecimalExtensionHandler))
                retVal.Value = new FhirDecimal((Decimal)(ext.Value ?? new DecimalExtensionHandler().DeSerialize(ext.Data)));
            else if (ext.Value is String || eType.ExtensionHandler == typeof(StringExtensionHandler))
                retVal.Value = new FhirString((String)(ext.Value ?? new StringExtensionHandler().DeSerialize(ext.Data)));
            else if (ext.Value is Boolean || eType.ExtensionHandler == typeof(BooleanExtensionHandler))
                retVal.Value = new FhirBoolean((bool)(ext.Value ?? new BooleanExtensionHandler().DeSerialize(ext.Data)));
            else if (ext.Value is Concept concept)
                retVal.Value = ToFhirCodeableConcept(concept);
            else if (ext.Value is SanteDB.Core.Model.Roles.Patient patient)
                retVal.Value = DataTypeConverter.CreateVersionedReference<Patient>(patient, context);
            else
                retVal.Value = new Base64Binary(ext.Data);
            return retVal;
        }

        /// <summary>
        /// Gets the concept via the codeable concept
        /// </summary>
        /// <param name="codeableConcept">The codeable concept.</param>
        /// <returns>Returns a concept.</returns>
        public static Concept ToConcept(CodeableConcept codeableConcept)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping codeable concept");
            var retVal = codeableConcept?.Coding.Select(o => DataTypeConverter.ToConcept(o)).FirstOrDefault(o => o != null);
            if (retVal == null)
                throw new ConstraintException($"Can't find any reference term mappings from '{codeableConcept.Coding.FirstOrDefault().Code}' in {codeableConcept.Coding.FirstOrDefault().System} to a Concept");
            return retVal;
        }

        /// <summary>
        /// Convert from FHIR coding to concept
        /// </summary>
        /// <param name="coding">The coding.</param>
        /// <param name="defaultSystem">The default system.</param>
        /// <returns>Returns a concept which matches the given code and code system or null if no concept is found.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Unable to locate service
        /// or
        /// Coding must have system attached
        /// </exception>
        public static Concept ToConcept(Coding coding, String defaultSystem = null)
        {
            if (coding == null)
            {
                return null;
            }

            return ToConcept(coding.Code, coding.System ?? defaultSystem);
        }

        /// <summary>
        /// Convert to concept
        /// </summary>
        public static Concept ToConcept(String code, String system)
        {

            var conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();

            if (system == null)
            {
                throw new InvalidOperationException("Coding must have system attached");
            }

            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR coding");

            // Lookup
            return conceptService.FindConceptsByReferenceTerm(code, system).FirstOrDefault();
        }

        /// <summary>
        /// Converts a <see cref="FhirCode{T}"/> instance to a <see cref="Concept"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="code">The code.</param>
        /// <param name="system">The system.</param>
        /// <returns>Concept.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// code - Value cannot be null
        /// or
        /// system - Value cannot be null
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Unable to locate service</exception>
        public static Concept ToConcept<T>(String code, string system)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code), "Value cannot be null");
            }

            if (system == null)
            {
                throw new ArgumentNullException(nameof(system), "Value cannot be null");
            }

            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR code");

            var retVal = ToConcept(new Coding(system, code));
            if (retVal == null)
                throw new ConstraintException($"Could not find concept with reference term '{code}' in {system}");
            return retVal;
        }

        /// <summary>
        /// Converts an <see cref="FhirAddress"/> instance to an <see cref="EntityAddress"/> instance.
        /// </summary>
        /// <param name="fhirAddress">The FHIR address.</param>
        /// <returns>Returns an entity address instance.</returns>
        public static EntityAddress ToEntityAddress(Address fhirAddress)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR address");

            var address = new EntityAddress
            {
                AddressUseKey = ToConcept(fhirAddress.Use?.ToString().ToLower() ?? "home", "http://hl7.org/fhir/address-use")?.Key
            };

            if (!String.IsNullOrEmpty(fhirAddress.City))
            {
                address.Component.Add(new EntityAddressComponent(AddressComponentKeys.City, fhirAddress.City));
            }

            if (!String.IsNullOrEmpty(fhirAddress.Country))
            {
                address.Component.Add(new EntityAddressComponent(AddressComponentKeys.Country, fhirAddress.Country));
            }

            if (fhirAddress.Line?.Any() == true)
            {
                address.Component.AddRange(fhirAddress.Line.Select(a => new EntityAddressComponent(AddressComponentKeys.AddressLine, a)));
            }

            if (!String.IsNullOrEmpty(fhirAddress.State))
            {
                address.Component.Add(new EntityAddressComponent(AddressComponentKeys.State, fhirAddress.State));
            }

            if (!String.IsNullOrEmpty(fhirAddress.PostalCode))
            {
                address.Component.Add(new EntityAddressComponent(AddressComponentKeys.PostalCode, fhirAddress.PostalCode));
            }

            if (!String.IsNullOrEmpty(fhirAddress.District))
            {
                address.Component.Add(new EntityAddressComponent(AddressComponentKeys.County, fhirAddress.District));
            }

            return address;
        }

        /// <summary>
        /// Convert a FhirIdentifier to an identifier
        /// </summary>
        /// <param name="fhirId">The fhir identifier.</param>
        /// <returns>Returns an entity identifier instance.</returns>
        public static EntityIdentifier ToEntityIdentifier(Identifier fhirId)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR identifier");

            if (fhirId == null)
            {
                return null;
            }

            EntityIdentifier retVal;

            if (fhirId.System != null)
            {
                retVal = new EntityIdentifier(DataTypeConverter.ToAssigningAuthority(fhirId.System), fhirId.Value);
            }
            else
            {
                throw new InvalidOperationException("Identifier must carry a coding system");
            }

            if (fhirId.Period != null)
            {
                if (fhirId.Period.StartElement != null)
                    retVal.IssueDate = fhirId.Period.StartElement.ToDateTimeOffset().DateTime;
                if (fhirId.Period.EndElement != null)
                    retVal.ExpiryDate = fhirId.Period.EndElement.ToDateTimeOffset().DateTime;
            }
            // TODO: Fill in use
            return retVal;
        }

        /// <summary>
        /// Converts a <see cref="FhirHumanName" /> instance to an <see cref="EntityName" /> instance.
        /// </summary>
        /// <param name="fhirHumanName">The name of the human.</param>
        /// <returns>Returns an entity name instance.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to locate service</exception>
        public static EntityName ToEntityName(HumanName fhirHumanName)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR human name");

            var name = new EntityName
            {
                NameUseKey = ToConcept(fhirHumanName.Use?.ToString()?.ToLower() ?? "official", "http://hl7.org/fhir/name-use")?.Key
            };

            if (fhirHumanName.Family != null)
            {
                name.Component.Add(new EntityNameComponent(NameComponentKeys.Family, fhirHumanName.Family));
            }

            name.Component.AddRange(fhirHumanName.Given.Select(g => new EntityNameComponent(NameComponentKeys.Given, g)));
            name.Component.AddRange(fhirHumanName.Prefix.Select(p => new EntityNameComponent(NameComponentKeys.Prefix, p)));
            name.Component.AddRange(fhirHumanName.Suffix.Select(s => new EntityNameComponent(NameComponentKeys.Suffix, s)));

            return name;
        }

        /// <summary>
        /// Converts a <see cref="PatientContact"/> instance to an <see cref="EntityRelationship"/> instance.
        /// </summary>
        /// <param name="patientContact">The patient contact.</param>
        /// <returns>Returns the mapped entity relationship instance..</returns>
        public static EntityRelationship ToEntityRelationship(Patient.ContactComponent patientContact)
        {

            var retVal = new EntityRelationship();
            retVal.TargetEntity = new Core.Model.Entities.Person()
            {
                Addresses = new List<EntityAddress>() { DataTypeConverter.ToEntityAddress(patientContact.Address) },
                CreationTime = DateTimeOffset.Now,
                // TODO: Gender (after refactor)
                Names = new List<EntityName>() { DataTypeConverter.ToEntityName(patientContact.Name) },
                Telecoms = patientContact.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).OfType<EntityTelecomAddress>().ToList()
            };
            retVal.TargetEntity.Extensions = patientContact.Extension.Select(o => DataTypeConverter.ToEntityExtension(o, retVal.TargetEntity)).OfType<EntityExtension>().ToList();
            retVal.RelationshipType = DataTypeConverter.ToConcept(patientContact.Relationship.FirstOrDefault());
            return retVal;
        }

        /// <summary>
        /// Converts a <see cref="FhirTelecom"/> instance to an <see cref="EntityTelecomAddress"/> instance.
        /// </summary>
        /// <param name="fhirTelecom">The telecom.</param>
        /// <returns>Returns an entity telecom address.</returns>
        public static EntityTelecomAddress ToEntityTelecomAddress(ContactPoint fhirTelecom)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping FHIR telecom");

            if (fhirTelecom.Value != null)
                return new EntityTelecomAddress
                {
                    Value = fhirTelecom.Value,
                    AddressUseKey = ToConcept(fhirTelecom.Use?.ToString()?.ToLower() ?? "temp", "http://hl7.org/fhir/contact-point-use")?.Key
                };
            return null;
        }

        /// <summary>
        /// Converts an <see cref="EntityAddress"/> instance to a <see cref="FhirAddress"/> instance.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>Returns a FHIR address.</returns>
        public static Address ToFhirAddress(EntityAddress address)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping entity address");

            if (address == null) return null;

            // Return value
            var retVal = new Address()
            {
                Use = DataTypeConverter.ToFhirEnumeration<Address.AddressUse>(address.LoadProperty<Concept>(nameof(EntityAddress.AddressUse)), "http://hl7.org/fhir/address-use"),
                Line = new List<String>()
            };

            // Process components
            foreach (var com in address.LoadCollection<EntityAddressComponent>(nameof(EntityAddress.Component)))
            {
                if (com.ComponentTypeKey == AddressComponentKeys.City)
                    retVal.City = com.Value;
                else if (com.ComponentTypeKey == AddressComponentKeys.Country)
                    retVal.Country = com.Value;
                else if (com.ComponentTypeKey == AddressComponentKeys.AddressLine ||
                    com.ComponentTypeKey == AddressComponentKeys.StreetAddressLine)
                    retVal.LineElement.Add(new FhirString(com.Value));
                else if (com.ComponentTypeKey == AddressComponentKeys.State)
                    retVal.State = com.Value;
                else if (com.ComponentTypeKey == AddressComponentKeys.PostalCode)
                    retVal.PostalCode = com.Value;
                else if (com.ComponentTypeKey == AddressComponentKeys.County)
                    retVal.District = com.Value;
                else
                {
                    retVal.AddExtension(
                        FhirConstants.SanteDBProfile + "#address-" + com.LoadProperty<Concept>(nameof(EntityAddressComponent.ComponentType)).Mnemonic,
                        new FhirString(com.Value)
                    );
                }
            }

            return retVal;
        }

        /// <summary>
        /// Converts a <see cref="Concept"/> instance to an <see cref="FhirCodeableConcept"/> instance.
        /// </summary>
        /// <param name="concept">The concept to be converted to a <see cref="FhirCodeableConcept"/></param>
        /// <param name="preferredCodeSystem">The preferred code system for the codeable concept</param>
        /// <param name="nullIfNoPreferred">When true, instructs the system to return only code in preferred code system or nothing</param>
        /// <returns>Returns a FHIR codeable concept.</returns>
        public static CodeableConcept ToFhirCodeableConcept(Concept concept, String preferredCodeSystem = null, bool nullIfNoPreferred = false)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping concept");

            if (concept == null)
            {
                return null;
            }

            if (String.IsNullOrEmpty(preferredCodeSystem))
            {
                var refTerms = concept.LoadCollection<ConceptReferenceTerm>(nameof(Concept.ReferenceTerms));
                if (refTerms.Any())
                    return new CodeableConcept
                    {
                        Coding = refTerms.Select(o => DataTypeConverter.ToCoding(o.LoadProperty<ReferenceTerm>(nameof(ConceptReferenceTerm.ReferenceTerm)))).ToList(),
                        Text = concept.LoadCollection<ConceptName>(nameof(Concept.ConceptNames)).FirstOrDefault()?.Name
                    };
                else
                    return new CodeableConcept("http://openiz.org/concept", concept.Mnemonic)
                    {
                        Text = concept.LoadCollection<ConceptName>(nameof(Concept.ConceptNames)).FirstOrDefault()?.Name
                    };
            }
            else
            {
                var codeSystemService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
                var refTerm = codeSystemService.GetConceptReferenceTerm(concept.Key.Value, preferredCodeSystem);
                if (refTerm == null && nullIfNoPreferred)
                    return null; // No code in the preferred system, ergo, we will instead use our own
                else if (refTerm == null)
                    return new CodeableConcept
                    {
                        Coding = concept.LoadCollection<ConceptReferenceTerm>(nameof(Concept.ReferenceTerms)).Select(o => DataTypeConverter.ToCoding(o.LoadProperty<ReferenceTerm>(nameof(ConceptReferenceTerm.ReferenceTerm)))).ToList(),
                        Text = concept.LoadCollection<ConceptName>(nameof(Concept.ConceptNames)).FirstOrDefault()?.Name
                    };
                else
                    return new CodeableConcept
                    {
                        Coding = new List<Coding>() { ToCoding(refTerm) },
                        Text = concept.LoadCollection<ConceptName>(nameof(Concept.ConceptNames)).FirstOrDefault()?.Name
                    };
            }
        }

        /// <summary>
        /// Converts an <see cref="EntityName"/> instance to a <see cref="FhirHumanName"/> instance.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <returns>Returns the mapped FHIR human name.</returns>
        public static HumanName ToFhirHumanName(EntityName entityName)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping entity name");

            if (entityName == null)
            {
                return null;
            }

            // Return value
            var retVal = new HumanName
            {
                Use = DataTypeConverter.ToFhirEnumeration<HumanName.NameUse>(entityName.LoadProperty<Concept>(nameof(EntityName.NameUse)), "http://hl7.org/fhir/name-use")

            };

            // Process components
            foreach (var com in entityName.LoadCollection<EntityNameComponent>(nameof(EntityName.Component)))
            {
                if (string.IsNullOrEmpty(com.Value)) continue;

                if (com.ComponentTypeKey == NameComponentKeys.Given)
                    retVal.GivenElement.Add(new FhirString(com.Value));
                else if (com.ComponentTypeKey == NameComponentKeys.Family)
                    retVal.FamilyElement = new FhirString(com.Value);
                else if (com.ComponentTypeKey == NameComponentKeys.Prefix)
                    retVal.PrefixElement.Add(new FhirString(com.Value));
                else if (com.ComponentTypeKey == NameComponentKeys.Suffix)
                    retVal.SuffixElement.Add(new FhirString(com.Value));
            }

            return retVal;
        }

        /// <summary>
        /// To FHIR enumeration
        /// </summary>
        public static TEnum? ToFhirEnumeration<TEnum>(Concept concept, string codeSystem) where TEnum : struct
        {
            var coding = DataTypeConverter.ToFhirCodeableConcept(concept, codeSystem, true);
            if (coding != null)
                return Hl7.Fhir.Utility.EnumUtility.ParseLiteral<TEnum>(coding.Coding.First().Code, true);
            else
                throw new ConstraintException($"Cannot find FHIR mapping for Concept {concept}");
        }

        /// <summary>
        /// Converts a <see cref="IdentifierBase{TBoundModel}" /> instance to an <see cref="FhirIdentifier" /> instance.
        /// </summary>
        /// <typeparam name="TBoundModel">The type of the bound model.</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns>Returns the mapped FHIR identifier.</returns>
        public static Identifier ToFhirIdentifier<TBoundModel>(IdentifierBase<TBoundModel> identifier) where TBoundModel : VersionedEntityData<TBoundModel>, new()
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping entity identifier");

            if (identifier == null)
            {
                return null;
            }

            var imetaService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            var authority = imetaService.Get(identifier.AuthorityKey.Value);
            var retVal = new Identifier
            {
                System = authority?.Url ?? $"urn:oid:{authority?.Oid}",
                Type = ToFhirCodeableConcept(identifier.LoadProperty<IdentifierType>(nameof(EntityIdentifier.IdentifierType))?.TypeConcept),
                Value = identifier.Value
            };

            if (identifier.ExpiryDate.HasValue || identifier.IssueDate.HasValue)
                retVal.Period = new Period()
                {
                    StartElement = identifier.IssueDate.HasValue ? new FhirDateTime(identifier.IssueDate.Value) : null,
                    EndElement = identifier.ExpiryDate.HasValue ? new FhirDateTime(identifier.ExpiryDate.Value) : null
                };

            return retVal;

        }

        /// <summary>
        /// Converts an <see cref="EntityTelecomAddress"/> instance to <see cref="FhirTelecom"/> instance.
        /// </summary>
        /// <param name="telecomAddress">The telecom address.</param>
        /// <returns>Returns the mapped FHIR telecom.</returns>
        public static ContactPoint ToFhirTelecom(EntityTelecomAddress telecomAddress)
        {
            traceSource.TraceEvent(EventLevel.Verbose, "Mapping entity telecom address");

            return new ContactPoint()
            {
                Use = DataTypeConverter.ToFhirEnumeration<ContactPoint.ContactPointUse>(telecomAddress.AddressUse, "http://hl7.org/fhir/contact-point-use"),
                Value = telecomAddress.IETFValue
            };
        }

    }
}