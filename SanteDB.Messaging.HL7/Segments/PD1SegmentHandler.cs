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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a segment handler for PDQ
    /// </summary>
    public class PD1SegmentHandler : ISegmentHandler
    {

        private const string LivingArrangementCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.220";
        private const string DisabilityCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.295";

        /// <summary>
        /// Patient demographics 1
        /// </summary>
        public string Name => "PD1";

        public IEnumerable<ISegment> Create(IdentifiedData data, IMessage context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the PD1 segment
        /// </summary>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {

            var pd1Segment = segment as PD1;
            var retVal = context.OfType<Patient>().LastOrDefault();
            if (retVal == null)
                throw new MissingFieldException($"PD1 segment requires a PID segment to precede it");

            // Living arrangement
            if (!pd1Segment.LivingArrangement.IsEmpty())
                retVal.LivingArrangement = pd1Segment.LivingArrangement.ToConcept(LivingArrangementCodeSystem);

            // Primary facility
            if (pd1Segment.PatientPrimaryFacilityRepetitionsUsed > 0)
            {
                var sdlRepo = ApplicationContext.Current.GetService<IDataPersistenceService<Place>>();
                foreach (var xon in pd1Segment.GetPatientPrimaryFacility())
                {
                    var authority = xon.AssigningAuthority.ToModel(false);
                    var idnumber = xon.OrganizationIdentifier.Value ?? xon.IDNumber.Value;
                    // Find the org or SDL
                    Place place = null;
                    if (authority == null && xon.AssigningAuthority.NamespaceID.Value == ApplicationContext.Current.Configuration.Custodianship.Id.Id)
                        place = sdlRepo.Get(new Identifier<Guid>(Guid.Parse(idnumber)), AuthenticationContext.SystemPrincipal, true);
                    else
                        place = sdlRepo.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation && o.Identifiers.Any(i => i.Value == idnumber && i.AuthorityKey == authority.Key), AuthenticationContext.SystemPrincipal).SingleOrDefault();
                    if (place != null)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, place));

                }
            }

            // Disabilities
            if (!pd1Segment.Handicap.IsEmpty())
            {
                var handicap = pd1Segment.Handicap.ToConcept(DisabilityCodeSystem)?.Key.Value;
                if (handicap.HasValue)
                    retVal.DisabilityCodeKeys.Add(handicap.Value);
            }

            // Privacy code
            if (!pd1Segment.ProtectionIndicator.IsEmpty())
            {
                var pip = ApplicationContext.Current.GetService<IDataPersistenceService<SecurityPolicy>>();
                if (pd1Segment.ProtectionIndicator.Value == "Y")
                {
                    var policy = pip.Query(o => o.Oid == DataPolicyIdentifiers.RestrictedInformation, AuthenticationContext.SystemPrincipal).FirstOrDefault();
                    retVal.Policies.Add(new SecurityPolicyInstance(policy, PolicyGrantType.Grant));
                }
                else
                    retVal.Policies.Clear();
            }

            return new IdentifiedData[0];
        }
    }
}
