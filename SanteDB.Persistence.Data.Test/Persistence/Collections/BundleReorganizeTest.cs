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
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using SanteDB.Persistence.Data.Services.Persistence.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Collections
{
    /// <summary>
    /// Ensures that the bundle service can reorganize itself
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BundleReorganizeTest
    {


        /// <summary>
        /// Verifies that a bundle with contents A, B, C where:
        /// A relies on B
        /// B relies on C
        /// can be re-organized to bundle C, A, B
        /// </summary>
        [Test]
        public void TestReorganizeSimple()
        {
            IdentifiedData c = new Person() { Key = Guid.NewGuid() },
                b = new Patient() { Key = Guid.NewGuid(), Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.Mother, c as Entity) } },
                a = new Act() { Key = Guid.NewGuid(), Participations = new List<ActParticipation>() { new ActParticipation(ActParticipationKeys.RecordTarget, b as Entity) } };

            var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var reorganized = serviceManager.CreateInjected<BundlePersistenceService>().ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { a, b, c }));
            Assert.AreEqual(0, reorganized.Item.IndexOf(c));
            Assert.AreEqual(1, reorganized.Item.IndexOf(b));
            Assert.AreEqual(2, reorganized.Item.IndexOf(a));
        }

        /// <summary>
        /// Tests that bundle { A, B, C } where
        /// A relies on B
        /// B relies on C
        /// C relies on A
        /// Throws a circular dependency issue
        /// </summary>
        [Test]
        public void TestCircularDependency()
        {
            IdentifiedData c = new Person() { Key = Guid.NewGuid() },
                b = new Patient() { Key = Guid.NewGuid(), Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.Mother, c as Entity) } },
                a = new Act() { Key = Guid.NewGuid(), Participations = new List<ActParticipation>() { new ActParticipation(ActParticipationKeys.RecordTarget, b as Entity) } };

            (c as Entity).Participations = new List<ActParticipation>() { new ActParticipation(ActParticipationKeys.RecordTarget, c.Key) { ActKey = a.Key } };

            try
            {
                var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
                serviceManager.CreateInjected<BundlePersistenceService>().ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { a, b, c }));
                Assert.Fail("Should throw detected issue exception!");
            }
            catch (InvalidOperationException e) when (e.Message == ErrorMessageStrings.DATA_CIRCULAR_DEPENDENCY)
            {

            }
            catch
            {
                Assert.Fail("Wrong exception type thrown");
            }
        }

        /// <summary>
        /// Tests that bundles { A, B, C, D, E, F }, { B, A, D, E, C, F }, { F, E, D, C, B, A } and { E, F, B, A, C, D } where
        /// A relies on B and E
        /// B relies on F
        /// C relies on A
        /// D relies on C, E and A
        /// F relies on E
        /// are all organized to the proper ordering of { E, F, B, A, C, D }
        /// </summary>
        [Test]
        public void TestReorganizeComplex()
        {
            IdentifiedData e = new Organization() { Key = Guid.NewGuid() },
                f = new Person() { Key = Guid.NewGuid(), Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.Employee, e as Entity) } },
                b = new Patient() { Key = Guid.NewGuid(), Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.Mother, f as Entity) } },
                a = new Provider() { Key = Guid.NewGuid(), Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.HealthcareProvider, b as Entity), new EntityRelationship(EntityRelationshipTypeKeys.Employee, e as Entity) } },
                c = new Act() { Key = Guid.NewGuid(), Participations = new List<ActParticipation>() { new ActParticipation(ActParticipationKeys.Admitter, a as Entity) } },
                d = new Act()
                {
                    Key = Guid.NewGuid(),
                    Relationships = new List<ActRelationship>() { new ActRelationship(ActRelationshipTypeKeys.HasComponent, c as Act) },
                    Participations = new List<ActParticipation>()
                {
                    new ActParticipation(ActParticipationKeys.InformationRecipient, e as Entity),
                    new ActParticipation(ActParticipationKeys.Admitter, a as Entity)
                }
                };

            var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var persistenceService = serviceManager.CreateInjected<BundlePersistenceService>();
            var reorganized = persistenceService.ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { a, b, c, d, e, f }));
            Assert.AreEqual(0, reorganized.Item.IndexOf(e));
            Assert.AreEqual(1, reorganized.Item.IndexOf(f));
            Assert.AreEqual(2, reorganized.Item.IndexOf(b));
            Assert.AreEqual(3, reorganized.Item.IndexOf(a));
            Assert.AreEqual(4, reorganized.Item.IndexOf(c));
            Assert.AreEqual(5, reorganized.Item.IndexOf(d));

            reorganized = persistenceService.ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { b, a, d, e, c, f }));
            Assert.AreEqual(0, reorganized.Item.IndexOf(e));
            Assert.AreEqual(1, reorganized.Item.IndexOf(f));
            Assert.AreEqual(2, reorganized.Item.IndexOf(b));
            Assert.AreEqual(3, reorganized.Item.IndexOf(a));
            Assert.AreEqual(4, reorganized.Item.IndexOf(c));
            Assert.AreEqual(5, reorganized.Item.IndexOf(d));

            reorganized = persistenceService.ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { f, e, d, c, b, a }));
            Assert.AreEqual(0, reorganized.Item.IndexOf(e));
            Assert.AreEqual(1, reorganized.Item.IndexOf(f));
            Assert.AreEqual(2, reorganized.Item.IndexOf(b));
            Assert.AreEqual(3, reorganized.Item.IndexOf(a));
            Assert.AreEqual(4, reorganized.Item.IndexOf(c));
            Assert.AreEqual(5, reorganized.Item.IndexOf(d));

            reorganized = persistenceService.ReorganizeForInsert(new Core.Model.Collection.Bundle(new IdentifiedData[] { e, f, b, a, c, d }));
            Assert.AreEqual(0, reorganized.Item.IndexOf(e));
            Assert.AreEqual(1, reorganized.Item.IndexOf(f));
            Assert.AreEqual(2, reorganized.Item.IndexOf(b));
            Assert.AreEqual(3, reorganized.Item.IndexOf(a));
            Assert.AreEqual(4, reorganized.Item.IndexOf(c));
            Assert.AreEqual(5, reorganized.Item.IndexOf(d));

        }
    }

}
