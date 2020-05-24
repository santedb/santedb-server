/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Messaging.HL7.Exceptions;
using SanteDB.Core.Security.Services;

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
        public virtual IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, AssigningAuthority[] exportDomains)
        {
            var retVal = context.GetStructure("PD1") as PD1;
            var patient = data as Patient;
            
            // Load the PD1 data
            var relationships = patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships));

            // Living arrangement
            if (patient.LivingArrangementKey.HasValue)
                retVal.LivingArrangement.FromModel(patient.LoadProperty<Concept>(nameof(Patient.LivingArrangement)), LivingArrangementCodeSystem);

            // Assigned facilities
            foreach(var itm in relationships.Where(o=>o.RelationshipTypeKey == EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation))
            {
                var place = itm.LoadProperty<Place>(nameof(EntityRelationship.TargetEntity));
                var xon = retVal.GetPatientPrimaryFacility(retVal.PatientPrimaryFacilityRepetitionsUsed);

                xon.AssigningAuthority.FromModel(this.m_configuration.LocalAuthority);
                xon.OrganizationIdentifier.Value = place.Key.ToString();
                xon.OrganizationName.Value = place.LoadCollection<EntityName>(nameof(Entity.Names)).FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>(nameof(EntityName.Component))?.FirstOrDefault()?.Value;
                xon.OrganizationNameTypeCode.Value = "L"; // OFFICIAL RECORD
                xon.IdentifierTypeCode.Value = "XX";
            }

            // Protected?
            if (ApplicationServiceContext.Current.GetService<IPolicyInformationService>().GetPolicyInstance(patient, DataPolicyIdentifiers.RestrictedInformation) != null)
                retVal.ProtectionIndicator.Value = "Y";
            else
                retVal.ProtectionIndicator.Value = "N";
            return new ISegment[] { retVal };
        }

        /// <summary>
        /// Parse the PD1 segment
        /// </summary>
        public virtual IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {

            var fieldNo = 0;
            var pd1Segment = segment as PD1;

            try
            {
                var retVal = context.OfType<Patient>().LastOrDefault();
                if (retVal == null)
                    throw new MissingFieldException($"PD1 segment requires a PID segment to precede it");

                // Living arrangement
                fieldNo = 2;
                if (!pd1Segment.LivingArrangement.IsEmpty())
                    retVal.LivingArrangement = pd1Segment.LivingArrangement.ToConcept(LivingArrangementCodeSystem);

                // Primary facility
                fieldNo = 3;
                if (pd1Segment.PatientPrimaryFacilityRepetitionsUsed > 0)
                {
                    var sdlRepo = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>();
                    foreach (var xon in pd1Segment.GetPatientPrimaryFacility())
                    {
                        AssigningAuthority authority;
                        try
                        {
                            authority = xon.AssigningAuthority.ToModel();
                        }
                        catch (Exception e)
                        {
                            throw new HL7ProcessingException("Error processing patient primary facility", "PD1", "1", 3, 5, e);
                        }
                        var idnumber = xon.OrganizationIdentifier.Value ?? xon.IDNumber.Value;
                        // Find the org or SDL
                        Place place = null;
                        if (authority.Key == this.m_configuration.LocalAuthority.Key)
                            place = sdlRepo.Get(Guid.Parse(idnumber), null, true, AuthenticationContext.SystemPrincipal);
                        else
                            place = sdlRepo.Query(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation && o.Identifiers.Any(i => i.Value == idnumber && i.Authority.Key == authority.Key), AuthenticationContext.SystemPrincipal).SingleOrDefault();
                        if (place != null)
                            retVal.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.DedicatedServiceDeliveryLocation, place));
                        else
                            throw new KeyNotFoundException($"Facility {idnumber} could not be found");
                    }
                }


                // Disabilities - Create functional limitation template
                fieldNo = 6;
                if (!pd1Segment.Handicap.IsEmpty())
                {
                    var handicap = pd1Segment.Handicap.ToConcept(DisabilityCodeSystem).Key.Value;
                    // TODO: Create functional limitation observations about the patient
                    throw new NotImplementedException("Handicap / Functional Limitation handler for PD1 is not completed yet");
                }

                // Privacy code
                fieldNo = 12;
                if (!pd1Segment.ProtectionIndicator.IsEmpty())
                {
                    var pip = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityPolicy>>();
                    if (pd1Segment.ProtectionIndicator.Value == "Y")
                        retVal.AddPolicy(DataPolicyIdentifiers.RestrictedInformation);
                    else if (pd1Segment.ProtectionIndicator.Value == "N")
                        retVal.Policies.Clear();
                    else
                        throw new ArgumentOutOfRangeException($"Value {pd1Segment.ProtectionIndicator.Value} is not valid");
                }

                return new IdentifiedData[0];
            }
            catch (HL7ProcessingException) // Just re-throw
            {
                throw;
            }
            catch (HL7DatatypeProcessingException e)
            {
                throw new HL7ProcessingException("Error processing PD1 segment", "PD1", null, fieldNo, e.Component, e);
            }
            catch (Exception e)
            {
                throw new HL7ProcessingException("Error processing PD1 segment", "PD1", null, fieldNo, 1, e);
            }
        }
    }
}
