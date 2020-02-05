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
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.Exceptions;
using SanteDB.Core.Security.Services;
using System.Collections;
using SanteDB.Core.Model.Interfaces;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a NK1 segment
    /// </summary>
    public class NK1SegmentHandler : ISegmentHandler
    {

        // Next of kin relationship code system
        private const string RelationshipCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.63";

        // Next of kin relationship types
        private Guid[] m_nextOfKinRelationshipTypes;

        // Configuration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        /// <summary>
        /// NOK relationship types
        /// </summary>
        private Guid[] NextOfKinRelationshipTypes
        {
            get
            {
                if (this.m_nextOfKinRelationshipTypes == null)
                    this.m_nextOfKinRelationshipTypes = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptSetMembers("FamilyMember").Select(c => c.Key.Value).ToArray();
                return this.m_nextOfKinRelationshipTypes;
            }
        }
        /// <summary>
        /// NK1 segment handler ctor
        /// </summary>
        public NK1SegmentHandler()
        {
        }

        /// <summary>
        /// Gets or sets the name of the segment
        /// </summary>
        public string Name => "NK1";

        /// <summary>
        /// Create next of kin relationship
        /// </summary>
        public virtual IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, AssigningAuthority[] exportDomains)
        {
            List<ISegment> retVal = new List<ISegment>();
            var patient = data as Patient;

            foreach (var rel in patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).Where(o => NextOfKinRelationshipTypes.Contains(o.RelationshipTypeKey.Value)))
            {
                var nk1 = context.GetStructure("NK1", context.GetAll("NK1").Length) as NK1;
                var person = rel.LoadProperty<Person>(nameof(EntityRelationship.TargetEntity));

                // HACK: This needs to be fixed on sync
                if (person == null) continue;

                nk1.Relationship.FromModel(rel.LoadProperty<Concept>(nameof(EntityRelationship.RelationshipType)), RelationshipCodeSystem);

                // Map person to NK1
                if (exportDomains == null || exportDomains?.Length == 0 || exportDomains?.Any(d => d.Key == this.m_configuration.LocalAuthority.Key) == true)
                {
                    nk1.GetNextOfKinAssociatedPartySIdentifiers(nk1.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed).FromModel(new EntityIdentifier(this.m_configuration.LocalAuthority, person.Key.ToString()));
                    nk1.GetNextOfKinAssociatedPartySIdentifiers(nk1.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed - 1).IdentifierTypeCode.Value = "PI";
                }

                // Map alternate identifiers
                foreach (var id in person.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)))
                    if (exportDomains == null || exportDomains.Any(e => e.Key == id.AuthorityKey) == true)
                        nk1.GetNextOfKinAssociatedPartySIdentifiers(nk1.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed).FromModel(id);

                // Addresses
                foreach (var addr in person.LoadCollection<EntityAddress>(nameof(Entity.Addresses)))
                    nk1.GetAddress(nk1.AddressRepetitionsUsed).FromModel(addr);

                // Names
                foreach (var en in person.LoadCollection<EntityName>(nameof(Entity.Names)))
                    nk1.GetName(nk1.NameRepetitionsUsed).FromModel(en);

                // Date of birth
                if (person.DateOfBirth.HasValue)
                {
                    switch (person.DateOfBirthPrecision ?? DatePrecision.Day)
                    {
                        case DatePrecision.Year:
                            nk1.DateTimeOfBirth.Time.Set(person.DateOfBirth.Value, "yyyy");
                            break;
                        case DatePrecision.Month:
                            nk1.DateTimeOfBirth.Time.Set(person.DateOfBirth.Value, "yyyyMM");
                            break;
                        case DatePrecision.Day:
                            nk1.DateTimeOfBirth.Time.Set(person.DateOfBirth.Value, "yyyyMMdd");
                            break;
                    }
                }

                // Telecoms
                foreach (var tel in person.LoadCollection<EntityTelecomAddress>(nameof(Entity.Telecoms)))
                {
                    if (tel.AddressUseKey.GetValueOrDefault() == AddressUseKeys.WorkPlace)
                        nk1.GetBusinessPhoneNumber(nk1.BusinessPhoneNumberRepetitionsUsed).FromModel(tel);
                    else
                        nk1.GetPhoneNumber(nk1.PhoneNumberRepetitionsUsed).FromModel(tel);
                }

                // Contact extension
                var contactExtension = person.LoadCollection<EntityExtension>(nameof(Entity.Extensions)).FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.ContactRolesExtension);
                if (contactExtension != null)
                {
                    var existingValue = (contactExtension.ExtensionValue as dynamic);
                    var roles = existingValue.roles as IEnumerable<dynamic>;
                    if (roles != null)
                    {
                        var contact = Enumerable.FirstOrDefault<dynamic>(roles, o => o.patientKey == patient.Key)?.contact;
                        if (!String.IsNullOrEmpty(contact?.ToString()))
                            nk1.ContactRole.Identifier.Value = contact;
                        else if (patient.Tags.Any(o => o.TagKey == "$alt.keys")) // might be a composite
                        {
                            var altKeys = patient.Tags.FirstOrDefault(o => o.TagKey == "$alt.keys")?.Value.Split(';').Select(o => Guid.Parse(o));
                            contact = Enumerable.FirstOrDefault<dynamic>(roles, o => altKeys.Contains((Guid)o.patientKey))?.contact;
                            if (!String.IsNullOrEmpty(contact?.ToString()))
                                nk1.ContactRole.Identifier.Value = contact;
                        }
                    }
                }

                // Load relationships
                var relationships = person.LoadCollection<EntityRelationship>(nameof(Entity.Relationships));

                // Citizenships
                var citizenships = relationships.Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Citizen);
                foreach (var itm in citizenships)
                {
                    var ce = nk1.GetCitizenship(nk1.CitizenshipRepetitionsUsed);
                    var place = itm.LoadProperty<Place>(nameof(EntityRelationship.TargetEntity));
                    ce.Identifier.Value = place.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)).FirstOrDefault(o => o.AuthorityKey == AssigningAuthorityKeys.Iso3166CountryCode)?.Value;
                    ce.Text.Value = place.LoadCollection<EntityName>(nameof(Entity.Names)).FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>(nameof(EntityName.Component)).FirstOrDefault()?.Value;
                }

                // Language of communication
                var lang = person.LoadCollection<PersonLanguageCommunication>(nameof(Person.LanguageCommunication)).FirstOrDefault(o => o.IsPreferred);
                if (lang != null)
                    nk1.PrimaryLanguage.Identifier.Value = lang.LanguageCode;

                // Protected?
                if (ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicyInstance(person, DataPolicyIdentifiers.RestrictedInformation) != null)
                    nk1.ProtectionIndicator.Value = "Y";
                else
                    nk1.ProtectionIndicator.Value = "N";

                retVal.Add(nk1);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// Parse the Next Of Kin Data
        /// </summary>
        public virtual IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {

            // Cast segment
            var nk1Segment = segment as NK1;
            var fieldNo = 0;
            // Person persistence service
            var personService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Person>>();

            try
            {
                // Patient to which the parsing belongs
                var patient = context.OfType<Patient>().FirstOrDefault();
                if (patient == null)
                    throw new InvalidOperationException("NK1 Requires PID segment to be processed");

                // Next of kin is a person
                Person retVal = new Person() { Key = Guid.NewGuid() };
                EntityRelationship retValRelation = new EntityRelationship(EntityRelationshipTypeKeys.NextOfKin, retVal.Key) { SourceEntityKey = patient.Key };
                bool foundByKey = false;

                // Look for existing person
                fieldNo = 33;
                foreach (var id in nk1Segment.GetNextOfKinAssociatedPartySIdentifiers())
                {
                    var idnumber = id.IDNumber.Value;
                    AssigningAuthority authority = null;
                    try
                    {
                        authority = id.AssigningAuthority.ToModel();
                    }
                    catch (Exception e)
                    {
                        throw new HL7ProcessingException(e.Message, "NK1", nk1Segment.SetIDNK1.Value, 33, 3, e);
                    }
                    Guid idguid = Guid.Empty;
                    int tr = 0;
                    Person found = null;
                    if (authority.Key == this.m_configuration.LocalAuthority.Key)
                        found = personService.Get(Guid.Parse(id.IDNumber.Value), null, true, AuthenticationContext.SystemPrincipal);
                    else if (authority?.IsUnique == true)
                        found = personService.Query(o => o.Identifiers.Any(i => i.Value == idnumber &&
                            i.Authority.Key == authority.Key), 0, 1, out tr, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    if (found != null)
                    {
                        retVal = found;
                        foundByKey = true;
                        // Existing relationship?
                        retValRelation = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(r => r.SourceEntityKey == patient.Key.Value && r.TargetEntityKey == retVal.Key.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault() ?? retValRelation;
                        break;
                    }
                }

                // Relationship type
                fieldNo = 3;
                if (!nk1Segment.Relationship.IsEmpty())
                    retValRelation.RelationshipTypeKey = nk1Segment.Relationship.ToModel(RelationshipCodeSystem)?.Key;

                // Some relationships only allow one person, we should update them 
                var existingNokRel = patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).FirstOrDefault(o => o.RelationshipTypeKey == retValRelation.RelationshipTypeKey);
                if (existingNokRel != null)
                {
                    retValRelation = existingNokRel;
                    if (!foundByKey) // We didn't actually resolve anyone by current key so we should try to find them in the DB
                    {
                        retVal = context.FirstOrDefault(o => o.Key == existingNokRel.TargetEntityKey) as Person;
                        // Mother isn't in context, load
                        if (retVal == null)
                            retVal = personService.Get(existingNokRel.TargetEntityKey.Value, null, true, AuthenticationContext.SystemPrincipal);
                        if (retVal == null)
                            throw new InvalidOperationException("Cannot locate described NOK entity on patient record");

                        // IF the person is a PATIENT and not a PERSON we will not update them - too dangerous - ignore the NOK entry
                        if (retVal is Patient && !foundByKey)
                            return new IdentifiedData[0];
                    }
                }

                fieldNo = 2;
                // Names
                if (nk1Segment.NameRepetitionsUsed > 0)
                    retVal.Names.AddRange(nk1Segment.GetName().ToModel().ToList().Where(n => !retVal.Names.Any(e => e.SemanticEquals(n))));

                // Address
                fieldNo = 4;
                if (nk1Segment.AddressRepetitionsUsed > 0)
                    retVal.Addresses = nk1Segment.GetAddress().ToModel().ToList();

                // Phone numbers 
                fieldNo = 5;
                if (nk1Segment.PhoneNumberRepetitionsUsed > 0)
                    retVal.Telecoms = nk1Segment.GetPhoneNumber().ToModel().ToList();

                // Context role, when the person should be contact
                fieldNo = 7;
                if (!nk1Segment.ContactRole.IsEmpty())
                {
                    var contactExtension = retVal.Extensions.FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.ContactRolesExtension);

                    if (contactExtension == null)
                        contactExtension = new EntityExtension(ExtensionTypeKeys.ContactRolesExtension, typeof(DictionaryExtensionHandler), new
                        {
                            roles = new[]
                            {
                                new {
                                    patientKey = patient.Key.Value,
                                    contact = nk1Segment.ContactRole.Identifier.Value
                                }
                            }.ToList()
                        });
                    else
                    {
                        var existingValue = (contactExtension.ExtensionValue as dynamic);
                        existingValue.roles.Add(new { patientKey = patient.Key.Value, contact = nk1Segment.ContactRole.Identifier.Value });
                    }
                    retVal.Extensions.Add(contactExtension);
                }

                fieldNo = 16;
                if (!nk1Segment.DateTimeOfBirth.IsEmpty())
                {
                    retVal.DateOfBirth = nk1Segment.DateTimeOfBirth.ToModel();
                    retVal.DateOfBirthPrecision = nk1Segment.DateTimeOfBirth.ToDatePrecision();
                }

                // Citizenship
                fieldNo = 19;
                if (nk1Segment.CitizenshipRepetitionsUsed > 0)
                {
                    foreach (var cit in nk1Segment.GetCitizenship())
                    {
                        var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value && i.Authority.Key == AssigningAuthorityKeys.Iso3166CountryCode), AuthenticationContext.SystemPrincipal);
                        if (places.Count() == 1)
                            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                    }
                }

                fieldNo = 20;
                if (!nk1Segment.PrimaryLanguage.IsEmpty())
                    retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(nk1Segment.PrimaryLanguage.Identifier.Value.ToLower(), true)
                };

                // Privacy code
                fieldNo = 23;
                if (!nk1Segment.ProtectionIndicator.IsEmpty())
                {
                    var pip = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityPolicy>>();
                    if (nk1Segment.ProtectionIndicator.Value == "Y")
                        retVal.AddPolicy(DataPolicyIdentifiers.RestrictedInformation);
                    else if (nk1Segment.ProtectionIndicator.Value == "N")
                        retVal.Policies.Clear();
                    else
                        throw new ArgumentOutOfRangeException($"Protection indicator {nk1Segment.ProtectionIndicator.Value} is invalid");
                }


                // Associated person identifiers
                fieldNo = 33;
                if (nk1Segment.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed > 0)
                {
                    retVal.Identifiers.AddRange(nk1Segment.GetNextOfKinAssociatedPartySIdentifiers().ToModel().ToList().Where(i => !retVal.Identifiers.Any(e => e.SemanticEquals(i))));
                }

                // Find the existing relationship on the patient

                patient.Relationships.RemoveAll(o => o.SourceEntityKey == retValRelation.SourceEntityKey && o.TargetEntityKey == retValRelation.TargetEntityKey);
                patient.Relationships.Add(retValRelation);

                if (!context.Contains(retVal))
                    return new IdentifiedData[] { retVal };
                else
                    return new IdentifiedData[0];

            }
            catch (HL7ProcessingException) // Just re-throw
            {
                throw;
            }
            catch (HL7DatatypeProcessingException e)
            {
                throw new HL7ProcessingException("Error processing NK1 segment", "NK1", nk1Segment.SetIDNK1.Value, fieldNo, e.Component, e);
            }
            catch (Exception e)
            {
                throw new HL7ProcessingException("Error processing NK1 segment", "NK1", nk1Segment.SetIDNK1.Value, fieldNo, 1, e);
            }
        }

    }
}
