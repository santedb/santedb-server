using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a segment handler which handles PID segments
    /// </summary>
    public class PIDSegmentHandler : ISegmentHandler
    {

        private const string AdministrativeGenderCodeSystem = "2.16.840.1.113883.5.1";
        private const string RaceCodeSystem = "2.16.840.1.113883.5.5";
        private const string MaritalStatusCodeSystem = "2.16.840.1.113883.5.2";
        private const string ReligionCodeSystem = "2.16.840.1.113883.5.6";
        private const string EthnicGroupCodeSystem = "2.16.840.1.113883.5.189";


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
        public IEnumerable<ISegment> Create(IdentifiedData data, IMessage context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the parse the specified segment into a patient object
        /// </summary>
        /// <param name="segment">The segment to be parsed</param>
        /// <returns>The parsed patient information</returns>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            var patientService = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>();

            var pidSegment = segment as PID;
           
            Patient retVal = new Patient() { Key = Guid.NewGuid() };
            // Existing patient?
            foreach (var id in pidSegment.GetAlternatePatientIDPID())
            {
                var queryId = id.ToModel();
                retVal = patientService.Query(o => o.Identifiers.Any(i => i.Value == queryId.Value &&
                    i.AuthorityKey == queryId.AuthorityKey), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if (retVal != null) break;
            }
            Dictionary<Guid, Person> nok = new Dictionary<Guid, Person>();


            if (!pidSegment.PatientID.IsEmpty())
                retVal.Identifiers.Add(pidSegment.PatientID.ToModel());
            if (pidSegment.PatientIdentifierListRepetitionsUsed > 0)
                retVal.Identifiers.AddRange(pidSegment.GetPatientIdentifierList().ToModel());
            if (pidSegment.PatientNameRepetitionsUsed > 0)
                retVal.Names.AddRange(pidSegment.GetPatientName().ToModel());

            // Mother's maiden name, create a relationship for mother
            if (pidSegment.MotherSMaidenNameRepetitionsUsed > 0 || pidSegment.MotherSIdentifierRepetitionsUsed > 0)
                nok.Add(EntityRelationshipTypeKeys.Mother, new Person()
                {
                    Identifiers = pidSegment.GetMotherSIdentifier().ToModel().ToList(),
                    Names = pidSegment.GetMotherSMaidenName().ToModel().ToList()
                });

            // Date/time of birth
            if (!pidSegment.DateTimeOfBirth.IsEmpty())
            {
                retVal.DateOfBirth = pidSegment.DateTimeOfBirth.ToModel();
                retVal.DateOfBirthPrecision = pidSegment.DateTimeOfBirth.DegreeOfPrecision.ToDatePrecision() ;
            }

            // Administrative gender
            if (!pidSegment.AdministrativeSex.IsEmpty())
                retVal.GenderConcept = pidSegment.AdministrativeSex.ToConcept(AdministrativeGenderCodeSystem);

            // Patient Alias
            if (pidSegment.PatientAliasRepetitionsUsed > 0)
                retVal.Names.AddRange(pidSegment.GetPatientAlias().ToModel());

            // Race codes
            if (pidSegment.RaceRepetitionsUsed > 0)
                retVal.RaceCodeKeys.AddRange(pidSegment.GetRace().ToModel(RaceCodeSystem).Select(o => o.Key.Value));

            if (pidSegment.PatientAddressRepetitionsUsed > 0)
                retVal.Addresses.AddRange(pidSegment.GetPatientAddress().ToModel());

            if (pidSegment.PhoneNumberHomeRepetitionsUsed > 0)
                retVal.Telecoms.AddRange(pidSegment.GetPhoneNumberHome().ToModel());
            if (pidSegment.PhoneNumberBusinessRepetitionsUsed > 0)
                retVal.Telecoms.AddRange(pidSegment.GetPhoneNumberBusiness().ToModel());

            if (!pidSegment.PrimaryLanguage.IsEmpty())
                retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(pidSegment.PrimaryLanguage.Identifier.Value, true)
                };

            if (!pidSegment.MaritalStatus.IsEmpty())
                retVal.MaritalStatus = pidSegment.MaritalStatus.ToModel(MaritalStatusCodeSystem);

            if (!pidSegment.Religion.IsEmpty())
                retVal.ReligiousAffiliation = pidSegment.Religion.ToModel(ReligionCodeSystem);

            if (pidSegment.EthnicGroupRepetitionsUsed > 0)
                retVal.EthnicGroupCodeKeys = pidSegment.GetEthnicGroup().ToModel(EthnicGroupCodeSystem).Select(o=>o.Key.Value).ToList();

            // Patient account, locate the specified account
            if(!pidSegment.PatientAccountNumber.IsEmpty())
            {

                var account = ApplicationContext.Current.GetService<IDataPersistenceService<Account>>()?.Query(o => o.Identifiers.Any(i => i.Value == pidSegment.PatientAccountNumber.IDNumber.Value), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if(account != null)
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.Holder, retVal) { SourceEntityKey = account.Key });
            }

            // Birth place is present
            if(!pidSegment.BirthPlace.IsEmpty()) // We need to find the birthplace relationship
            {
                var places = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Names.Any(n => n.Component.Any(c => c.Value == pidSegment.BirthPlace.Value)), AuthenticationContext.SystemPrincipal);
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
                    var places = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value), AuthenticationContext.SystemPrincipal);
                    if (places.Count() == 1)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                }
            }

            // Death info
            if (!pidSegment.PatientDeathIndicator.IsEmpty())
                retVal.DeceasedDate = pidSegment.PatientDeathIndicator.Value == "Y" ? (DateTime?)DateTime.MinValue : null;
            if (!pidSegment.PatientDeathDateAndTime.IsEmpty()) {
                retVal.DeceasedDate = pidSegment.PatientDeathDateAndTime.ToModel();
                retVal.DeceasedDatePrecision = pidSegment.PatientDeathDateAndTime.DegreeOfPrecision.ToDatePrecision();
            }

            // Last update time
            if (!pidSegment.LastUpdateDateTime.IsEmpty())
                retVal.CreationTime = (DateTimeOffset)pidSegment.LastUpdateDateTime.ToModel();
            if(!pidSegment.LastUpdateFacility.IsEmpty())
            {
                // Find by user ID
                var user = ApplicationContext.Current.GetService<IDataPersistenceService<SecurityUser>>().Query(u=>u.UserName == pidSegment.LastUpdateFacility.NamespaceID.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if (user != null)
                    retVal.CreatedBy = user;
            }

            return new IdentifiedData[] { retVal };

        }
    }
}
