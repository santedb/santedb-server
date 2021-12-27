using NUnit.Framework;
using SanteDB.Core;
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
    /// Test for persistence of materials
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ContainerPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var container = new Container()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "OPV 100 Quantity")
                    },
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    CapacityQuantity = 10,
                    DiameterQuantity = 5,
                    HeightQuantity = 10
                };

                // Perform the insert
                var afterInsert = base.TestInsert(container);
                Assert.IsNull(afterInsert.QuantityConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(10, afterInsert.CapacityQuantity);
                Assert.AreEqual(10, afterInsert.HeightQuantity);
                Assert.AreEqual(5, afterInsert.DiameterQuantity);

                // Now we want to query
                var afterQuery = base.TestQuery<Container>(o => o.Names.Any(n=>n.Component.Any(c=>c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 10, 1).AsResultSet().First();
                Assert.IsNull(afterQuery.QuantityConcept);
                Assert.AreEqual(10, afterQuery.CapacityQuantity);
                Assert.AreEqual(10, afterQuery.HeightQuantity);
                Assert.AreEqual(5, afterQuery.DiameterQuantity);

                // Update the expiry and form
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.DiameterQuantity = 10;
                    o.HeightQuantity = 20;
                    o.CapacityQuantity = 20;
                    return o;
                });
                afterQuery = base.TestQuery<Container>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 10, 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<Container>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 20, 1).AsResultSet().FirstOrDefault();
                
                Assert.IsNull(afterQuery.QuantityConcept);
                Assert.AreEqual(20, afterQuery.CapacityQuantity);
                Assert.AreEqual(20, afterQuery.HeightQuantity);
                Assert.AreEqual(10, afterQuery.DiameterQuantity);

            }
        }

    }
}