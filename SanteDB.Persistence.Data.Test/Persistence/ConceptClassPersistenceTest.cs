using NUnit.Framework;
using SanteDB.Core;
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
    /// Tests for the perssitence of concept classes
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO ConceptClass")]
    [ExcludeFromCodeCoverage]
    public class ConceptClassPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Tests that class can be inserted
        /// </summary>
        [Test]
        public void TestInsertConceptClass()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var conceptClass = new ConceptClass()
                {
                    Key = Guid.NewGuid(),
                    Name = "A test concept class",
                    Mnemonic = "TEST_01"
                };

                var after = base.TestInsert(conceptClass);
            }
        }

        /// <summary>
        /// Tests that an update to a concept class works
        /// </summary>
        [Test]
        public void TestUpdateConceptClass()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var conceptClass = new ConceptClass()
                {
                    Key = Guid.NewGuid(),
                    Name = "Another Test",
                    Mnemonic = "TEST_02"
                };

                var afterInsert = base.TestInsert(conceptClass);
                var afterUpdate = base.TestUpdate(afterInsert, (o) => { o.Name = "Updated"; return o; });
            }
        }

        /// <summary>
        /// Tests that the persistence layer obsoletes the data correctly
        /// </summary>
        [Test]
        public void TestObsoleteConceptClass()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var conceptClass = new ConceptClass()
                {
                    Key = Guid.NewGuid(),
                    Name = "Yet Another Test",
                    Mnemonic = "TEST_03"
                };

                var afterInsert = base.TestInsert(conceptClass);
                var afterObsolete = base.TestDelete(afterInsert, Core.Services.DeleteMode.LogicalDelete);
            }
        }

        /// <summary>
        /// Tests that the concept persistence service can query for data
        /// </summary>
        [Test]
        public void TestQueryConceptClass()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                for (var i = 0; i < 10; i++)
                {
                    var conceptClass = new ConceptClass()
                    {
                        Key = Guid.NewGuid(),
                        Name = $"Repeat Class {i}",
                        Mnemonic = $"TEST_04_{i}"
                    };
                    base.TestInsert(conceptClass);
                }

                var afterQuery = base.TestQuery<ConceptClass>(o => o.Mnemonic.StartsWith("TEST_04_"), 10).AsResultSet();
                // In Debugging mode verify that the count() operation is executed on demand
                var first = afterQuery.First();
                Assert.AreEqual("TEST_04_0", first.Mnemonic);

                var last = afterQuery.OrderByDescending(o => o.Mnemonic).First();
                Assert.AreEqual("TEST_04_9", last.Mnemonic);

                // Verify the persistence query stuff
                var queryService = ApplicationServiceContext.Current.GetService<TestQueryPersistenceService>();
                var qid = Guid.NewGuid();
                queryService.SetExpectedQueryStats(qid, 10);
                var asState = afterQuery.AsResultSet().AsStateful(qid);

                // Verify that the stateful query doesn't actually call query
                Assert.AreEqual(5, asState.Skip(1).Take(5).Count());
            }
        }
    }
}