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
    [TestFixture(Category = "Persistence")]
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

        /// <summary>
        /// Ensures that the generic Entity persistence service inserts the appropriate organization data (i.e.
        /// it detects the presence of an organization and inserts it)
        /// </summary>
        [Test]
        public void TestInsertWithImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var appentity = new ApplicationEntity()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Some Application 2")
                    },
                    SecurityApplicationKey = Guid.Parse(AuthenticationContext.SystemApplicationSid),
                    SoftwareName = "Test Software 99",
                    VendorName = "Test Software Inc. 99",
                    VersionName = "2.1.3"
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(appentity) as ApplicationEntity;
                Assert.IsNotNull(afterInsert);
                Assert.AreEqual("Test Software 99", afterInsert.SoftwareName);
                Assert.AreEqual("Test Software Inc. 99", afterInsert.VendorName);
                Assert.AreEqual("2.1.3", afterInsert.VersionName);
                Assert.IsNull(afterInsert.SecurityApplication);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.SecurityApplication));
                Assert.AreEqual("SYSTEM", afterInsert.SecurityApplication.Name);

                // Query using entity
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Some Application 2")), 1).AsResultSet().First();
                Assert.IsInstanceOf<ApplicationEntity>(afterQuery);
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Test Software 99", (afterQuery as ApplicationEntity).SoftwareName);
                Assert.AreEqual("Test Software Inc. 99", (afterQuery as ApplicationEntity).VendorName);
                Assert.AreEqual("2.1.3", (afterQuery as ApplicationEntity).VersionName);

                // Update the key
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, (o) =>
                {
                    (o as ApplicationEntity).SoftwareName = "Test Software 2";
                    return o;
                }) as ApplicationEntity;
                Assert.IsNotNull(afterUpdate);
                Assert.AreEqual("Test Software 2", afterUpdate.SoftwareName);

                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Some Application 2")), 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Test Software 2", (afterQuery as ApplicationEntity).SoftwareName);
            }
        }

        /// <summary>
        /// Test retrieve with improper provider
        /// </summary>
        [Test]
        public void TestRetreiveImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var appentity = new ApplicationEntity()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Some Application 3")
                    },
                    SecurityApplicationKey = Guid.Parse(AuthenticationContext.SystemApplicationSid),
                    SoftwareName = "Test Software 22",
                    VendorName = "Test Software Inc. 22",
                    VersionName = "2.1.5"
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(appentity) as ApplicationEntity;
                Assert.IsNotNull(afterInsert);

                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Some Application 3")), 1);
                Assert.IsInstanceOf<ApplicationEntity>(afterQuery.First());
                Assert.AreEqual("Test Software 22", afterQuery.OfType<ApplicationEntity>().First().SoftwareName);
            }
        }
    }
}