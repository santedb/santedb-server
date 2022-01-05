using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// User entity persistence test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ProviderPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Ensures that the persistence of a provider entity with the proper persistence service
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var provider = new Provider()
                {
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street East", "Hamilton", "ON", "CA", "L8K5N2")
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, "Test", "Provider")
                    },
                    LanguageCommunication = new List<PersonLanguageCommunication>()
                    {
                        new PersonLanguageCommunication("en", true)
                    },
                    Specialty = new Concept()
                    {
                        Mnemonic = "Specialty-Test",
                        ClassKey = ConceptClassKeys.Other,
                        StatusConceptKey = StatusKeys.Active
                    },
                    DateOfBirth = new DateTime(1984, 05, 04)
                };

                // Insert user entity
                var afterInsert = base.TestInsert(provider);
                Assert.IsInstanceOf<Provider>(provider);
                Assert.IsNull(afterInsert.Specialty);
                Assert.AreEqual("Specialty-Test", afterInsert.LoadProperty(o => o.Specialty).Mnemonic);

                // Attempt lookup by user name
                var afterQuery = base.TestQuery<Provider>(o => o.Specialty.Mnemonic == "Specialty-Test", 1).AsResultSet().First();
                Assert.AreEqual(afterQuery.Key, afterInsert.Key);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);
                Assert.IsNull(afterQuery.Specialty);
                Assert.AreEqual("Specialty-Test", afterQuery.LoadProperty(o => o.Specialty).Mnemonic);

            }

        }

    }
}
