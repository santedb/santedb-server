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

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Test the persistence of acts
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ActPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test the insert and query back of a basic 
        /// </summary>
        [Test]
        public void TestInsertBasicAct()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ActTime = DateTime.Now,
                    ClassConceptKey = ActClassKeys.Battery,
                    TypeConceptKey = ObservationTypeKeys.ClinicalState,
                    IsNegated = true,
                    MoodConceptKey = MoodConceptKeys.Goal,
                    ReasonConceptKey = ActReasonKeys.Broken
                };

                // Test insert
                var afterInsert = base.TestInsert(act);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterInsert.StatusConceptKey);
                Assert.AreEqual(ActClassKeys.Battery, afterInsert.ClassConceptKey);
                Assert.AreEqual(ObservationTypeKeys.ClinicalState, afterInsert.TypeConceptKey);
                Assert.AreEqual(ActReasonKeys.Broken, afterInsert.ReasonConceptKey);
                Assert.IsTrue(afterInsert.IsNegated);

            }
        }

        [Test]
        public void TestInsertActTemplates()
        {

        }

        [Test]
        public void TestTagPersistence()
        {

        }

        [Test]
        public void TestInsertActFull()
        {

        }

        [Test]
        public void TestUpdateAct()
        {

        }

        [Test]
        public void TestObsoleteAct()
        {

        }

        [Test]
        public void TestQueryAct()
        {

        }

        [Test]
        public void TestActOrdering()
        {

        }

        [Test]
        public void TestStatefulQuery()
        {

        }

        [Test]
        public void TestDeleteAll()
        {

        }
    }
}
