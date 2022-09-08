/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-9-7
 */
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Relationships validation provider tests
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class RelationshipValidationProviderTest : DataPersistenceTest
    {

        /// <summary>
        /// Test that the creation of a validation rule works
        /// </summary>
        [Test]
        public void TestCreateEntityValidationRule()
        {

            var evService = ApplicationServiceContext.Current.GetService<IRelationshipValidationProvider>();

            using (AuthenticationContext.EnterSystemContext())
            {
                Entity e1 = new Place(), e2 = new Person();
                var er = new EntityRelationship()
                {
                    SourceEntity = e1,
                    TargetEntity = e2,
                    RelationshipTypeKey = EntityRelationshipTypeKeys.Parent
                };

                // First - we try to insert two entities that don't make sense - this should throw an exception
                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Insert(er.Clone() as EntityRelationship, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("Place -[Parent]-> Person should throw validation exception!");
                }
                catch(DataPersistenceException) { }
                catch(DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }

                var relationshipCount = evService.GetValidRelationships<EntityRelationship>().Count();

                // Now we want to add the relationship
                evService.AddValidRelationship<EntityRelationship>(EntityClassKeys.Place, EntityClassKeys.Person, EntityRelationshipTypeKeys.Parent, "THIS IS A TEST");
                Assert.Greater(evService.GetValidRelationships<EntityRelationship>().Count(), relationshipCount);

                // Validate that the rule is persisted
                Assert.IsTrue(evService.GetValidRelationships<EntityRelationship>(EntityClassKeys.Place).Any(o => o.TargetClassKey == EntityClassKeys.Person));
                Assert.IsFalse(evService.GetValidRelationships<ActRelationship>(EntityClassKeys.Place).Any(o => o.TargetClassKey == EntityClassKeys.Person));

                // The relationship should persist now
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

                // Now we want to remove validation rule
                evService.RemoveValidRelationship<EntityRelationship>(EntityClassKeys.Place, EntityClassKeys.Person, EntityRelationshipTypeKeys.Parent);

                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Insert(er.Clone() as EntityRelationship, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("Place -[Parent]-> Person should throw validation exception!");
                }
                catch (DataPersistenceException) { }
                catch (DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }
            }

        }

        /// <summary>
        /// Test that the creation of a validation rule for acts work
        /// </summary>
        [Test]
        public void TestCreateActValidationRule()
        {

            var evService = ApplicationServiceContext.Current.GetService<IRelationshipValidationProvider>();

            using (AuthenticationContext.EnterSystemContext())
            {
                Act e1 = new SubstanceAdministration()
                {
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ActTime = DateTimeOffset.Now,
                    RouteKey = RouteOfAdministrationKeys.ChewOral,
                    DoseQuantity = 1,
                    DoseUnitKey = UnitOfMeasureKeys.Dose
                }, e2 = new PatientEncounter()
                {
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ActTime = DateTimeOffset.Now
                };
                var er = new ActRelationship()
                {
                    SourceEntity = e1,
                    TargetAct = e2,
                    RelationshipTypeKey = ActRelationshipTypeKeys.HasComponent
                };

                // First - we try to insert two entities that don't make sense - this should throw an exception
                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("SubstanceAdministration -[HasComponent]-> PatientEncounter should throw validation exception!");
                }
                catch (DataPersistenceException) { }
                catch (DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }

                var relationshipCount = evService.GetValidRelationships<ActRelationship>().Count();

                // Now we want to add the relationship
                evService.AddValidRelationship<ActRelationship>(ActClassKeys.SubstanceAdministration, ActClassKeys.Encounter, ActRelationshipTypeKeys.HasComponent, "THIS IS A TEST");
                Assert.Greater(evService.GetValidRelationships<ActRelationship>().Count(), relationshipCount);

                // Validate that the rule is persisted
                Assert.IsTrue(evService.GetValidRelationships<ActRelationship>(ActClassKeys.SubstanceAdministration).Any(o => o.TargetClassKey == ActClassKeys.Encounter));
                Assert.IsFalse(evService.GetValidRelationships<EntityRelationship>(ActClassKeys.Supply).Any(o => o.TargetClassKey == ActClassKeys.Encounter));

                // The relationship should persist now
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

                // Now we want to remove validation rule
                evService.RemoveValidRelationship<ActRelationship>(ActClassKeys.SubstanceAdministration, ActClassKeys.Encounter, ActRelationshipTypeKeys.HasComponent);

                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("SubstanceAdministration -[HasComponent]-> PatientEncounter should throw validation exception!");
                }
                catch (DataPersistenceException) { }
                catch (DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }
            }

        }

        /// <summary>
        /// Test that the creation of a validation rule for participations works
        /// </summary>
        [Test]
        public void TestCreateParticipationValidationRule()
        {

            var evService = ApplicationServiceContext.Current.GetService<IRelationshipValidationProvider>();

            using (AuthenticationContext.EnterSystemContext())
            {
                Act e1 = new SubstanceAdministration()
                {
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ActTime = DateTimeOffset.Now,
                    RouteKey = RouteOfAdministrationKeys.ChewOral,
                    DoseQuantity = 1,
                    DoseUnitKey = UnitOfMeasureKeys.Dose
                };
                Entity e2 = new Material()
                {
                };
                var er = new ActParticipation()
                {
                    SourceEntity = e1,
                    PlayerEntity = e2,
                    ParticipationRoleKey = ActParticipationKeys.Admitter // A material cannot be an admitter
                };

                // First - we try to insert two entities that don't make sense - this should throw an exception
                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActParticipation>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("SubstanceAdministration -[Admitter]-> Material should throw validation exception!");
                }
                catch (DataPersistenceException) { }
                catch (DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }

                var relationshipCount = evService.GetValidRelationships<ActParticipation>().Count();

                // Now we want to add the relationship
                evService.AddValidRelationship<ActParticipation>(ActClassKeys.SubstanceAdministration, EntityClassKeys.Material, ActParticipationKeys.Admitter, "THIS IS A TEST");
                Assert.Greater(evService.GetValidRelationships<ActParticipation>().Count(), relationshipCount);

                // The relationship should persist now
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActParticipation>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

                // Now we want to remove validation rule
                evService.RemoveValidRelationship<ActParticipation>(ActClassKeys.SubstanceAdministration, EntityClassKeys.Material, ActParticipationKeys.Admitter);

                try
                {
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActParticipation>>().Insert(er, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    Assert.Fail("SubstanceAdministration -[Admitter]-> Material should throw validation exception!");
                }
                catch (DataPersistenceException) { }
                catch (DetectedIssueException) { }
                catch
                {
                    Assert.Fail("Wrong type of exception is thrown");
                }
            }
        }
    }
}
