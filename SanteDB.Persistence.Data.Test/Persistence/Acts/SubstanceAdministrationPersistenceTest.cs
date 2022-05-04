using SanteDB.Core.Model;
using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Exceptions;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Tests for SBADM
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class SubstanceAdministrationPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Ensures that substance administrations with the proper persistence layer will store and load data
        /// </summary>
        [Test]
        public void TestPersistWithProper()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var sbadm = new SubstanceAdministration()
                {
                    ActTime = DateTimeOffset.Now.Date,
                    DoseQuantity = (decimal)1,
                    DoseUnitKey = UnitOfMeasureKeys.Dose,
                    IsNegated = true,
                    RouteKey = RouteOfAdministrationKeys.InjectionIntramuscular,
                    SiteKey = AdministrationSiteKeys.LeftArm,
                    MoodConceptKey = MoodConceptKeys.Intent
                };

                var afterInsert = base.TestInsert(sbadm);
                Assert.AreEqual(RouteOfAdministrationKeys.InjectionIntramuscular, afterInsert.RouteKey);
                Assert.AreEqual(AdministrationSiteKeys.LeftArm, afterInsert.SiteKey);
                Assert.AreEqual(UnitOfMeasureKeys.Dose, afterInsert.DoseUnitKey);
                Assert.IsNull(afterInsert.DoseUnit);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.DoseUnit));
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.Route ));
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.Site));
                Assert.IsTrue(afterInsert.IsNegated);

                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 1);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 2 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Each, 0);
                var afterQuery = base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 1).First();
                Assert.AreEqual(RouteOfAdministrationKeys.InjectionIntramuscular, afterQuery.RouteKey);
                Assert.AreEqual(AdministrationSiteKeys.LeftArm, afterQuery.SiteKey);
                Assert.AreEqual(UnitOfMeasureKeys.Dose, afterQuery.DoseUnitKey);
                Assert.AreEqual(1, afterQuery.DoseQuantity);
                Assert.AreEqual(MoodConceptKeys.Intent, afterQuery.MoodConceptKey);
                Assert.IsNull(afterQuery.DoseUnit);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.DoseUnit));
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.Route));
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.Site));
                Assert.IsTrue(afterQuery.IsNegated);

                // Test update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.IsNegated = false;
                    o.MoodConceptKey = ActMoodKeys.Eventoccurrence;
                    o.DoseQuantity = 100;
                    o.DoseUnitKey = UnitOfMeasureKeys.Percentage;
                    return o;
                });
                Assert.IsFalse(afterUpdate.IsNegated);
                Assert.AreEqual(ActMoodKeys.Eventoccurrence, afterUpdate.MoodConceptKey);

                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 2 && o.DoseUnitKey == UnitOfMeasureKeys.Dose, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == true && o.DoseQuantity == 1 && o.DoseUnitKey == UnitOfMeasureKeys.Each, 0);
                afterQuery = base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence, 1).First();

                // Test delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence, 0);
                afterQuery = base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence && o.ObsoletionTime != null, 1).First();

                // Test un-delete
                base.TestUpdate(afterQuery, o =>
                {
                    return o;
                });
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence, 1);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence && o.ObsoletionTime != null, 0);

                // Perma delete
                base.TestDelete(afterUpdate, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence, 0);
                base.TestQuery<SubstanceAdministration>(o => o.IsNegated == false && o.DoseQuantity == 100 && o.DoseUnitKey == UnitOfMeasureKeys.Percentage && o.MoodConceptKey == MoodConceptKeys.Eventoccurrence && o.ObsoletionTime != null, 0);

                // Should not update
                try
                {
                    base.TestUpdate(afterQuery, o => o);
                    Assert.Fail("Should throw exception!");
                }
                catch(DataPersistenceException d) when (d.InnerException is KeyNotFoundException) { }
                catch
                {
                    Assert.Fail("Incorrect exception type thrown");
                }


            }
        }

    }
}
