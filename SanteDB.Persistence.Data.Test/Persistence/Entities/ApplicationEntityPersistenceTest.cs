using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Tests for Organization
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Application Entity")]
    [ExcludeFromCodeCoverage]
    public class ApplicationEntityPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var appentity = new ApplicationEntity()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Some Application")
                    },
                    SecurityApplicationKey = Guid.Parse(AuthenticationContext.SystemApplicationSid),
                    SoftwareName = "Test Software",
                    VendorName = "Test Software Inc.",
                    VersionName = "2.1.3"
                };

                // Perform the insert
                var afterInsert = base.TestInsert(appentity);
                Assert.AreEqual("Test Software", afterInsert.SoftwareName);
                Assert.AreEqual("Test Software Inc.", afterInsert.VendorName);
                Assert.AreEqual("2.1.3", afterInsert.VersionName);

                // Now we want to query
                var afterQuery = base.TestQuery<ApplicationEntity>(o => o.SoftwareName == "Test Software" && o.VendorName == "Test Software Inc." && o.VersionName == "2.1.3", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Test Software", afterQuery.SoftwareName);
                Assert.AreEqual("Test Software Inc.", afterQuery.VendorName);
                Assert.AreEqual("2.1.3", afterQuery.VersionName);

                // Update the key
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.VersionName = "2.1.5";
                    o.SoftwareName = "Some Test Software";
                    return o;
                });
                Assert.AreEqual("2.1.5", afterUpdate.VersionName);

                afterQuery = base.TestQuery<ApplicationEntity>(o => o.SoftwareName == "Test Software" && o.VendorName == "Test Software Inc." && o.VersionName == "2.1.3", 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<ApplicationEntity>(o => o.SoftwareName == "Some Test Software" && o.VendorName == "Test Software Inc." && o.VersionName == "2.1.5", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
            }
        }
    }
}