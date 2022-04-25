using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using SanteDB.Core.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Coded observation persistence test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class QuantityObservationPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Test persistence with proper persistence classes
        /// </summary>
        [Test]
        public void TestPersistWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                // Goal => WEIGHT
                var quantityObservation = new QuantityObservation()
                {
                    ActTime = DateTimeOffset.Now.Date,
                    MoodConceptKey = ActMoodKeys.Goal,
                    TypeConceptKey = Guid.Parse("a261f8cd-69b0-49aa-91f4-e6d3e5c612ed"),
                    InterpretationConceptKey = ActInterpretationKeys.AbnormalHigh,
                    Value = (decimal)65.2,
                    UnitOfMeasureKey = UnitOfMeasureKeys.Kilograms
                };

                var afterInsert = base.TestInsert(quantityObservation);
                Assert.IsNotNull(afterInsert.UnitOfMeasureKey);
                Assert.AreEqual("UnitOfMeasure-Kilograms", afterInsert.LoadProperty(o => o.UnitOfMeasure).Mnemonic);

                // Test for querying
                base.TestQuery<QuantityObservation>(o => o.Value > 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 1);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 0);
                var afterQuery = base.TestQuery<QuantityObservation>(o => o.TypeConceptKey == VitalSignObservationTypeKeys.Weight && o.Value > 20, 1).First();
                Assert.IsNull(afterQuery.UnitOfMeasure);
                Assert.IsNull(afterQuery.InterpretationConcept);
                Assert.AreEqual("UnitOfMeasure-Kilograms", afterQuery.LoadProperty(o => o.UnitOfMeasure).Mnemonic);
                Assert.AreEqual("AbnormalHigh", afterQuery.LoadProperty(o => o.InterpretationConcept).Mnemonic);

                // Test update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Value = (decimal)6.52;
                    return o;
                });
                Assert.AreEqual(6.52, afterUpdate.Value);
                Assert.AreEqual(65.2, (afterUpdate.GetPreviousVersion() as QuantityObservation).Value);

                base.TestQuery<QuantityObservation>(o => o.Value > 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 0);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 1);

                // Delete
                base.TestDelete(afterInsert, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 0);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms" && o.ObsoletionTime != null, 1);

                // Un-delete
                base.TestUpdate(afterQuery, o =>
                {
                    return o;
                });
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 1);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms" && o.ObsoletionTime != null, 0);

                // Test perma delete
                base.TestDelete(afterInsert, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms", 0);
                base.TestQuery<QuantityObservation>(o => o.Value < 10 && o.UnitOfMeasure.Mnemonic == "UnitOfMeasure-Kilograms" && o.ObsoletionTime != null, 0);

                // should fail on update
                try
                {
                    base.TestUpdate(afterQuery, o =>
                    {
                        return o;
                    });
                    Assert.Fail("Should have thrown exception");
                }
                catch (DataPersistenceException e) when (e.InnerException is KeyNotFoundException k) { }
                catch { Assert.Fail("Wrong exception type thrown"); }
            }
        }
    }

}
