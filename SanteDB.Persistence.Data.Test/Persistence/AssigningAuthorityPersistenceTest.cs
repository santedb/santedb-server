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

        public void TestUpdateAssigningAuthority()
        {

        }

        public void TestUpdateExtendedAssigningAuthority()
        {

        }

        public void TestObsoleteAssigningAuthority()
        {

        }

        public void TestObsoleteExtendedAssigningAuthority()
        {

        }

    }
}
