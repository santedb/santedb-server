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
using SanteDB.Core.Model;

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
            using (AuthenticationContext.EnterSystemContext())
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

                // Test fetch
                var afterQuery = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(StatusKeys.New, afterQuery.StatusConceptKey);
                Assert.AreEqual(ActClassKeys.Battery, afterQuery.ClassConceptKey);
                Assert.AreEqual(ObservationTypeKeys.ClinicalState, afterQuery.TypeConceptKey);
                Assert.AreEqual(ActReasonKeys.Broken, afterQuery.ReasonConceptKey);
                Assert.IsTrue(afterQuery.IsNegated);

            }
        }

        /// <summary>
        /// Test the insertion of an act with template defined
        /// </summary>
        [Test]
        public void TestInsertActTemplates()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ActTime = DateTime.Now,
                    ClassConceptKey = ActClassKeys.Condition,
                    IsNegated = false,
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    Protocols = new List<ActProtocol>()
                    {
                        new ActProtocol()
                        {
                            Sequence = 1,
                            Protocol = new Protocol()
                            {
                                Definition = new byte[] {1, 2, 3, 4},
                                HandlerClass = typeof(String),
                                Name = "Some Name This Is",
                                Oid = "2.25.40430439493043"
                            },
                            StateData = new byte[] { 1,2,3,4 }
                        }
                    },
                    ReasonConceptKey = ActReasonKeys.ProfessionalJudgement,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = ObservationTypeKeys.Problem,
                    Template = new Core.Model.DataTypes.TemplateDefinition()
                    {
                        Description = "A sample template",
                        Mnemonic = "act.sample",
                        Name = "ThisSample",
                        Oid = "2.25.4040494"
                    }
                };

                var afterInsert = base.TestInsert(act);
                Assert.AreEqual(1, afterInsert.Protocols.Count);
                Assert.IsNull(afterInsert.Template);
                Assert.AreEqual("act.sample", afterInsert.LoadProperty(o => o.Template).Mnemonic);

                var afterQuery = base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Template);
                Assert.AreEqual("act.sample", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.IsNull(afterQuery.Protocols);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(1, afterQuery.Protocols[0].Sequence);
                Assert.IsNull(afterQuery.Protocols[0].Protocol);
                Assert.AreEqual("2.25.40430439493043", afterQuery.Protocols[0].LoadProperty(o => o.Protocol).Oid);
                base.TestQuery<Act>(o => o.Protocols.Any(p=>p.Protocol.Oid == "2.25.40430439493043"), 1);

                // Try adding protocol
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Template = new Core.Model.DataTypes.TemplateDefinition()
                    {
                        Description = "Another sample template",
                        Mnemonic = "act.sample2",
                        Name = "ThisSample2",
                        Oid = "2.25.4040494232"
                    };
                    o.TemplateKey = null;
                    o.Protocols.Add(new ActProtocol()
                    {
                        Sequence = 2,
                        Protocol = new Protocol()
                        {
                            Definition = Encoding.UTF8.GetBytes("TesT"),
                            Name = "This is a name",
                            Oid = "2.2.34343433",
                            HandlerClass = typeof(String)
                        }
                    });
                    return o;
                });
                Assert.AreEqual(2, afterUpdate.Protocols.Count);

                base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample", 0);
                base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample2", 1);
                afterQuery = base.TestQuery<Act>(o => o.Protocols.Any(p => p.Protocol.Oid == "2.2.34343433"), 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Template);
                Assert.AreEqual("act.sample2", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.IsNull(afterQuery.Protocols);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(1, afterQuery.Protocols[0].Sequence);
                Assert.AreEqual(2, afterQuery.Protocols[1].Sequence);
                Assert.IsNull(afterQuery.Protocols[0].Protocol);
                Assert.AreEqual("2.25.40430439493043", afterQuery.Protocols[0].LoadProperty(o => o.Protocol).Oid);
                Assert.AreEqual("2.2.34343433", afterQuery.Protocols[1].LoadProperty(o => o.Protocol).Oid);
            }
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
