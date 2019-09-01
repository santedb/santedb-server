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

            foreach (var itm in patient.LoadCollection<EntityRelationship>("Relationships").Where(o => NextOfKinRelationshipTypes.Contains(o.RelationshipTypeKey.Value)))
            {
                var nk1 = context.GetStructure("NK1", context.GetAll("NK1").Length) as NK1;

                // TODO: Map the NK1

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
                        // Existing relationship?
                        retValRelation = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(r => r.SourceEntityKey == patient.Key.Value && r.TargetEntityKey == retVal.Key.Value, AuthenticationContext.SystemPrincipal).FirstOrDefault() ?? retValRelation;
                        break;
                    }
                }

                // Relationship type
                fieldNo = 3;
                if (!nk1Segment.Relationship.IsEmpty())
                    retValRelation.RelationshipTypeKey = nk1Segment.Relationship.ToModel(RelationshipCodeSystem)?.Key;

                // Do we have a mother already?
                if (retValRelation.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother)
                {
                    // Get the existing mother relationship
                    var existingMotherRel = patient.Relationships.FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother);
                    if (existingMotherRel != null)
                    {
                        retValRelation = existingMotherRel;
                        retVal = context.FirstOrDefault(o => o.Key == existingMotherRel.TargetEntityKey) as Person;
                        // Mother isn't in context, load
                        if (retVal == null)
                            retVal = personService.Get(existingMotherRel.TargetEntityKey.Value, null, true, AuthenticationContext.SystemPrincipal);
                        if (retVal == null)
                            throw new InvalidOperationException("Cannot locate described MTH entity on patient record");
                    }
                }

                fieldNo = 2;
                // Names
                if (nk1Segment.NameRepetitionsUsed > 0)
                    retVal.Names = nk1Segment.GetName().ToModel().ToList();

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
                    retVal.Identifiers = nk1Segment.GetNextOfKinAssociatedPartySIdentifiers().ToModel().ToList();
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
