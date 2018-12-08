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
using SanteDB.Core.Model.Constants;
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

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a segment handler for PDQ
    /// </summary>
    public class PD1SegmentHandler : ISegmentHandler
    {

        private const string LivingArrangementCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.220";
        private const string DisabilityCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.295";
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        /// <summary>
        /// Patient demographics 1
        /// </summary>
        public string Name => "PD1";

        /// <summary>
        /// Create PD1
        /// </summary>
        public IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, string[] exportDomains)
        {
            var retVal = context.GetStructure("PD1") as PD1;

            return new ISegment[] { retVal };
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
                var sdlRepo = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>();
                foreach (var xon in pd1Segment.GetPatientPrimaryFacility())
                {
                    var authority = xon.AssigningAuthority.ToModel(false);
                    var idnumber = xon.OrganizationIdentifier.Value ?? xon.IDNumber.Value;
                    // Find the org or SDL
                    Place place = null;
                    if (authority == null && xon.AssigningAuthority.NamespaceID.Value == this.m_configuration.LocalAuthority.DomainName)
                        place = sdlRepo.Get(Guid.Parse(idnumber), null, true, AuthenticationContext.SystemPrincipal);
                    else
                        place = sdlRepo.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation && o.Identifiers.Any(i => i.Value == idnumber && i.AuthorityKey == authority.Key), AuthenticationContext.SystemPrincipal).SingleOrDefault();
                    if (place != null)
                        retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, place));

                }
            }

            // Disabilities - Create functional limitation template
            if (!pd1Segment.Handicap.IsEmpty())
            {
                var handicap = pd1Segment.Handicap.ToConcept(DisabilityCodeSystem)?.Key.Value;
                // TODO: Create functional limitation
                throw new NotImplementedException("Handicap / Functional Limitation handler for PD1 is not completed yet");
            }

            // Privacy code
            if (!pd1Segment.ProtectionIndicator.IsEmpty())
            {
                var pip = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityPolicy>>();
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
