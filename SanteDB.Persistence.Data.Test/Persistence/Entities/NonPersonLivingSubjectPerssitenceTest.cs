using SanteDB.Core.Model;
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

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Persistence tests for non-person living subject (food, chemical substances, etc.)
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class NonPersonLivingSubjectPerssitenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests the NPLS persistence service
        /// </summary>
        [Test]
        public void TestWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var npls = new NonPersonLivingSubject()
                {
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Assigned, "A New Disease")
                    },
                    StrainKey = NullReasonKeys.NoInformation
                };

                var afterInsert = base.TestInsert(npls);
                Assert.IsInstanceOf<NonPersonLivingSubject>(afterInsert);

                var afterQuery = base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-NoInformation", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Strain);
                Assert.AreEqual("NullFlavor-NoInformation", afterQuery.LoadProperty(o => o.Strain).Mnemonic);

                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.StrainKey = NullReasonKeys.UnEncoded;
                    o.Strain = null;
                    return o;
                });

                base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-NoInformation", 0);
                base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-UnEncoded", 1);

                // Test with entity persistence


            }
        }
    }
}
