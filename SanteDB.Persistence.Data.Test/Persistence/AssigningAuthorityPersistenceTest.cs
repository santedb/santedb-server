using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Tests the assigning authority persistence service
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO AssigningAuthority")]
    public class AssigningAuthorityPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Tests that a simple assigning authority can be inserted
        /// and then retrieved
        /// </summary>
        [Test]
        public void TestInsertAssigningAuthority()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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
                }
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(aa.Key);
            Assert.IsNotNull(aa.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, aa.CreatedByKey.ToString());
            Assert.IsNotNull(aa.CreationTime);
            Assert.AreEqual(1, aa.AuthorityScope.Count);

            // Now, re-fetch and validate
            var aa2 = persistenceService.Get(aa.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(aa2);
            Assert.AreEqual(aa.Key, aa2.Key);
            Assert.AreEqual(aa.DomainName, aa2.DomainName);
            Assert.AreEqual(aa.Oid, aa2.Oid);
            Assert.AreEqual(aa.Url, aa2.Url);
            Assert.AreEqual(1, aa2.AuthorityScope.Count);

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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal, DeleteMode.LogicalDelete);
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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal, DeleteMode.LogicalDelete);
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
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var aa = persistenceService.Insert(new AssigningAuthority()
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

            var aa2 = persistenceService.Delete(aa.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal, DeleteMode.LogicalDelete);
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
    }
}