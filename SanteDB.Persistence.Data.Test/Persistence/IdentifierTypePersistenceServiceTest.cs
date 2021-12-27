using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Identifier type persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class IdentifierTypePersistenceServiceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion of the identiifer type
        /// </summary>
        [Test]
        public void TestInsertIdentifierType()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var identifierType = new IdentifierType()
                {
                    ScopeConceptKey = EntityClassKeys.Patient,
                    TypeConceptKey = IdentifierTypeKeys.Bank
                };

                var afterInsert = base.TestInsert(identifierType);
                Assert.IsNull(afterInsert.ScopeConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.ScopeConcept));

                identifierType = new IdentifierType()
                {
                    TypeConcept = new Concept()
                    {
                        Key = IdentifierTypeKeys.Bank,
                        Mnemonic = "IdentifierType-Bank"
                    },
                    ScopeConcept = new Concept()
                    {
                        Key = EntityClassKeys.Patient
                    }
                };

                afterInsert = base.TestInsert(identifierType);
                Assert.IsNull(afterInsert.ScopeConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.ScopeConcept));
            }
        }
    }
}