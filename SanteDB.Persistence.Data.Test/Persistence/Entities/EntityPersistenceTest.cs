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
using SanteDB.Core.Extensions;
using SanteDB.Core.Model.Query;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using SanteDB.Core;
using SanteDB.Core.Services;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Tests for entities
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
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
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Names).Count);

                // Test query by name
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.IsNotNull(afterFetch.CreationTime);

                // Test name is added
                afterInsert = base.TestUpdate(afterInsert, (o) =>
                {
                    o.Names.Add(new EntityName(NameUseKeys.License, "Bobz", "Smith"));
                    return o;
                });
                Assert.AreEqual(3, afterInsert.Names.Count);

                // Test query by name
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(3, afterFetch.LoadProperty(o => o.Names).Count);
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
                Assert.AreEqual("Robert", afterFetch.LoadProperty(o => o.Names).FirstOrDefault(o => o.NameUseKey == NameUseKeys.Legal).LoadProperty(o => o.Component)[0].Value);
                Assert.AreEqual("Bobby", afterFetch.Names.FirstOrDefault(o => o.NameUseKey == NameUseKeys.Assigned).LoadProperty(o => o.Component)[0].Value);
                Assert.Greater(afterFetch.Names[0].Component[0].OrderSequence, 0);
                // No more justin
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.NameUseKey == NameUseKeys.Assigned && n.Component.Any(c => c.Value == "Justin")), 0).AsResultSet();
                // But we have one Bob
                fetched = base.TestQuery<Entity>(k => k.Names.Any(n => n.Component.Any(c => c.Value.Contains("Bobz"))), 1).AsResultSet();

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
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Addresses).Count);
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
        /// Test tag persistence
        /// </summary>
        [Test]
        public void TestTagPersistence()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place,
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(0, afterInsert.LoadProperty(o=>o.Tags).Count);

                // Test fetch
                var fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetched.First();
                Assert.AreEqual(0, afterFetch.LoadProperty(o => o.Tags).Count);

                // Add tags
                var tagService = ApplicationServiceContext.Current.GetService<ITagPersistenceService>();
                Assert.IsNotNull(tagService);
                tagService.Save(afterInsert.Key.Value, new EntityTag("foo", "bars"));
                
                fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);
                Assert.AreEqual("bars", afterFetch.LoadProperty(o => o.Tags).First().Value);

                // Update tag
                tagService.Save(afterInsert.Key.Value, new EntityTag("foo", "baz"));
                fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);
                Assert.AreEqual("baz", afterFetch.LoadProperty(o => o.Tags).First().Value);

                // Add tag
                tagService.Save(afterInsert.Key.Value, new EntityTag("foobar", "baz"));
                fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Tags).Count);

                // Remove tag
                tagService.Save(afterInsert.Key.Value, new EntityTag("foobar", null));
                fetched = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);

                // Query by tag
                base.TestQuery<Entity>(o => o.Tags.Any(t => t.Value == "baz"), 1);
                base.TestQuery<Entity>(o => o.Tags.Any(t => t.Value == "bars"), 0);



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
                        new Core.Model.DataTypes.EntityIdentifier(new IdentityDomain("TEST_3", "TESTING", "1.2.3.4.5.5"), "TEST3")
                    }
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(1, afterInsert.Identifiers.Count);

                var fetch = base.TestQuery<Entity>(o => o.Identifiers.Where(g => g.IdentityDomain.DomainName == "TEST_3").Any(i => i.Value == "TEST3"), 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual("TEST_3", afterFetch.Identifiers[0].LoadProperty(o=>o.IdentityDomain).DomainName);
            }
        }

        /// <summary>
        /// Test insertion of entity data with a telecom address
        /// </summary>
        [Test]
        public void TestInsertEntityTelecom()
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
                    Telecoms = new List<EntityTelecomAddress>()
                    {
                        new EntityTelecomAddress(TelecomAddressUseKeys.Public, "mailto:justin@fyfesoftware.ca")
                        {
                            TypeConceptKey = TelecomAddressTypeKeys.Internet
                        }
                    }
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(1, afterInsert.Telecoms.Count);

                var fetch = base.TestQuery<Entity>(o => o.Telecoms.Any(t => t.Value == "mailto:justin@fyfesoftware.ca"), 1).AsResultSet();
                fetch = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Telecoms).Count);
                Assert.AreEqual(TelecomAddressTypeKeys.Internet, afterFetch.Telecoms[0].TypeConceptKey);
                Assert.AreEqual(TelecomAddressUseKeys.Public, afterFetch.Telecoms[0].AddressUseKey);
                Assert.AreEqual("mailto:justin@fyfesoftware.ca", afterFetch.Telecoms[0].Value);

                // Update
                var afterUpdate = base.TestUpdate(afterFetch, (o) =>
                {
                    o.Telecoms.Add(new EntityTelecomAddress(TelecomAddressUseKeys.Pager, "394-304-3045"));
                    return o;
                });
                afterFetch = fetch.First();
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Telecoms).Count);

                afterUpdate = base.TestUpdate(afterFetch, (o) =>
                {
                    o.Telecoms.RemoveAt(1);
                    return o;
                });
                afterFetch = fetch.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Telecoms).Count);

                afterUpdate = base.TestUpdate(afterFetch, (o) =>
                {
                    o.Telecoms[0].Value = "mailto:justin@fyfesoftware.com";
                    return o;
                });
                afterFetch = fetch.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Telecoms).Count);
                Assert.AreEqual("mailto:justin@fyfesoftware.com", afterFetch.Telecoms[0].Value);
            }
        }

        /// <summary>
        /// Tests the insert of a full entity
        /// </summary>
        [Test]
        public void TestInsertEntityFull()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConcept = new Concept() { Key = EntityClassKeys.Place },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Assigned, "Justin8")
                    },
                    Addresses = new List<EntityAddress>()
                {
                    new EntityAddress(AddressUseKeys.Direct, "123 Test8 Street West", "Hamilton8", "ON", "CA", "L8K5N2")
                },
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(new IdentityDomain("TEST8", "TEST8", "2.25.438792"), "TEST_8")
                    },
                    Telecoms = new List<EntityTelecomAddress>()
                    {
                        new EntityTelecomAddress(TelecomAddressUseKeys.Public, "mailto:justin2@fyfesoftware.ca")
                        {
                            TypeConceptKey = TelecomAddressTypeKeys.Internet
                        }
                    },
                    Relationships = new List<EntityRelationship>()
                    {
                        new EntityRelationship(EntityRelationshipTypeKeys.Replaces, new Entity() {
                                ClassConceptKey = EntityClassKeys.LivingSubject,
                                DeterminerConceptKey = DeterminerKeys.Specific,
                            Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "A name of a parent")
                            }
                        })
                    },
                    Extensions = new List<EntityExtension>()
                    {
                        new EntityExtension(ExtensionTypeKeys.DataQualityExtension, typeof(DictionaryExtensionHandler), new
                        {
                            foo = "bar"
                        })
                    },
                    Template = new TemplateDefinition()
                    {
                        Mnemonic = "a template",
                        Name = "A template",
                        Oid = "2.25.349329849823",
                        Description = "Just a test"
                    },
                    Notes = new List<EntityNote>()
                    {
                        new EntityNote()
                        {
                            Author = new Entity()
                            {
                                ClassConceptKey = EntityClassKeys.Entity,
                                DeterminerConceptKey = DeterminerKeys.Specific,
                                Names = new List<EntityName>()
                                {
                                    new EntityName(NameUseKeys.Legal, "Testing Author")
                                }
                            },
                            Text = "This is a test note"
                        }
                    },
                    Tags = new List<EntityTag>()
                    {
                        new EntityTag("foo","bar")
                    }
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(1, afterInsert.Telecoms.Count);

                var fetch = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetch.First();

                Assert.AreEqual(EntityClassKeys.Place, afterFetch.TypeConceptKey);
                Assert.AreEqual("Place", afterFetch.LoadProperty(o => o.TypeConcept).Mnemonic);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Justin8", afterFetch.Names[0].LoadProperty(o => o.Component)[0].Value);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Addresses).Count);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual("TEST_8", afterFetch.Identifiers[0].Value);
                Assert.AreEqual("TEST8", afterFetch.Identifiers[0].LoadProperty(o => o.IdentityDomain).DomainName);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Telecoms).Count);
                Assert.AreEqual("mailto:justin2@fyfesoftware.ca", afterFetch.Telecoms[0].Value);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual(EntityRelationshipTypeKeys.Replaces, afterFetch.Relationships[0].RelationshipTypeKey);
                Assert.AreEqual("A name of a parent", afterFetch.Relationships[0].LoadProperty(o => o.TargetEntity).LoadProperty(o => o.Names)[0].LoadProperty(o => o.Component)[0].Value);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);
                Assert.AreEqual("foo", afterFetch.Tags[0].TagKey);
                Assert.AreEqual("bar", afterFetch.Tags[0].Value);

                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Extensions).Count);
                Assert.AreEqual(ExtensionTypeKeys.DataQualityExtension, afterFetch.Extensions[0].ExtensionTypeKey);
                Assert.IsNotNull(afterFetch.Extensions[0].ExtensionValue);

                Assert.IsNotNull(afterFetch.LoadProperty(o => o.Template));
                Assert.AreEqual("A template", afterFetch.Template.Name);

                // Note
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Notes).Count);
                Assert.AreEqual("This is a test note", afterFetch.Notes.First().Text);
                Assert.AreEqual("Testing Author", afterFetch.Notes.First().LoadProperty(o => o.Author).LoadProperty(o => o.Names).First().LoadProperty(o => o.Component).First().Value);
            }
        }

        /// <summary>
        /// Verifies the changing of an entity's classification code
        /// </summary>
        [Test]
        public void TestUpdateEntityChangeClassCode()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);

                var fetch = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.AreEqual(EntityClassKeys.LivingSubject, afterFetch.ClassConceptKey);

                var afterUpdate = base.TestUpdate(afterFetch, (o) =>
                {
                    o.ClassConceptKey = EntityClassKeys.Food;
                    return o;
                });
                Assert.AreEqual(EntityClassKeys.Food, afterUpdate.ClassConceptKey);

                afterFetch = fetch.First();
                Assert.AreEqual(EntityClassKeys.Food, afterFetch.ClassConceptKey);
            }
        }

        /// <summary>
        /// Test the various modes of obsoleting an entity
        /// </summary>
        [Test]
        public void TestObsoleteEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.LivingSubject,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    TypeConceptKey = EntityClassKeys.Place
                };

                var afterInsert = base.TestInsert(entity);
                Assert.IsNotNull(afterInsert.CreationTime);

                var fetch = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.AreEqual(StatusKeys.New, afterFetch.StatusConceptKey);

                var afterDelete = base.TestDelete(afterFetch, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 0);
                base.TestQuery<Entity>(o => o.Key == afterInsert.Key && o.ObsoletionTime.HasValue, 1);

                // Now perma-delete
                afterDelete = base.TestDelete(afterDelete, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 0);
                base.TestQuery<Entity>(o => o.Key == afterInsert.Key && StatusKeys.InactiveStates.Contains(o.StatusConceptKey.Value), 0);
            }
        }

        /// <summary>
        /// Tests various queries against the database
        /// </summary>
        [Test]
        public void TestQueryEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                Guid aaUuid = Guid.NewGuid();
                var aa = base.TestInsert<IdentityDomain>(new IdentityDomain()
                {
                    Key = aaUuid,
                    DomainName = "TESTSTRESS",
                    Oid = "2.25.030404",
                    Url = "http://google.test",
                    Description = "A test thing",
                    Name = "TEST_STRESS"
                });

                Enumerable.Range(0, 9).AsParallel().ForAll(i =>
                {
                    using (AuthenticationContext.EnterSystemContext())
                    {
                        var entity = new Entity()
                        {
                            ClassConceptKey = EntityClassKeys.LivingSubject,
                            DeterminerConceptKey = DeterminerKeys.Specific,
                            TypeConceptKey = EntityClassKeys.Place,
                            Identifiers = new List<EntityIdentifier>()
                        {
                            new EntityIdentifier(aa, $"TEST_STRESS2_{i}")
                        },
                            Names = new List<EntityName>()
                        {
                            new EntityName(NameUseKeys.Legal, $"Name_TEST{i}")
                        },
                            Addresses = new List<EntityAddress>()
                        {
                            new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street", $"TestCity{i}", "ON", "CA", "L8K5N2")
                        },
                            Relationships = new List<EntityRelationship>()
                        {
                            new EntityRelationship(EntityRelationshipTypeKeys.Replaces, new Entity()
                            {
                                ClassConceptKey = EntityClassKeys.LivingSubject,
                                DeterminerConceptKey = DeterminerKeys.Specific,
                                Names= new List<EntityName>()
                                {
                                    new EntityName(NameUseKeys.Legal, $"ReplacedEntity{i}")
                                }
                            })
                        }
                        };

                        base.TestInsert(entity);
                    }
                });

                Enumerable.Range(0, 9).AsParallel().ForAll(f =>
                {
                    var name = $"Name_TEST{f}";
                    var id = $"TEST_STRESS2_{f}";
                    var afterQuery = base.TestQuery<Entity>(o => o.Identifiers.Any(i => i.Value == id), 1);
                    afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == name)), 1);
                });
            }
        }

        /// <summary>
        /// Tests the querying of entities with sorting
        /// </summary>
        [Test]
        public void TestQueryEntityOrdering()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                Guid aaUuid = Guid.NewGuid();
                var aa = base.TestInsert<IdentityDomain>(new IdentityDomain()
                {
                    Key = aaUuid,
                    DomainName = "TESTSTRESS3",
                    Oid = "2.25.030406",
                    Url = "http://google.test3",
                    Description = "A test thing",
                    Name = "TEST_STRESS_3"
                });

                Enumerable.Range(0, 3).ToList().ForEach(i =>
                {
                    var entity = new Entity()
                    {
                        ClassConceptKey = EntityClassKeys.LivingSubject,
                        DeterminerConceptKey = DeterminerKeys.Specific,
                        TypeConceptKey = EntityClassKeys.Place,
                        Identifiers = new List<EntityIdentifier>()
                            {
                                new EntityIdentifier(aaUuid, $"TEST_CASE3_{i}")
                            }
                    };

                    base.TestInsert(entity);
                });

                var afterQuery = base.TestQuery<Entity>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_CASE3_%")), 3).AsResultSet() as IOrderableQueryResultSet<Entity>;
                // Ordering should be newest first
                var sorted = afterQuery.OrderByDescending(o => o.VersionSequence);
                Assert.Greater(sorted.First().VersionSequence, sorted.Skip(1).First().VersionSequence);
                sorted = afterQuery.OrderBy(o => o.VersionSequence);
                // Ordering should be oldest first
                Assert.Less(sorted.First().VersionSequence, sorted.Skip(1).First().VersionSequence);

                // Order by date
                sorted = afterQuery.OrderByDescending(o => o.CreationTime);
                Assert.Greater(sorted.First().CreationTime, sorted.Skip(1).First().CreationTime);
            }
        }

        /// <summary>
        /// Test stateful queries of entites
        /// </summary>
        [Test]
        public void TestStatefulQuery()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                Guid aaUuid = Guid.NewGuid();
                var aa = base.TestInsert<IdentityDomain>(new IdentityDomain()
                {
                    Key = aaUuid,
                    DomainName = "TESTSTRESS4",
                    Oid = "2.25.030406888",
                    Url = "http://google.test4",
                    Description = "A test thing",
                    Name = "TEST_STRESS_4"
                });

                Enumerable.Range(0, 3).ToList().ForEach(i =>
                {
                    var entity = new Entity()
                    {
                        ClassConceptKey = EntityClassKeys.LivingSubject,
                        DeterminerConceptKey = DeterminerKeys.Specific,
                        TypeConceptKey = EntityClassKeys.Place,
                        Identifiers = new List<EntityIdentifier>()
                            {
                                new EntityIdentifier(aaUuid, $"TEST_CASE4_{i}")
                            }
                    };

                    base.TestInsert(entity);
                });

                var afterQuery = base.TestQuery<Entity>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_CASE4_%")), 3).AsResultSet() as IOrderableQueryResultSet<Entity>;

                // Ordering should be newest first
                var queryService = ApplicationServiceContext.Current.GetService<TestQueryPersistenceService>();
                var qid = Guid.NewGuid();
                queryService.SetExpectedQueryStats(qid, 3);
                var stateful = afterQuery.OrderByDescending(o => o.VersionSequence).AsStateful(qid);
                Assert.Greater(stateful.First().VersionSequence, stateful.Skip(1).First().VersionSequence);

                qid = Guid.NewGuid();
                queryService.SetExpectedQueryStats(qid, 3);
                var stateful2 = afterQuery.OrderBy(o => o.VersionSequence).AsStateful(qid);
                Assert.Less(stateful2.First().VersionSequence, stateful2.Skip(1).First().VersionSequence);

                // Union should return 3
                Assert.AreEqual(3, stateful2.Union(stateful).Count());
            }
        }

        /// <summary>
        /// Test query hax
        /// </summary>
        [Test]
        public void TestQueryHax()
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
                        new EntityName(NameUseKeys.Assigned, "Some Crazy Name"),
                        new EntityName(NameUseKeys.Legal, "Smithy", "John", "E", "T")
                    },
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.Direct, "123 Test8 Street West", "Hamilton9", "ON", "CA", "L8K5N2"),
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Home Street West", "Hometown", "ON", "CA", "L8K5N2"),
                    }
                };

                var afterInsert = this.TestInsert(entity);
                var checkConcept = afterInsert.LoadProperty(o => o.TypeConcept).Mnemonic;

                // Test that the query hax works
                var afterQuery = this.TestQuery<Entity>(o => o.Names.Where(g => g.NameUseKey == NameUseKeys.Legal).Any(n => n.Component.Where(c => c.ComponentTypeKey == NameComponentKeys.Family).Any(c => c.Value == "Smithy")), 1);
                afterQuery = this.TestQuery<Entity>(o => o.TypeConcept.Mnemonic == checkConcept && o.Names.Where(g => g.NameUseKey == NameUseKeys.Legal).Any(n => n.Component.Where(c => c.ComponentTypeKey == NameComponentKeys.Family).Any(c => c.Value == "Smithy")), 1);
                afterQuery = this.TestQuery<Entity>(o => o.Names.Where(g => g.NameUseKey == NameUseKeys.Legal).Any(n => n.Component.Where(c => c.ComponentTypeKey == NameComponentKeys.Family).Any(c => c.Value == "John")), 0);
                afterQuery = this.TestQuery<Entity>(o => o.Names.Where(g => g.NameUseKey == NameUseKeys.Legal).Any(n => n.Component.Where(c => c.ComponentTypeKey == NameComponentKeys.Given).Any(c => c.Value == "John")), 1);
                afterQuery = this.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.HomeAddress).Any(n => n.Component.Where(c => c.ComponentTypeKey == AddressComponentKeys.City).Any(c => c.Value == "Hometown")), 1);
                afterQuery = this.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.HomeAddress).Any(n => n.Component.Where(c => c.ComponentTypeKey == AddressComponentKeys.State).Any(c => c.Value == "Hometown")), 0);
                afterQuery = this.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.HomeAddress).Any(n => n.AddressUseKey == AddressUseKeys.HomeAddress && n.Component.Where(c => c.ComponentTypeKey == AddressComponentKeys.State).Any(c => c.Value == "Hometown")), 0);
                afterQuery = this.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.HomeAddress).Any(n => n.AddressUseKey == AddressUseKeys.HomeAddress && n.Component.Where(c => c.ComponentTypeKey == AddressComponentKeys.City).Any(c => c.Value == "Hometown")), 1);
                afterQuery = this.TestQuery<Entity>(o => o.Addresses.Where(g => g.AddressUseKey == AddressUseKeys.HomeAddress).Any(n => n.Component.Where(c => c.ComponentTypeKey == AddressComponentKeys.City).Any(c => c.Value == "Hometown"))
                && o.Names.Where(g => g.NameUseKey == NameUseKeys.Legal).Any(n => n.Component.Where(g => g.ComponentTypeKey == NameComponentKeys.Given).Any(c => c.Value == "John")), 1);
            }
        }

        /// <summary>
        /// Tests the delete all functions
        /// </summary>
        [Test]
        public void TestDeleteAll()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                for (var i = 0; i < 10; i++)
                {
                    var entity = new Entity()
                    {
                        ClassConceptKey = EntityClassKeys.LivingSubject,
                        DeterminerConceptKey = DeterminerKeys.Specific,
                        TypeConceptKey = EntityClassKeys.Place,
                        Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Assigned, "Delete All")
                    }
                    };

                    this.TestInsert(entity);
                }

                var afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), 10);

                // Delete all
                var dpe = ApplicationServiceContext.Current.GetService<IDataPersistenceServiceEx<Entity>>();
                dpe.DeleteAll(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

                // Ensure no results on regular query
                afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), 0);
                // Ensure 10 results on inactive query
                afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")) && o.ObsoletionTime.HasValue, 10);

                // Restore all
                var oldData = afterQuery.ToList().Select(q =>
                {
                    return this.TestUpdate(q, (r) =>
                    {
                        r.StatusConceptKey = StatusKeys.Active;
                        return r;
                    });
                }).ToList();

                // Ensure 10 now results
                afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), 10);

                // Now erase
                using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
                {
                    dpe.DeleteAll(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                }
                // Ensure 0 now results
                afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")), 0);
                // Ensure 0 results on inactive query
                afterQuery = this.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Delete All")) && StatusKeys.InactiveStates.Contains(o.StatusConceptKey.Value), 0);
            }
        }
    }
}