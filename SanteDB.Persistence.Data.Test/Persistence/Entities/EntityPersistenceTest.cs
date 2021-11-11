using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Tests for entities
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Entity")]
    public class EntityPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Tests the insertion of a basic entity
        /// </summary>
        [Test]
        public void TestInsertBasicEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place
                };

                // Test insert
                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterInsert.StatusConceptKey);
                Assert.AreEqual(EntityClassKeys.LivingSubject, afterInsert.ClassConceptKey);
                Assert.AreEqual(EntityClassKeys.Place, afterInsert.TypeConceptKey);
                Assert.AreEqual(DeterminerKeys.Specific, afterInsert.DeterminerConceptKey);

                // Test retrieve
                var fetched = base.TestQuery<Entity>(k => k.Key == afterInsert.Key, 1).AsQueryable();
                var afterFetch = fetched.First();
                Assert.IsNotNull(afterFetch.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterFetch.StatusConceptKey);
                Assert.AreEqual(EntityClassKeys.LivingSubject, afterFetch.ClassConceptKey);
                Assert.AreEqual(EntityClassKeys.Place, afterFetch.TypeConceptKey);
                Assert.AreEqual(DeterminerKeys.Specific, afterFetch.DeterminerConceptKey);

                var classKey = base.TestQuery<Entity>(k => k.Key == afterInsert.Key, 1).AsResultSet();
                Assert.AreEqual(EntityClassKeys.LivingSubject, classKey.Select(o => o.ClassConceptKey).First());
            }
        }

        /// <summary>
        /// Test insertion of an entity with names
        /// </summary>
        [Test]
        public void TestInsertEntityNames()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place,
                    Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Assigned, "Justin"),
                    new EntityName(NameUseKeys.Legal, "Smith", "Justin", "T", "E")
                }
                };

                // Test insert
                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterInsert.StatusConceptKey);
                Assert.AreEqual(EntityClassKeys.LivingSubject, afterInsert.ClassConceptKey);
                Assert.AreEqual(EntityClassKeys.Place, afterInsert.TypeConceptKey);
                Assert.AreEqual(DeterminerKeys.Specific, afterInsert.DeterminerConceptKey);
                Assert.AreEqual(2, afterInsert.Names.Count);

                // Test retrieve
                var fetched = base.TestQuery<Entity>(k => k.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetched.First();
                Assert.IsNotNull(afterFetch.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterFetch.StatusConceptKey);
                Assert.AreEqual(EntityClassKeys.LivingSubject, afterFetch.ClassConceptKey);
                Assert.AreEqual(EntityClassKeys.Place, afterFetch.TypeConceptKey);
                Assert.AreEqual(DeterminerKeys.Specific, afterFetch.DeterminerConceptKey);
                Assert.IsNull(afterFetch.Names); // We're in "Quick" mode so we shouldn't have loaded any properties
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Names).Count);

                // Test query by name
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.IsNotNull(afterFetch.CreationTime);

                // Test name is added
                afterInsert = base.TestUpdate(afterInsert, (o) =>
                {
                    o.Names.Add(new EntityName(NameUseKeys.License, "Bob", "Smith"));
                    return o;
                });
                Assert.AreEqual(3, afterInsert.Names.Count);

                // Test query by name
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.IsNull(afterFetch.Names);
                Assert.AreEqual(3, afterFetch.LoadProperty(o => o.Names).Count);
                Assert.IsNull(afterFetch.Names[0].Component);
                Assert.AreEqual("Justin", afterFetch.Names[0].LoadProperty(o => o.Component)[0].Value);

                // Test name is updated
                afterInsert = base.TestUpdate(afterInsert, (o) =>
                {
                    o.Names.FirstOrDefault(n => n.NameUseKey == NameUseKeys.Legal).Component[0].Value = "Robert";
                    o.Names.FirstOrDefault(n => n.NameUseKey == NameUseKeys.Assigned).Component[0].Value = "Bobby";
                    return o;
                });
                Assert.AreEqual(3, afterInsert.Names.Count);

                fetched = base.TestQuery<Entity>(k => k.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.IsNull(afterFetch.Names);
                Assert.AreEqual("Robert", afterFetch.LoadProperty(o => o.Names).FirstOrDefault(o => o.NameUseKey == NameUseKeys.Legal).LoadProperty(o => o.Component)[0].Value);
                Assert.AreEqual("Bobby", afterFetch.Names.FirstOrDefault(o => o.NameUseKey == NameUseKeys.Assigned).LoadProperty(o => o.Component)[0].Value);
                Assert.Greater(afterFetch.Names[0].Component[0].OrderSequence, 0);
                // No more justin
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 0).AsResultSet();
                // But we have one Bob
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.Component.Any(c => c.Value.Contains("Bob"))), 1).AsResultSet();

                afterFetch = fetched.First();
                Assert.AreEqual(afterInsert.Key, afterFetch.Key);
            }
        }

        /// <summary>
        /// Test insertion of entity with addresses
        /// </summary>
        [Test]
        public void TestInsertEntityAddresses()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place,
                    Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Assigned, "Justin2"),
                    new EntityName(NameUseKeys.Legal, "Smith", "Justin2", "T", "E")
                },
                    Addresses = new List<EntityAddress>()
                {
                    new EntityAddress(AddressUseKeys.Direct, "123 Test1 Street West", "Hamilton1", "ON", "CA", "L8K5N2")
                }
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(1, afterInsert.Addresses.Count);

                // Test fetch
                var fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetched.First();
                Assert.IsNull(afterFetch.Addresses);
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Addresses).Count);
                Assert.IsNull(afterFetch.Addresses[0].Component);
                Assert.AreEqual(5, afterFetch.Addresses[0].LoadProperty(o => o.Component).Count);

                // Test query by address
                fetched = base.TestQuery<Entity>(o => o.Addresses.Any(a => a.Component.Any(c => c.Value.Contains("Test1 Street"))), 1).AsResultSet();
                // Test property passthrough
                Assert.Catch<InvalidOperationException>(() => fetched.Select(o => o.Addresses).First());

                // Test adding address

                afterInsert = base.TestUpdate(afterInsert, (o) =>
                {
                    o.Addresses.Add(new EntityAddress(AddressUseKeys.WorkPlace, "123 Main Street West", "Hamilton1", "ON", "CA", "L8K5N2"));
                    return o;
                });
                Assert.AreEqual(2, afterInsert.Addresses.Count);

                // Test fetch
                fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.IsNull(afterFetch.Addresses);
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Addresses).Count);

                // Test Query by either address
                fetched = base.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.WorkPlace || g.AddressUseKey == AddressUseKeys.Direct).Any(a => a.Component.Where(g => g.ComponentTypeKey == AddressComponentKeys.City).Any(c => c.Value == "Hamilton1")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(afterInsert.Key, afterFetch.Key);

                // Test updating an address
                afterInsert = base.TestUpdate(afterInsert, (o) =>
                {
                    o.Addresses[0].Component.Add(new EntityAddressComponent(AddressComponentKeys.BuildingNumberSuffix, "123"));
                    return o;
                }, (o) =>
                {
                    o.Addresses[0].Component.First(c => c.ComponentTypeKey == AddressComponentKeys.City).Value = "Hamilton2";
                    return o;
                });
                Assert.AreEqual("123", afterInsert.Addresses[0].Component.Find(c => c.ComponentTypeKey == AddressComponentKeys.BuildingNumberSuffix).Value);

                // Test that the update applied
                fetched = base.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.WorkPlace || g.AddressUseKey == AddressUseKeys.Direct).Any(a => a.Component.Where(g => g.ComponentTypeKey == AddressComponentKeys.BuildingNumberSuffix).Any(c => c.Value == "123")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(afterInsert.Key, afterFetch.Key);
                fetched = base.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.WorkPlace || g.AddressUseKey == AddressUseKeys.Direct).Any(a => a.Component.Where(g => g.ComponentTypeKey == AddressComponentKeys.City).Any(c => c.Value == "Hamilton2")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(afterInsert.Key, afterFetch.Key);
            }
        }

        /// <summary>
        /// Test the insertion of an entity identifier
        /// </summary>
        [Test]
        public void TestInsertEntityIdentifier()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place,
                    Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Assigned, "Justin3"),
                    new EntityName(NameUseKeys.Legal, "Smith", "Justin3", "T", "E")
                },
                    Addresses = new List<EntityAddress>()
                {
                    new EntityAddress(AddressUseKeys.Direct, "123 Test1 Street West", "Hamilton3", "ON", "CA", "L8K5N2")
                },
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new Core.Model.DataTypes.EntityIdentifier(new AssigningAuthority("TEST_3", "TESTING", "1.2.3.4.5.5"), "TEST3")
                    }
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(1, afterInsert.Identifiers.Count);

                var fetch = base.TestQuery<Entity>(o => o.Identifiers.Where(g => g.Authority.DomainName == "TEST_3").Any(i => i.Value == "TEST3"), 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.IsNull(afterFetch.Identifiers);
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Identifiers).Count);
                Assert.IsNotNull(afterFetch.Identifiers[0].Authority);
                Assert.AreEqual("TEST_3", afterFetch.Identifiers[0].Authority.DomainName);
            }
        }

        public void TestInsertEntityTelecom()
        {
        }

        public void TestInsertEntityFull()
        {
        }

        public void TestUpdateEntityBasic()
        {
        }

        public void TestUpdateEntityRemoveAssociated()
        {
        }

        public void TestUpdateEntityChangeClassCode()
        {
        }

        public void TestObsoleteEntity()
        {
        }

        public void TestQueryEntity()
        {
        }

        public void TestQueryEntityOrdering()
        {
        }

        public void TestQueryEntityNested()
        {
        }
    }
}