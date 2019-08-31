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
        public virtual IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, string[] exportDomains)
        {
            var retVal = context.GetStructure("PID") as PID;
            var patient = data as Patient;
            if (patient == null)
                throw new InvalidOperationException($"Cannot convert {data.GetType().Name} to PID");

            // Map patient to PID
            if (exportDomains?.Length == 0 || exportDomains?.Contains(this.m_configuration.LocalAuthority.DomainName) == true)
            {
                retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed).FromModel(new EntityIdentifier(this.m_configuration.LocalAuthority, patient.Key.ToString()));
                retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed - 1).IdentifierTypeCode.Value = "PI";

            }
            // Map alternate identifiers
            foreach (var id in patient.LoadCollection<EntityIdentifier>("Identifiers"))
                if (exportDomains == null || exportDomains.Contains(id.LoadProperty<AssigningAuthority>("Authority").DomainName) == true)
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
                foreach (var nam in mother.LoadCollection<EntityName>(nameof(Entity.Names)))
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
            // TODO: Contact information
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
                    catch (HL7DatatypeProcessingException e)
                    {
                        throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 3, 3);
                    }

                    Guid idguid = Guid.Empty;
                    Patient found = null;
                    if (authority == null &&
                        id.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority?.DomainName)
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


            if (!pidSegment.PatientID.IsEmpty())
                try
                {
                    retVal.Identifiers.Add(pidSegment.PatientID.ToModel());
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 2, e.Component, e);
                }
            if (pidSegment.PatientIdentifierListRepetitionsUsed > 0)
                try
                {
                    retVal.Identifiers.AddRange(
                    pidSegment.GetPatientIdentifierList().ToModel().Where(o => !retVal.Identifiers.Any(i => i.Authority.Key == o.AuthorityKey && i.Value == o.Value)).ToArray()
                );
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 3, e.Component, e);
                }

            // Find the key for the patient 
            var keyId = pidSegment.GetPatientIdentifierList().FirstOrDefault(o => o.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority.DomainName);
            if (keyId != null)
                retVal.Key = Guid.Parse(keyId.IDNumber.Value);

            if (retVal.Identifiers.Count == 0)
                throw new HL7ProcessingException("Couldn't understand any patient identity", "PID", pidSegment.SetIDPID.Value, 3, 1);

            if (pidSegment.PatientNameRepetitionsUsed > 0)
                try
                {
                    foreach (var itm in pidSegment.GetPatientName())
                    {
                        var model = itm.ToModel();
                        var existing = retVal.Names.FirstOrDefault(o => o.NameUseKey == model.NameUseKey);
                        if (existing == null)
                            retVal.Names.Add(model);
                        else
                            existing.CopyObjectData(model);
                    }
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 5, e.Component, e);
                }

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
                    catch (HL7DatatypeProcessingException e)
                    {
                        throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 21, 3);
                    }

                    if (authority?.IsUnique == true)
                        motherEntity = personPersistence.Query(o => o.Identifiers.Any(i => i.Value == id.IDNumber.Value && i.Authority.Key == authority.Key), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                }

                // Mother doesn't exist, so add it
                if (motherEntity == null)
                    try
                    {
                        motherEntity = new Person()
                        {
                            Key = Guid.NewGuid(),
                            Identifiers = pidSegment.GetMotherSIdentifier().ToModel().ToList(),
                            Names = pidSegment.GetMotherSMaidenName().ToModel().ToList(),
                            StatusConceptKey = StatusKeys.New
                        };
                    }
                    catch (HL7DatatypeProcessingException e)
                    {
                        throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 6, e.Component, e);
                    }

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
            if (!pidSegment.DateTimeOfBirth.IsEmpty())
            {
                try
                {
                    retVal.DateOfBirth = pidSegment.DateTimeOfBirth.ToModel();
                    retVal.DateOfBirthPrecision = pidSegment.DateTimeOfBirth.ToDatePrecision();
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 7, e.Component, e);
                }
            }

            // Administrative gender
            if (!pidSegment.AdministrativeSex.IsEmpty())
                try
                {
                    retVal.GenderConcept = pidSegment.AdministrativeSex.ToConcept(AdministrativeGenderCodeSystem);
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 8, e.Component, e);
                }

            // Patient Alias
            if (pidSegment.PatientAliasRepetitionsUsed > 0)
                try
                {
                    retVal.Names.AddRange(pidSegment.GetPatientAlias().ToModel());
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 9, e.Component, e);
                }

            // Race codes
            if (pidSegment.RaceRepetitionsUsed > 0)
                ; // TODO: Implement as an extension if needed 

            if (pidSegment.PatientAddressRepetitionsUsed > 0)
                try
                {
                    foreach (var itm in pidSegment.GetPatientAddress())
                    {
                        var model = itm.ToModel();
                        var existing = retVal.Addresses.FirstOrDefault(o => o.AddressUseKey == model.AddressUseKey);
                        if (existing == null)
                            retVal.Addresses.Add(model);
                        else
                            existing.CopyObjectData(model);
                    }
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 11, e.Component, e);
                }

            try
            {
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
            }
            catch (HL7DatatypeProcessingException e)
            {
                throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 13, e.Component, e);
            }

            if (!pidSegment.PrimaryLanguage.IsEmpty())
                retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(pidSegment.PrimaryLanguage.Identifier.Value.ToLower(), true)
                };

            if (!pidSegment.MaritalStatus.IsEmpty())
                try
                {
                    retVal.MaritalStatus = pidSegment.MaritalStatus.ToModel(MaritalStatusCodeSystem);
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 16, e.Component, e);
                }

            if (!pidSegment.Religion.IsEmpty())
                try
                {
                    retVal.ReligiousAffiliation = pidSegment.Religion.ToModel(ReligionCodeSystem);
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 17, e.Component, e);
                }

            if (pidSegment.EthnicGroupRepetitionsUsed > 0)
                try
                {
                    retVal.EthnicGroupCodeKey = pidSegment.GetEthnicGroup().First().ToModel(EthnicGroupCodeSystem).Key;
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 22, e.Component, e);
                }

            // Patient account, locate the specified account
            if (!pidSegment.PatientAccountNumber.IsEmpty())
            {

                var account = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Account>>()?.Query(o => o.Identifiers.Any(i => i.Value == pidSegment.PatientAccountNumber.IDNumber.Value), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if (account != null)
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.Holder, retVal) { SourceEntityKey = account.Key });
            }

            // Birth place is present
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
                        throw new HL7ProcessingException($"Cannot find unique birth place registration with name {pidSegment.BirthPlace.Value}. Try using UUID.", "PID", pidSegment.SetIDPID.Value, 23, 1);
                }
            }

            // MB indicator
            if (!pidSegment.MultipleBirthIndicator.IsEmpty())
                retVal.MultipleBirthOrder = pidSegment.MultipleBirthIndicator.Value == "Y" ? (int?)-1 : null;
            if (!pidSegment.BirthOrder.IsEmpty())
                retVal.MultipleBirthOrder = Int32.Parse(pidSegment.BirthOrder.Value);

            // Citizenship
            if (pidSegment.CitizenshipRepetitionsUsed > 0)
            {
                foreach (var cit in pidSegment.GetCitizenship())
                {
                    var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value && i.Authority.Key == AssigningAuthorityKeys.Iso3166CountryCode), AuthenticationContext.SystemPrincipal);
                    if (places.Count() == 1)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                    else
                        throw new HL7ProcessingException($"Cannot find country with code {cit.Identifier.Value}", "PID", pidSegment.SetIDPID.Value, 26, 1);

                }
            }

            // Death info
            if (!pidSegment.PatientDeathIndicator.IsEmpty())
                retVal.DeceasedDate = pidSegment.PatientDeathIndicator.Value == "Y" ? (DateTime?)DateTime.MinValue : null;
            if (!pidSegment.PatientDeathDateAndTime.IsEmpty())
            {
                try
                {
                    retVal.DeceasedDate = pidSegment.PatientDeathDateAndTime.ToModel();
                    retVal.DeceasedDatePrecision = pidSegment.PatientDeathDateAndTime.ToDatePrecision();
                }
                catch (HL7DatatypeProcessingException e)
                {
                    throw new HL7ProcessingException(e.Message, "PID", pidSegment.SetIDPID.Value, 29, e.Component, e);
                }
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
    }
}
