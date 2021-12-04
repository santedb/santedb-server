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
using SanteDB.Core;
using SanteDB.Core.Security.Services;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Tests for Organization
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Device Entity")]
    [ExcludeFromCodeCoverage]
    public class DeviceEntityPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var deviceService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
                var device = deviceService.CreateIdentity("SOME_DEVICE_ID", "BLAH", AuthenticationContext.SystemPrincipal);
                var deventity = new DeviceEntity()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Some Device")
                    },
                    SecurityDeviceKey = deviceService.GetSid("SOME_DEVICE_ID"),
                    ManufacturerModelName = "Test Manufacturer",
                    OperatingSystemName = "Operating System Inc. OS 4",
                    GeoTag = new Core.Model.DataTypes.GeoTag(12, 13, false)
                };

                // Perform the insert
                var afterInsert = base.TestInsert(deventity);
                Assert.AreEqual("Test Manufacturer", afterInsert.ManufacturerModelName);
                Assert.AreEqual("Operating System Inc. OS 4", afterInsert.OperatingSystemName);
                Assert.IsNull(afterInsert.SecurityDevice);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.SecurityDevice));
                Assert.AreEqual("SOME_DEVICE_ID", afterInsert.SecurityDevice.Name);
                Assert.IsNull(afterInsert.GeoTag);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.GeoTag));

                // Now we want to query
                var afterQuery = base.TestQuery<DeviceEntity>(o => o.ManufacturerModelName == "Test Manufacturer" && o.OperatingSystemName == "Operating System Inc. OS 4", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Test Manufacturer", afterQuery.ManufacturerModelName);
                Assert.AreEqual("Operating System Inc. OS 4", afterQuery.OperatingSystemName);
                Assert.IsNull(afterQuery.GeoTag);
                Assert.IsNotNull(afterQuery.GeoTagKey);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.GeoTag));
                Assert.AreEqual(12.0f, afterQuery.GeoTag.Lat);
                // Update the key
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.OperatingSystemName = "Operating System Inc. OS 5";
                    o.ManufacturerModelName = "Some Test Manufacturer";
                    return o;
                });
                Assert.AreEqual("Operating System Inc. OS 5", afterUpdate.OperatingSystemName);

                afterQuery = base.TestQuery<DeviceEntity>(o => o.ManufacturerModelName == "Test Manufacturer" && o.OperatingSystemName == "Operating System Inc. OS 4", 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<DeviceEntity>(o => o.ManufacturerModelName == "Some Test Manufacturer" && o.OperatingSystemName == "Operating System Inc. OS 5", 1).AsResultSet().FirstOrDefault();
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
                var deviceService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
                var device = deviceService.CreateIdentity("SOME_DEVICE_ID2", "BLAH", AuthenticationContext.SystemPrincipal);
                var deventity = new DeviceEntity()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Some Device 2")
                    },
                    SecurityDeviceKey = deviceService.GetSid("SOME_DEVICE_ID2"),
                    ManufacturerModelName = "Test Manufacturer",
                    OperatingSystemName = "Operating System Inc. OS 4"
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(deventity);
                Assert.IsInstanceOf<DeviceEntity>(afterInsert);

                // Now we want to query
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Some Device 2")), 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsInstanceOf<DeviceEntity>(afterQuery);
                Assert.AreEqual("Test Manufacturer", (afterQuery as DeviceEntity).ManufacturerModelName);

                // Update the key
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, (o) =>
                {
                    (o as DeviceEntity).OperatingSystemName = "Operating System Inc. OS 5";
                    return o;
                });
                Assert.AreEqual("Operating System Inc. OS 5", (afterUpdate as DeviceEntity).OperatingSystemName);

                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Some Device 2")), 1).AsResultSet().FirstOrDefault();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Operating System Inc. OS 5", (afterQuery as DeviceEntity).OperatingSystemName);
            }
        }
    }
}