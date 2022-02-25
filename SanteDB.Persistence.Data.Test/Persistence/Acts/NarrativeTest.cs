using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Roles;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Test the narrative persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class NarrativeTest : DataPersistenceTest
    {

        /// <summary>
        /// Test insertion with proper data provider
        /// </summary>
        [Test]
        public void TestL1WithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var narrative = Narrative.DocumentFromStream("A Sample Document", "en", "application/pdf", typeof(NarrativeTest).Assembly.GetManifestResourceStream("SanteDB.Persistence.Data.Test.example.pdf"));
                var afterInsert = base.TestInsert(narrative);
                Assert.Greater(afterInsert.Text.Length, 0);

                // Test query 
                base.TestQuery<Narrative>(o => o.MimeType == "application/pdf", 1);
                base.TestQuery<Narrative>(o => o.MimeType == "text/plain", 0);
                base.TestQuery<Narrative>(o => o.Title == "A Sample Document", 1);
                var afterQuery = base.TestQuery<Narrative>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual("application/pdf", afterQuery.MimeType);
                Assert.AreEqual("A Sample Document", afterQuery.Title);

                Assert.AreEqual(narrative.Text, afterQuery.Text);

                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Title = "An updated title";
                    return o;
                });
                base.TestQuery<Narrative>(o => o.MimeType == "application/pdf", 1);
                base.TestQuery<Narrative>(o => o.MimeType == "text/plain", 0);
                base.TestQuery<Narrative>(o => o.Title == "A Sample Document", 0);
                base.TestQuery<Narrative>(o => o.Title == "An updated title", 1);
                afterQuery = base.TestQuery<Narrative>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual("application/pdf", afterQuery.MimeType);
                Assert.AreEqual("An updated title", afterQuery.Title);
                Assert.AreEqual(narrative.Text, afterQuery.Text);

            }
        }

        /// <summary>
        /// Test insertion of L2 document (with sections) with proper data provider
        /// </summary>
        [Test]
        public void TestL2WithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var narrative = Narrative.DocumentFromString("An L2 Document", "en", "text/plain", "This is a plain text document");
                narrative.Relationships = new List<ActRelationship>() {
                    new ActRelationship(ActRelationshipTypeKeys.HasComponent, Narrative.SectionFromString("Cause of Death", "en", "text/plain", ObservationTypeKeys.CauseOfDeath, "The patient died of stuff"))
                };
                narrative.Participations = new List<ActParticipation>() {
                    new ActParticipation(ActParticipationKeys.RecordTarget, new Patient()
                    {
                        Names = new List<Core.Model.Entities.EntityName>() {
                            new Core.Model.Entities.EntityName(NameUseKeys.Legal, "Smith", "Bobby")
                        }
                    })
                };

                var afterInsert = base.TestInsert(narrative);
                Assert.Greater(afterInsert.Text.Length, 0);

                // Test query 
                base.TestQuery<Narrative>(o => o.ClassConceptKey == ActClassKeys.Document && o.MimeType == "text/plain", 1);
                base.TestQuery<Narrative>(o => o.ClassConceptKey == ActClassKeys.Document && o.Title == "An L2 Document", 1);
                base.TestQuery<Narrative>(o => o.Relationships.Any(r => (r.TargetAct as Narrative).Title == "Cause of Death"), 1); // Casting test
                base.TestQuery<Narrative>(o => o.Participations.Any(r => r.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Smith"))), 1);
                var afterQuery = base.TestQuery<Narrative>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual("text/plain", afterQuery.MimeType);
                Assert.AreEqual(Encoding.UTF8.GetBytes("This is a plain text document"), afterQuery.Text);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual(narrative.Text, afterQuery.Text);


            }
        }
    }
}
