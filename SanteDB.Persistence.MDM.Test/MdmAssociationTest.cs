using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Caching.Memory;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Persistence.Data.ADO.Test;
using SanteDB.Persistence.MDM.Services;

namespace SanteDB.Persistence.MDM.Test
{
    /// <summary>
    /// Tests the MDM daemon service for testing capabilities
    /// </summary>
    [TestClass]
    public class MdmAssociationTest : DataTest
    {

        /// <summary>
        /// Setup the test class
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            typeof(MdmDaemonService).Equals(null); // Trick - Force test context to load
            typeof(MemoryCacheService).Equals(null);
            DataTest.DataTestUtil.Start(context);
        }

        /// <summary>
        /// Test - When there is a record created for a subscribed type, the system should create a MASTER reference
        /// </summary>
        [TestMethod]
        public void TestShouldCreateMasterRecord()
        {
            var patientUnderTest = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-02-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "10239")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };

            try
            {
                var createdPatient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Insert(patientUnderTest, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            // The creation / matching of a master record may take some time, so we need to wait for the matcher to finish
            Thread.Sleep(1000);
        }
    }
}
