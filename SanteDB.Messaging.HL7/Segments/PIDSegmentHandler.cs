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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
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

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a segment handler which handles PID segments
    /// </summary>
    public class PIDSegmentHandler : ISegmentHandler
    {

        private const string AdministrativeGenderCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.1";
        private const string RaceCodeSystem = "2.16.840.1.113883.5.5";
        private const string MaritalStatusCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.2";
        private const string ReligionCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.6";
        private const string EthnicGroupCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.189";

        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();


        /// <summary>
        /// Gets the name of the segment
        /// </summary>
        public string Name => "PID";

        /// <summary>
        /// Create the PID segment from data elements
        /// </summary>
        /// <param name="data">The data to be created</param>
        /// <param name="context">The message in which the segment is created</param>
        /// <returns>The segments to add to the messge</returns>
        public virtual IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, AssigningAuthority[] exportDomains)
        {
            var retVal = context.GetStructure("PID") as PID;
            var patient = data as Patient;
            if (patient == null)
                throw new InvalidOperationException($"Cannot convert {data.GetType().Name} to PID");

            // Map patient to PID
            if (exportDomains == null || exportDomains?.Length == 0 || exportDomains?.Any(d => d.Key == this.m_configuration.LocalAuthority.Key) == true)
            {
                retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed).FromModel(new EntityIdentifier(this.m_configuration.LocalAuthority, patient.Key.ToString()));
                retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed - 1).IdentifierTypeCode.Value = "PI";
            }
            // Map alternate identifiers
            foreach (var id in patient.LoadCollection<EntityIdentifier>("Identifiers"))
                if (exportDomains == null || exportDomains.Any(e => e.Key == id.AuthorityKey) == true)
                    retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed).FromModel(id);

            // Addresses
            foreach (var addr in patient.LoadCollection<EntityAddress>("Addresses"))
                retVal.GetPatientAddress(retVal.PatientAddressRepetitionsUsed).FromModel(addr);

            // Names
            foreach (var en in patient.LoadCollection<EntityName>("Names"))
                retVal.GetPatientName(retVal.PatientNameRepetitionsUsed).FromModel(en);

            // Date of birth
            if (patient.DateOfBirth.HasValue)
            {
                switch (patient.DateOfBirthPrecision ?? DatePrecision.Day)
                {
                    case DatePrecision.Year:
                        retVal.DateTimeOfBirth.Time.Set(patient.DateOfBirth.Value, "yyyy");
                        break;
                    case DatePrecision.Month:
                        retVal.DateTimeOfBirth.Time.Set(patient.DateOfBirth.Value, "yyyyMM");
                        break;
                    case DatePrecision.Day:
                        retVal.DateTimeOfBirth.Time.Set(patient.DateOfBirth.Value, "yyyyMMdd");
                        break;
                }
            }

            // Gender
            retVal.AdministrativeSex.FromModel(patient.LoadProperty<Concept>("GenderConcept"), AdministrativeGenderCodeSystem);

            // Deceased date
            if (patient.DeceasedDate.HasValue)
            {
                if (patient.DeceasedDate == DateTime.MinValue)
                    retVal.PatientDeathIndicator.Value = "Y";
                else
                    switch (patient.DeceasedDatePrecision ?? DatePrecision.Day)
                    {
                        case DatePrecision.Year:
                            retVal.PatientDeathDateAndTime.Time.Set(patient.DeceasedDate.Value, "yyyy");
                            break;
                        case DatePrecision.Month:
                            retVal.PatientDeathDateAndTime.Time.Set(patient.DeceasedDate.Value, "yyyyMM");
                            break;
                        case DatePrecision.Day:
                            retVal.PatientDeathDateAndTime.Time.Set(patient.DeceasedDate.Value, "yyyyMMdd");
                            break;
                    }
            }

            // Mother's info
            var motherRelation = patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother);
            if (motherRelation != null)
            {
                var mother = motherRelation.LoadProperty(nameof(EntityRelationship.TargetEntity)) as Person;
                foreach (var nam in mother.LoadCollection<EntityName>(nameof(Entity.Names)).Where(n=>n.NameUseKey == NameUseKeys.MaidenName))
                    retVal.GetMotherSMaidenName(retVal.MotherSMaidenNameRepetitionsUsed).FromModel(nam);
                foreach (var id in mother.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)))
                    retVal.GetMotherSIdentifier(retVal.MotherSIdentifierRepetitionsUsed).FromModel(id);
            }

            // Telecoms
            foreach (var tel in patient.LoadCollection<EntityTelecomAddress>(nameof(Entity.Telecoms)))
            {
                if (tel.AddressUseKey.GetValueOrDefault() == AddressUseKeys.WorkPlace)
                    retVal.GetPhoneNumberBusiness(retVal.PhoneNumberBusinessRepetitionsUsed).FromModel(tel);
                else
                    retVal.GetPhoneNumberHome(retVal.PhoneNumberHomeRepetitionsUsed).FromModel(tel);
            }

            // Load relationships
            var relationships = patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships));
            var participations = patient.LoadCollection<ActParticipation>(nameof(Entity.Participations));

            // Birthplace
            var birthplace = relationships.FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Birthplace);
            if (birthplace != null)
                retVal.BirthPlace.Value = birthplace.LoadCollection<EntityName>(nameof(Entity.Names)).FirstOrDefault()?.LoadCollection<EntityNameComponent>(nameof(EntityName.Component)).FirstOrDefault()?.Value;

            // Citizenships
            var citizenships = relationships.Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Citizen);
            foreach (var itm in citizenships)
            {
                var ce = retVal.GetCitizenship(retVal.CitizenshipRepetitionsUsed);
                var place = itm.LoadProperty<Place>(nameof(EntityRelationship.TargetEntity));
                ce.Identifier.Value = place.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)).FirstOrDefault(o => o.AuthorityKey == AssigningAuthorityKeys.Iso3166CountryCode)?.Value;
                ce.Text.Value = place.LoadCollection<EntityName>(nameof(Entity.Names)).FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>(nameof(EntityName.Component)).FirstOrDefault()?.Value;
            }

            // Account number
            var account = participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Holder && o.LoadProperty<Account>(nameof(ActParticipation.Act)) != null);
            if (account != null)
                retVal.PatientAccountNumber.FromModel(account.Act.Identifiers.FirstOrDefault() ?? new ActIdentifier(this.m_configuration.LocalAuthority, account.Key.ToString()));

            // Marital status
            if (patient.MaritalStatusKey.HasValue)
                retVal.MaritalStatus.FromModel(patient.LoadProperty<Concept>(nameof(Patient.MaritalStatus)), MaritalStatusCodeSystem);

            // Religion
            if (patient.ReligiousAffiliationKey.HasValue)
                retVal.Religion.FromModel(patient.LoadProperty<Concept>(nameof(Patient.ReligiousAffiliation)), ReligionCodeSystem);

            // Ethnic groups
            if (patient.EthnicGroupCodeKey.HasValue)
                retVal.GetEthnicGroup(0).FromModel(patient.LoadProperty<Concept>(nameof(Patient.EthnicGroup)), EthnicGroupCodeSystem);

            // Primary language
            var lang = patient.LoadCollection<PersonLanguageCommunication>(nameof(Patient.LanguageCommunication)).FirstOrDefault(o => o.IsPreferred);
            if (lang != null)
                retVal.PrimaryLanguage.Identifier.Value = lang.LanguageCode;

            return new ISegment[] { retVal };
        }

        /// <summary>
        /// Parse the parse the specified segment into a patient object
        /// </summary>
        /// <param name="segment">The segment to be parsed</param>
        /// <returns>The parsed patient information</returns>
        public virtual IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            var patientService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>();
            var pidSegment = segment as PID;
            int fieldNo = 0;

            try
            {

                Patient retVal = new Patient() { Key = Guid.NewGuid() };
                Person motherEntity = null;

                retVal.CreationAct = context.OfType<ControlAct>().FirstOrDefault();

                // Existing patient?
                if (pidSegment.Message.GetStructureName().StartsWith("ADT"))
                {
                    foreach (var id in pidSegment.GetPatientIdentifierList())
                    {
                        var idnumber = id.IDNumber.Value;
                        AssigningAuthority authority;
                        try
                        {
                            authority = id.AssigningAuthority.ToModel();
                        }
                        catch (Exception e)
                        {
                            throw new HL7ProcessingException("Error processig assigning authority", "PID", pidSegment.SetIDPID.Value, 3, 3, e);
                        }

                        Guid idguid = Guid.Empty;
                        Patient found = null;
                        if (authority.Key == this.m_configuration.LocalAuthority.Key)
                            found = patientService.Get(Guid.Parse(id.IDNumber.Value), null, true, AuthenticationContext.Current.Principal);
                        else if (authority?.IsUnique == true)
                            found = patientService.Query(o => o.Identifiers.Any(i => i.Authority.Key == authority.Key && i.Value == idnumber), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                        if (found != null)
                        {
                            retVal = found;

                            break;
                        }
                    }
                }


                fieldNo = 2;
                if (!pidSegment.PatientID.IsEmpty())
                    retVal.Identifiers.Add(pidSegment.PatientID.ToModel());


                fieldNo = 3;
                if (pidSegment.PatientIdentifierListRepetitionsUsed > 0)
                    retVal.Identifiers.AddRange(
                    pidSegment.GetPatientIdentifierList().ToModel().Where(o => !retVal.Identifiers.Any(i => i.Authority.Key == o.AuthorityKey && i.Value == o.Value)).ToArray()
                );

                // Find the key for the patient 
                var keyId = pidSegment.GetPatientIdentifierList().FirstOrDefault(o => o.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority.DomainName);
                if (keyId != null)
                    retVal.Key = Guid.Parse(keyId.IDNumber.Value);

                if (retVal.Identifiers.Count == 0)
                    throw new HL7ProcessingException("Couldn't understand any patient identity", "PID", pidSegment.SetIDPID.Value, 3, 1);

                fieldNo = 5;
                if (pidSegment.PatientNameRepetitionsUsed > 0)
                    foreach (var itm in pidSegment.GetPatientName())
                    {
                        var model = itm.ToModel();
                        var existing = retVal.Names.FirstOrDefault(o => o.NameUseKey == model.NameUseKey);
                        if (existing == null)
                            retVal.Names.Add(model);
                        else
                            existing.CopyObjectData(model);
                    }

                fieldNo = 21;
                // Mother's maiden name, create a relationship for mother
                if (pidSegment.MotherSMaidenNameRepetitionsUsed > 0 || pidSegment.MotherSIdentifierRepetitionsUsed > 0)
                {
                    // Attempt to find the mother
                    var personPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Person>>();
                    foreach (var id in pidSegment.GetMotherSIdentifier())
                    {
                        AssigningAuthority authority = null;
                        try
                        {
                            authority = id.AssigningAuthority.ToModel();
                        }
                        catch (Exception e)
                        {
                            throw new HL7ProcessingException("Error processing mother's identifiers", "PID", pidSegment.SetIDPID.Value, 21, 3);
                        }

                        if (authority.Key == this.m_configuration.LocalAuthority.Key)
                            motherEntity = personPersistence.Get(Guid.Parse(id.IDNumber.Value), null, true, AuthenticationContext.SystemPrincipal);
                        else if (authority?.IsUnique == true)
                            motherEntity = personPersistence.Query(o => o.Identifiers.Any(i => i.Value == id.IDNumber.Value && i.Authority.Key == authority.Key), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    }

                    fieldNo = 6;
                    // Mother doesn't exist, so add it
                    if (motherEntity == null)
                        motherEntity = new Person()
                        {
                            Key = Guid.NewGuid(),
                            Identifiers = pidSegment.GetMotherSIdentifier().ToModel().ToList(),
                            Names = pidSegment.GetMotherSMaidenName().ToModel(NameUseKeys.MaidenName).ToList(),
                            StatusConceptKey = StatusKeys.New
                        };
                    
                    var existingRelationship = retVal.Relationships.FirstOrDefault(r => r.SourceEntityKey == retVal.Key && r.TargetEntityKey == motherEntity.Key);
                    if (existingRelationship == null)
                    {
                        // Find by mother relationship
                        existingRelationship = retVal.Relationships.FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother);
                        if (existingRelationship == null) // No current mother relationship
                            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Mother, motherEntity.Key));
                        else if (!existingRelationship.LoadProperty<Entity>("TargetEntity").SemanticEquals(motherEntity))
                            existingRelationship.TargetEntityKey = motherEntity.Key;
                    }
                    else
                        existingRelationship.RelationshipTypeKey = EntityRelationshipTypeKeys.Mother;

                }

                // Date/time of birth
                fieldNo = 7;
                if (!pidSegment.DateTimeOfBirth.IsEmpty())
                {
                    retVal.DateOfBirth = pidSegment.DateTimeOfBirth.ToModel();
                    retVal.DateOfBirthPrecision = pidSegment.DateTimeOfBirth.ToDatePrecision();
                }

                // Administrative gender
                fieldNo = 8;
                if (!pidSegment.AdministrativeSex.IsEmpty())
                    retVal.GenderConcept = pidSegment.AdministrativeSex.ToConcept(AdministrativeGenderCodeSystem);

                // Patient Alias
                fieldNo = 9;
                if (pidSegment.PatientAliasRepetitionsUsed > 0)
                    retVal.Names.AddRange(pidSegment.GetPatientAlias().ToModel());

                // Race codes
                fieldNo = 10;
                if (pidSegment.RaceRepetitionsUsed > 0)
                    ; // TODO: Implement as an extension if needed 

                // Addresses
                fieldNo = 11;
                if (pidSegment.PatientAddressRepetitionsUsed > 0)
                    foreach (var itm in pidSegment.GetPatientAddress())
                    {
                        var model = itm.ToModel();
                        var existing = retVal.Addresses.FirstOrDefault(o => o.AddressUseKey == model.AddressUseKey);
                        if (existing == null)
                            retVal.Addresses.Add(model);
                        else
                            existing.CopyObjectData(model);
                    }

                // Fields
                fieldNo = 13;
                var telecoms = pidSegment.GetPhoneNumberBusiness().Union(pidSegment.GetPhoneNumberHome());
                foreach (var itm in telecoms)
                {
                    var model = itm.ToModel();
                    var existing = retVal.Telecoms.FirstOrDefault(o => o.AddressUseKey == model.AddressUseKey);
                    if (existing == null)
                        retVal.Telecoms.Add(model);
                    else
                        existing.CopyObjectData(model);
                }

                // Language
                if (!pidSegment.PrimaryLanguage.IsEmpty())
                    retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(pidSegment.PrimaryLanguage.Identifier.Value.ToLower(), true)
                };

                // Marital Status
                fieldNo = 16;
                if (!pidSegment.MaritalStatus.IsEmpty())
                    retVal.MaritalStatus = pidSegment.MaritalStatus.ToModel(MaritalStatusCodeSystem);

                // Religion
                fieldNo = 17;
                if (!pidSegment.Religion.IsEmpty())
                    retVal.ReligiousAffiliation = pidSegment.Religion.ToModel(ReligionCodeSystem);

                // Ethinic groups
                fieldNo = 22;
                if (pidSegment.EthnicGroupRepetitionsUsed > 0)
                    retVal.EthnicGroupCodeKey = pidSegment.GetEthnicGroup().First().ToModel(EthnicGroupCodeSystem).Key;

                fieldNo = 18;
                // Patient account, locate the specified account
                if (!pidSegment.PatientAccountNumber.IsEmpty())
                {

                    var account = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Account>>()?.Query(o => o.Identifiers.Any(i => i.Value == pidSegment.PatientAccountNumber.IDNumber.Value), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    if (account != null)
                        retVal.Participations.Add(new ActParticipation(ActParticipationKey.Holder, retVal) { SourceEntityKey = account.Key });
                    else
                    {
                        retVal.Participations.Add(new ActParticipation(ActParticipationKey.Holder, retVal)
                        {
                            SourceEntity = new Account()
                            {
                                Identifiers = new List<ActIdentifier>()
                                {
                                    new ActIdentifier()
                                    {
                                        Authority = pidSegment.PatientAccountNumber.AssigningAuthority.ToModel(),
                                        Value = pidSegment.PatientAccountNumber.IDNumber.Value
                                    }
                                },
                                StatusConceptKey = StatusKeys.New
                            }
                        });
                    }
                }

                // Birth place is present
                fieldNo = 23;
                if (!pidSegment.BirthPlace.IsEmpty()) // We need to find the birthplace relationship
                {
                    Guid birthPlaceId = Guid.Empty;
                    if (Guid.TryParse(pidSegment.BirthPlace.Value, out birthPlaceId))
                    {
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Birthplace, birthPlaceId));
                    }
                    else
                    {
                        var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Names.Any(n => n.Component.Any(c => c.Value == pidSegment.BirthPlace.Value)), AuthenticationContext.SystemPrincipal);
                        if (places.Count() == 1)
                            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Birthplace, places.First().Key));
                        else
                            throw new KeyNotFoundException($"Cannot find unique birth place registration with name {pidSegment.BirthPlace.Value}. Try using UUID.");
                    }
                }

                // MB indicator
                if (!pidSegment.MultipleBirthIndicator.IsEmpty())
                    retVal.MultipleBirthOrder = pidSegment.MultipleBirthIndicator.Value == "Y" ? (int?)-1 : null;
                if (!pidSegment.BirthOrder.IsEmpty())
                    retVal.MultipleBirthOrder = Int32.Parse(pidSegment.BirthOrder.Value);

                // Citizenship
                fieldNo = 26;
                if (pidSegment.CitizenshipRepetitionsUsed > 0)
                {
                    foreach (var cit in pidSegment.GetCitizenship())
                    {
                        var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value && i.Authority.Key == AssigningAuthorityKeys.Iso3166CountryCode), AuthenticationContext.SystemPrincipal);
                        if (places.Count() == 1)
                            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                        else
                            throw new KeyNotFoundException($"Cannot find country with code {cit.Identifier.Value}");

                    }
                }

                // Death info
                fieldNo = 29;
                if (!pidSegment.PatientDeathIndicator.IsEmpty())
                    retVal.DeceasedDate = pidSegment.PatientDeathIndicator.Value == "Y" ? (DateTime?)DateTime.MinValue : null;
                if (!pidSegment.PatientDeathDateAndTime.IsEmpty())
                {
                    retVal.DeceasedDate = pidSegment.PatientDeathDateAndTime.ToModel();
                    retVal.DeceasedDatePrecision = pidSegment.PatientDeathDateAndTime.ToDatePrecision();
                }

                // Last update time
                if (!pidSegment.LastUpdateDateTime.IsEmpty())
                    retVal.CreationTime = (DateTimeOffset)pidSegment.LastUpdateDateTime.ToModel();

                //if(!pidSegment.LastUpdateFacility.IsEmpty())
                //{
                //    // Find by user ID
                //    var user = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>().Query(u=>u.UserName == pidSegment.LastUpdateFacility.NamespaceID.Value).FirstOrDefault();
                //    if (user != null)
                //        retVal.CreatedBy = user;
                //}

                if (motherEntity == null)
                    return new IdentifiedData[] { retVal };
                else
                    return new IdentifiedData[] { motherEntity, retVal };
            }
            catch (HL7ProcessingException) // Just re-throw
            {
                throw;
            }
            catch (HL7DatatypeProcessingException e)
            {
                throw new HL7ProcessingException("Error processing PID segment", "PID", pidSegment.SetIDPID.Value, fieldNo, e.Component, e);
            }
            catch (Exception e)
            {
                throw new HL7ProcessingException("Error processing PID segment", "PID", pidSegment.SetIDPID.Value, fieldNo, 1, e);
            }
        }
    }
}
