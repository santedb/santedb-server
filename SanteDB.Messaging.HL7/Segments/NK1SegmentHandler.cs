using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;

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
        private Guid[] NextOfKinRelationshipTypes;

        /// <summary>
        /// NK1 segment handler ctor
        /// </summary>
        public NK1SegmentHandler()
        {
            ApplicationContext.Current.Started += (o, e) =>
            {
                this.NextOfKinRelationshipTypes = ApplicationContext.Current.GetService<IDataPersistenceService<Concept>>().Query(
                    c => c.ConceptSets.Any(s => s.Mnemonic == "FamilyMember"),
                    AuthenticationContext.SystemPrincipal
                ).Select(c => c.Key.Value).ToArray();
            };
        }

        /// <summary>
        /// Gets or sets the name of the segment
        /// </summary>
        public string Name => "NK1";

        /// <summary>
        /// Create next of kin relationship
        /// </summary>
        public IEnumerable<ISegment> Create(IdentifiedData data, IGroup context)
        {
            List<ISegment> retVal = new List<ISegment>();
            var patient = data as Patient;

            foreach(var itm in patient.Relationships.Where(o=>NextOfKinRelationshipTypes.Contains(o.RelationshipTypeKey.Value)))
            {
                var nk1 = context.GetStructure("NK1", context.GetAll("NK1").Length) as NK1;

                retVal.Add(nk1);
            }

            return retVal.ToArray();
        }

        /// <summary>
        /// Parse the Next Of Kin Data
        /// </summary>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {

            // Cast segment
            var nk1Segment = segment as NK1;

            // Person persistence service
            var personService = ApplicationContext.Current.GetService<IDataPersistenceService<Person>>();

            // Patient to which the parsing belongs
            var patient = context.OfType<Patient>().FirstOrDefault();
            if (patient == null)
                throw new InvalidOperationException("NK1 Requires PID segment to be processed");

            // Next of kin is a person
            Person retVal = new Person() { Key = Guid.NewGuid() };
            EntityRelationship retValRelation = new EntityRelationship(EntityRelationshipTypeKeys.NextOfKin, retVal.Key) { SourceEntityKey = patient.Key };

            // TODO: Look for existing persons
            foreach (var id in nk1Segment.GetNextOfKinAssociatedPartySIdentifiers())
            {
                var idnumber = id.IDNumber.Value;
                var authority = id.AssigningAuthority.ToModel();
                Guid idguid = Guid.Empty;
                Person found = null;
                if (authority == null &&
                    id.AssigningAuthority.NamespaceID.Value == ApplicationContext.Current.Configuration.Custodianship.Id.Id)
                    found = personService.Get(new Identifier<Guid>(Guid.Parse(id.IDNumber.Value)), AuthenticationContext.SystemPrincipal, true);
                else if (authority?.IsUnique == true)
                    found = personService.Query(o => o.Identifiers.Any(i => i.Value == idnumber &&
                        i.AuthorityKey == authority.Key), AuthenticationContext.SystemPrincipal).FirstOrDefault();
                if (found != null)
                {
                    retVal = found;
                    // Existing relationship?
                    retValRelation = ApplicationContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(r => r.SourceEntityKey == patient.Key.Value && r.TargetEntityKey == retVal.Key.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault() ?? retValRelation;
                    break;
                }
            }

            // Relationship type
            if (!nk1Segment.Relationship.IsEmpty())
                retValRelation.RelationshipTypeKey = nk1Segment.Relationship.ToModel(RelationshipCodeSystem)?.Key;

            // Do we have a mother already?
            if (retValRelation.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother)
            {
                // Get the existing mother relationship
                var existingMotherRel = patient.Relationships.FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother);
                if(existingMotherRel != null)
                {
                    retValRelation = existingMotherRel;
                    retVal = context.FirstOrDefault(o => o.Key == existingMotherRel.TargetEntityKey) as Person;
                    // Mother isn't in context, load
                    if (retVal == null)
                        retVal = personService.Get(new Identifier<Guid>(existingMotherRel.TargetEntityKey.Value), AuthenticationContext.SystemPrincipal, true);
                    if (retVal == null)
                        throw new InvalidOperationException("Cannot locate described MTH entity on patient record");
                }
            }
            // Names
            if (nk1Segment.NameRepetitionsUsed > 0)
                retVal.Names = nk1Segment.GetName().ToModel().ToList();

            // Address
            if (nk1Segment.AddressRepetitionsUsed > 0)
                retVal.Addresses = nk1Segment.GetAddress().ToModel().ToList();

            // Phone numbers 
            if (nk1Segment.PhoneNumberRepetitionsUsed > 0)
                retVal.Telecoms = nk1Segment.GetPhoneNumber().ToModel().ToList();

            // Context role, when the person should be contact
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
            }

            if (!nk1Segment.DateTimeOfBirth.IsEmpty())
            {
                retVal.DateOfBirth = nk1Segment.DateTimeOfBirth.ToModel();
                retVal.DateOfBirthPrecision = nk1Segment.DateTimeOfBirth.ToDatePrecision();
            }

            // Citizenship
            if (nk1Segment.CitizenshipRepetitionsUsed > 0)
            {
                foreach (var cit in nk1Segment.GetCitizenship())
                {
                    var places = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>()?.Query(o => o.Identifiers.Any(i => i.Value == cit.Identifier.Value && i.AuthorityKey == AssigningAuthorityKeys.Iso3166CountryCode), AuthenticationContext.SystemPrincipal);
                    if (places.Count() == 1)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Citizen, places.First().Key));
                }
            }

            if (!nk1Segment.PrimaryLanguage.IsEmpty())
                retVal.LanguageCommunication = new List<PersonLanguageCommunication>()
                {
                    new PersonLanguageCommunication(nk1Segment.PrimaryLanguage.Identifier.Value.ToLower(), true)
                };

            // Privacy code
            if (!nk1Segment.ProtectionIndicator.IsEmpty())
            {
                var pip = ApplicationContext.Current.GetService<IDataPersistenceService<SecurityPolicy>>();
                if (nk1Segment.ProtectionIndicator.Value == "Y")
                {
                    var policy = pip.Query(o => o.Oid == DataPolicyIdentifiers.RestrictedInformation, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    retVal.Policies.Add(new SecurityPolicyInstance(policy, PolicyGrantType.Grant));
                }
                else
                    retVal.Policies.Clear();
            }

            // Associated person identifiers
            if (nk1Segment.NextOfKinAssociatedPartySIdentifiersRepetitionsUsed > 0)
                retVal.Identifiers = nk1Segment.GetNextOfKinAssociatedPartySIdentifiers().ToModel().ToList();

            // Find the existing relationship on the patient
            
            patient.Relationships.RemoveAll(o => o.SourceEntityKey == retValRelation.SourceEntityKey && o.TargetEntityKey == retValRelation.TargetEntityKey);
            patient.Relationships.Add(retValRelation);

            if (!context.Contains(retVal))
                return new IdentifiedData[] { retVal };
            else
                return new IdentifiedData[0];
        }
    }
}
