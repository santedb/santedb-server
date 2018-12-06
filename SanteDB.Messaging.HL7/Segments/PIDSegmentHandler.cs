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
        public IEnumerable<ISegment> Create(IdentifiedData data, IGroup context)
        {
            var retVal = context.GetStructure("PID") as PID;
            var patient = data as Patient;
            if (patient == null)
                throw new InvalidOperationException($"Cannot convert {data.GetType().Name} to PID");

            // Map patient to PID
            retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed).FromModel(new EntityIdentifier(this.m_configuration.LocalAuthority, patient.Key.ToString()));

            // Map alternate identifiers
            foreach (var id in patient.LoadCollection<EntityIdentifier>("Identifiers"))
                retVal.GetPatientIdentifierList(retVal.PatientIdentifierListRepetitionsUsed).FromModel(id);

            // Addresses
            foreach (var addr in patient.LoadCollection<EntityAddress>("Addresses"))
                retVal.GetPatientAddress(retVal.PatientAddressRepetitionsUsed).FromModel(addr);

            // Names
            foreach (var en in patient.LoadCollection<EntityName>("Names"))
                retVal.GetPatientName(retVal.PatientNameRepetitionsUsed).FromModel(en);

            // Date of birth
            if (patient.DateOfBirth.HasValue) {
                switch (patient.DateOfBirthPrecision ?? DatePrecision.Day) {
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

            // Deceased date
            if(patient.DeceasedDate.HasValue)
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

            // TODO: Contact information
            return new ISegment[] { retVal };
        }

        /// <summary>
        /// Parse the parse the specified segment into a patient object
        /// </summary>
        /// <param name="segment">The segment to be parsed</param>
        /// <returns>The parsed patient information</returns>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            var patientService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>();

            var pidSegment = segment as PID;

            Patient retVal = new Patient() { Key = Guid.NewGuid() };
            Person motherEntity = null;

            retVal.CreationAct = context.OfType<ControlAct>().FirstOrDefault();

            // Existing patient?
            foreach (var id in pidSegment.GetPatientIdentifierList())
            {
                var idnumber = id.IDNumber.Value;
                var authority = id.AssigningAuthority.ToModel();
                Guid idguid = Guid.Empty;
                Patient found = null;
                if (authority == null &&
                    id.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority?.DomainName)
                    found = patientService.Get(Guid.Parse(id.IDNumber.Value), null, true, AuthenticationContext.Current.Principal);
                else if(authority?.IsUnique == true)
                    found = patientService.Query(o => o.Identifiers.Any(i => i.Value == idnumber &&
                        i.AuthorityKey == authority.Key), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if (found != null)
                {
                    retVal = found;
                    retVal.Names.Clear();
                    retVal.Addresses.Clear();
                    retVal.Telecoms.Clear();
                    retVal.Identifiers.Clear();
                    retVal.Extensions.Clear();
                    retVal.Relationships.Clear();
                    break;
                }
            }
            

            if (!pidSegment.PatientID.IsEmpty())
                retVal.Identifiers.Add(pidSegment.PatientID.ToModel());
            if (pidSegment.PatientIdentifierListRepetitionsUsed > 0)
                retVal.Identifiers.AddRange(pidSegment.GetPatientIdentifierList().ToModel());
            if (pidSegment.PatientNameRepetitionsUsed > 0)
                retVal.Names.AddRange(pidSegment.GetPatientName().ToModel());

            // Mother's maiden name, create a relationship for mother
            if (pidSegment.MotherSMaidenNameRepetitionsUsed > 0 || pidSegment.MotherSIdentifierRepetitionsUsed > 0)
            {
                // Attempt to find the mother
                var personPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Person>>();
                foreach(var id in pidSegment.GetMotherSIdentifier())
                {
                    var authortiy = id.AssigningAuthority.ToModel(false);
                    if (authortiy?.IsUnique == true)
                        motherEntity = personPersistence.Query(o => o.Identifiers.Any(i => i.Value == id.IDNumber.Value && i.AuthorityKey == authortiy.Key), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                }

                // Mother doesn't exist, so add it
                if (motherEntity == null)
                    motherEntity = new Person()
                    {
                        Key = Guid.NewGuid(),
                        Identifiers = pidSegment.GetMotherSIdentifier().ToModel().ToList(),
                        Names = pidSegment.GetMotherSMaidenName().ToModel().ToList()
                    };

                var existingRelationship = retVal.Relationships.FirstOrDefault(r => r.SourceEntityKey == retVal.Key && r.TargetEntityKey == motherEntity.Key);
                if (existingRelationship == null)
                    retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Mother, motherEntity.Key));
                else
                    existingRelationship.RelationshipTypeKey = EntityRelationshipTypeKeys.Mother;

            }

            // Date/time of birth
            if (!pidSegment.DateTimeOfBirth.IsEmpty())
            {
                retVal.DateOfBirth = pidSegment.DateTimeOfBirth.ToModel();
                retVal.DateOfBirthPrecision = pidSegment.DateTimeOfBirth.ToDatePrecision() ;
            }

            // Administrative gender
            if (!pidSegment.AdministrativeSex.IsEmpty())
                retVal.GenderConcept = pidSegment.AdministrativeSex.ToConcept(AdministrativeGenderCodeSystem);

            // Patient Alias
            if (pidSegment.PatientAliasRepetitionsUsed > 0)
                retVal.Names.AddRange(pidSegment.GetPatientAlias().ToModel());

            // Race codes
            if (pidSegment.RaceRepetitionsUsed > 0)
                ; // TODO: Implement as an extension if needed 

            if (pidSegment.PatientAddressRepetitionsUsed > 0)
                retVal.Addresses.AddRange(pidSegment.GetPatientAddress().ToModel());

            if (pidSegment.PhoneNumberHomeRepetitionsUsed > 0)
                retVal.Telecoms.AddRange(pidSegment.GetPhoneNumberHome().ToModel());
            if (pidSegment.PhoneNumberBusinessRepetitionsUsed > 0)
                retVal.Telecoms.AddRange(pidSegment.GetPhoneNumberBusiness().ToModel());

            if (!pidSegment.PrimaryLanguage.IsEmpty())
                retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(pidSegment.PrimaryLanguage.Identifier.Value.ToLower(), true)
                };

            if (!pidSegment.MaritalStatus.IsEmpty())
                retVal.MaritalStatus = pidSegment.MaritalStatus.ToModel(MaritalStatusCodeSystem);

            if (!pidSegment.Religion.IsEmpty())
                retVal.ReligiousAffiliation = pidSegment.Religion.ToModel(ReligionCodeSystem);

            if (pidSegment.EthnicGroupRepetitionsUsed > 0)
                retVal.EthnicGroupCodeKey = pidSegment.GetEthnicGroup().First().ToModel(EthnicGroupCodeSystem).Key;

            // Patient account, locate the specified account
            if(!pidSegment.PatientAccountNumber.IsEmpty())
            {

                var account = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Account>>()?.Query(o => o.Identifiers.Any(i => i.Value == pidSegment.PatientAccountNumber.IDNumber.Value), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if(account != null)
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.Holder, retVal) { SourceEntityKey = account.Key });
            }

            // Birth place is present
            if(!pidSegment.BirthPlace.IsEmpty()) // We need to find the birthplace relationship
            {
                var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Names.Any(n => n.Component.Any(c => c.Value == pidSegment.BirthPlace.Value)), AuthenticationContext.SystemPrincipal);
                if (places.Count() == 1)
                    retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Birthplace, places.First().Key));
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
                    var places = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value && i.AuthorityKey == AssigningAuthorityKeys.Iso3166CountryCode), AuthenticationContext.SystemPrincipal);
                    if (places.Count() == 1)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                }
            }

            // Death info
            if (!pidSegment.PatientDeathIndicator.IsEmpty())
                retVal.DeceasedDate = pidSegment.PatientDeathIndicator.Value == "Y" ? (DateTime?)DateTime.MinValue : null;
            if (!pidSegment.PatientDeathDateAndTime.IsEmpty()) {
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
    }
}
