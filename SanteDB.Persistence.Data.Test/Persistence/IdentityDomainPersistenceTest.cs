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
using SanteDB.Core.Model;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.BusinessRules;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Tests the assigning authority persistence service
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class IdentityDomainPersistenceTest : DataPersistenceTest, IIdentifierValidator
    {
        public string Name => throw new NotImplementedException();

        /// <summary>
        /// Tests that a simple assigning authority can be inserted
        /// and then retrieved
        /// </summary>
        [Test]
        public void TestInsertAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_1",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_01",
                Oid = "1.2.3",
                Url = "http://google.com/test1"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);

            // Now, re-fetch and validate
            var aa2 = persistenceService.Get(aa.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2);
            Assert.AreEqual(aa.Key, aa2.Key);
            Assert.AreEqual(aa.DomainName, aa2.DomainName);
            Assert.AreEqual(aa.Oid, aa2.Oid);
            Assert.AreEqual(aa.Url, aa2.Url);

            // Now fetch by name
            var aa3 = persistenceService.Query(o => o.Url == "http://google.com/test1", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, aa3.Count());
            Assert.AreEqual(aa.Key, aa3.First().Key);
        }

        /// <summary>
        /// Test the insert of an assigning authority with extended attributes
        /// </summary>
        [Test]
        public void TestInsertExtendedAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_2",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_02",
                Oid = "1.2.3.2",
                Url = "http://google.com/test2",
                AuthorityScopeXml = new List<Guid>()
                {
                    EntityClassKeys.Patient
                },
                AssigningAuthority = new List<AssigningAuthority>()
                {
                    new AssigningAuthority()
                    {
                        AssigningApplicationKey = Guid.Parse(AuthenticationContext.SystemApplicationSid),
                        Reliability = IdentifierReliability.Authoritative
                    }
                }
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);
            Assert.AreEqual(1, aa.AuthorityScope.Count);
            Assert.AreEqual(1, aa.AssigningAuthority.Count);

            // Now, re-fetch and validate
            var aa2 = persistenceService.Get(aa.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2);
            Assert.AreEqual(aa.Key, aa2.Key);
            Assert.AreEqual(aa.DomainName, aa2.DomainName);
            Assert.AreEqual(aa.Oid, aa2.Oid);
            Assert.AreEqual(aa.Url, aa2.Url);
            Assert.AreEqual(1, aa2.AuthorityScope.Count);
            Assert.IsNotNull(aa2.LoadProperty(o => o.AssigningAuthority));
            // Now fetch by name
            var aa3 = persistenceService.Query(o => o.Url == "http://google.com/test2", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, aa3.Count());
            Assert.AreEqual(aa.Key, aa3.First().Key);
            Assert.AreEqual(1, aa3.Single().AuthorityScopeXml.Count);
        }

        /// <summary>
        /// Test that the update of an assigning authority is successful
        /// </summary>
        [Test]
        public void TestUpdateAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_3",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_03",
                Oid = "1.2.3.3",
                Url = "http://google.com/test3"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);

            // Next update
            aa.Oid = "3.3.2.1";
            var aa2 = persistenceService.Update(aa, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            Assert.AreEqual("3.3.2.1", aa2.Oid);
        }

        /// <summary>
        /// Tests the adding / removing of related objects
        /// </summary>
        [Test]
        public void TestUpdateExtendedAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_4",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_04",
                Oid = "1.2.3.4",
                Url = "http://google.com/test4",
                AuthorityScopeXml = new List<Guid>()
                {
                    EntityClassKeys.Patient
                }
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);
            Assert.AreEqual(1, aa.AuthorityScope.Count);

            // Add a scope
            aa.AuthorityScopeXml.Add(EntityClassKeys.Material);
            var aa2 = persistenceService.Update(aa, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(2, aa2.AuthorityScopeXml.Count);

            // Get and validate
            var aa3 = persistenceService.Get(aa.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(2, aa3.AuthorityScopeXml.Count);

            // Remove scopes
            aa3.AuthorityScopeXml.Remove(EntityClassKeys.Patient);
            var aa4 = persistenceService.Update(aa3, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, aa4.AuthorityScopeXml.Count);
            Assert.AreEqual(EntityClassKeys.Material, aa4.AuthorityScopeXml.Single());
        }

        /// <summary>
        /// Tests that obsoletiong of a simple assigning authority removal
        /// </summary>
        [Test]
        public void TestObsoleteAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_5",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_05",
                Oid = "1.2.3.5",
                Url = "http://google.com/test5"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2.ObsoletionTime);
            Assert.IsNotNull(aa2.ObsoletedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa2.ObsoletedByKey.ToString());

            // Validate that the AA is retrievable by fetch
            var aa3 = persistenceService.Get(aa.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa3);

            // Validate that AA is not found via the query method
            var aa4 = persistenceService.Query(o => o.Url == "http://google.com/test5", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(0, aa4.Count());

            // Validate that AA can be found when explicitly querying for obsoleted
            var aa5 = persistenceService.Query(o => o.Url == "http://google.com/test5" && o.ObsoletionTime != null, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, aa5.Count());
        }

        /// <summary>
        /// Test obsoletion of an extended assigning authority
        /// </summary>
        [Test]
        public void TestObsoleteExtendedAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_6",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_06",
                Oid = "1.2.3.6",
                Url = "http://google.com/test6",
                AuthorityScopeXml = new List<Guid>()
                {
                    EntityClassKeys.Patient
                }
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);
            Assert.AreEqual(1, aa.AuthorityScope.Count);

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2.ObsoletionTime);
            Assert.IsNotNull(aa2.ObsoletedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa2.ObsoletedByKey.ToString());
        }

        /// <summary>
        /// Tests the un-deletion of an AA
        /// </summary>
        [Test]
        public void TestUnDelete()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new IdentityDomain()
            {
                Description = "A test authority",
                DomainName = "TEST_AA_7",
                IsUnique = true,
                CustomValidator = "TEST.VALIDATOR",
                Name = "TEST_AA_07",
                Oid = "1.2.3.7",
                Url = "http://google.com/test7",
                AuthorityScopeXml = new List<Guid>()
                {
                    EntityClassKeys.Patient
                }
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);
            Assert.AreEqual(1, aa.AuthorityScope.Count);

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2.ObsoletionTime);
            Assert.IsNotNull(aa2.ObsoletedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa2.ObsoletedByKey.ToString());

            // Validate that AA is not found via the query method
            var aa3 = persistenceService.Query(o => o.Url == "http://google.com/test7", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(0, aa3.Count());

            var aa4 = persistenceService.Update(aa2, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNull(aa4.ObsoletedByKey);
            Assert.IsNull(aa4.ObsoletionTime);
            Assert.IsNotNull(aa4.UpdatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa4.UpdatedByKey.ToString());

            // Validate that AA is now found via the query method
            var aa5 = persistenceService.Query(o => o.Url == "http://google.com/test7", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, aa5.Count());
        }

        /// <summary>
        /// Test the deletion of all 
        /// </summary>
        [Test]
        public void TestDeleteAll()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                Enumerable.Range(1, 9).ToList().ForEach(i =>
                 {
                     base.TestInsert(new IdentityDomain()
                     {
                         Description = "A test authority",
                         DomainName = $"TEST_OBSOLETE_{i}",
                         IsUnique = true,
                         CustomValidator = "TEST.VALIDATOR",
                         Name = $"TEST_AA_OBS_{i}",
                         Oid = $"1.2.3.8.{i}",
                         Url = $"http://google.com/test_obs_{i}",
                         AuthorityScopeXml = new List<Guid>()
                         {
                            EntityClassKeys.Patient
                         }
                     });

                 });

                // Test query 
                base.TestQuery<IdentityDomain>(o => o.Oid.Contains("1.2.3.8.%"), 9);
                base.TestQuery<IdentityDomain>(o => o.DomainName.Contains("TEST_OBSOLETE_%"), 9);

                // Now - obsolete all
                var pservice = ApplicationServiceContext.Current.GetService<IDataPersistenceService<IdentityDomain>>() as IDataPersistenceServiceEx<IdentityDomain>;
                pservice.DeleteAll(o => o.Oid.Contains("1.2.3.8.%"), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                base.TestQuery<IdentityDomain>(o => o.Oid.Contains("1.2.3.8.%"), 0); // No results 
                base.TestQuery<IdentityDomain>(o => o.Oid.Contains("1.2.3.8.%") && o.ObsoletionTime != null, 9);

                // Now permadelete
                pservice.DeleteAll(o => o.Oid.Contains("1.2.3.8.%") && o.ObsoletionTime != null, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                base.TestQuery<IdentityDomain>(o => o.Oid.Contains("1.2.3.8.%"), 0); // No results 
                base.TestQuery<IdentityDomain>(o => o.Oid.Contains("1.2.3.8.%") && o.ObsoletionTime != null, 0);

                // This should succeed since we've purged
                Enumerable.Range(1, 9).ToList().ForEach(i =>
                {
                    base.TestInsert(new IdentityDomain()
                    {
                        Description = "A test authority",
                        DomainName = $"TEST_OBSOLETE_{i}",
                        IsUnique = true,
                        CustomValidator = "TEST.VALIDATOR",
                        Name = $"TEST_AA_OBS_{i}",
                        Oid = $"1.2.3.8.{i}",
                        Url = $"http://google.com/test_obs_{i}",
                        AuthorityScopeXml = new List<Guid>()
                         {
                            EntityClassKeys.Animal
                         }
                    });

                });

                base.TestQuery<IdentityDomain>(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal), 9); // 9 results 

                // Now - obsolete all
                pservice.DeleteAll(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                base.TestQuery<IdentityDomain>(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal), 0); // No results 
                base.TestQuery<IdentityDomain>(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal) && o.ObsoletionTime != null, 9);

                // Now permadelete
                using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
                {
                    pservice.DeleteAll(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal) && o.ObsoletionTime != null, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                }
                base.TestQuery<IdentityDomain>(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal), 0); // No results 
                base.TestQuery<IdentityDomain>(o => o.AuthorityScope.Any(s => s.Key == EntityClassKeys.Animal) && o.ObsoletionTime != null, 0);
            }
        }

        /// <summary>
        /// Tests that scope enforcement occurs
        /// </summary>
        [Test]
        public void TestAuthorityEnforcement()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                // Insert an assigning authority
                var patientsOnly = new IdentityDomain()
                {
                    Description = "For Patients Only",
                    DomainName = "TEST_AA_PATIENTS",
                    IsUnique = true,
                    CustomValidator = this.GetType().AssemblyQualifiedName,
                    ValidationRegex = @"^P\d$",
                    Name = "TEST_AA_PATIENTS",
                    Oid = "1.2.3.999",
                    Url = "http://google.com/patients",
                    AuthorityScopeXml = new List<Guid>()
                {
                    EntityClassKeys.Patient
                }
                };

                patientsOnly = base.TestInsert(patientsOnly);

                // We SHOULD be able to register a patient having id value length = 2
                var patient = base.TestInsert(new Patient()
                {
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(patientsOnly, "P0")
                    }
                });

                // We should not be able to register a duplicate identifier
                patient = base.TestInsert(new Patient()
                {
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(patientsOnly, "P0")
                    }
                });
                patient = base.TestQuery<Patient>(o => o.Identifiers.Any(i => i.Value == "P0"), 2).AsResultSet().OrderByDescending(o=>o.VersionSequence).First();
                Assert.AreEqual(1, patient.LoadProperty(o => o.Extensions).Count);
                Assert.AreEqual(ExtensionTypeKeys.DataQualityExtension, patient.Extensions[0].ExtensionTypeKey);
                var extension = patient.Extensions[0].GetValue<List<DetectedIssue>>();
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierNotUnique), "Expected a not unique issue");

                // We should be able to register a identifier validation but the result should have a detected issue
                patient = base.TestInsert(new Patient()
                {
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(patientsOnly, "P000000") // <--- THIS WILL FAIL VALIDATION
                    }
                });
                patient = base.TestQuery<Patient>(o => o.Identifiers.Any(i => i.Value == "P000000"), 1).First();
                Assert.AreEqual(1, patient.LoadProperty(o => o.Extensions).Count);
                Assert.AreEqual(ExtensionTypeKeys.DataQualityExtension, patient.Extensions[0].ExtensionTypeKey);
                extension = patient.Extensions[0].GetValue<List<DetectedIssue>>();
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierCheckDigitFailed), "Expected a check digit fail issue");
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierPatternFormatFail), "Expected a format fail issue");

                // We should not be able to register an act with a patient identifier
                var act = base.TestInsert(new Act()
                {
                    ClassConceptKey = ActClassKeys.Act,
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ActTime = DateTimeOffset.Now,
                    Identifiers = new List<ActIdentifier>()
                    {
                        new ActIdentifier(patientsOnly, "A00")
                    }
                });
                act = base.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value == "A00"), 1).First();
                Assert.AreEqual(1, act.LoadProperty(o => o.Extensions).Count);
                Assert.AreEqual(ExtensionTypeKeys.DataQualityExtension, act.Extensions[0].ExtensionTypeKey);
                extension = act.Extensions[0].GetValue<List<DetectedIssue>>();
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierCheckDigitFailed), "Expected a check digit fail issue");
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierPatternFormatFail), "Expected a format fail issue");
                Assert.IsTrue(extension.Any(r => r.Id == DataConstants.IdentifierInvalidTargetScope), "Expected a scope fail issue");

            }
        }

        /// <summary>
        /// Returns if the identifier is valid
        /// </summary>
        public bool IsValid(IExternalIdentifier id)
        {
            return id.Value.Length == 2;
        }
    }
}