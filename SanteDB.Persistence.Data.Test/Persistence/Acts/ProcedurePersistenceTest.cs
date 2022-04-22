﻿using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SanteDB.Core.Model;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Tests for care plan persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ProcedurePersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests that loading, querying and inserting with proper persistence services works as expected
        /// </summary>
        [Test]
        public void TestWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var procedure = new Procedure()
                {
                    ActTime = DateTimeOffset.Now,
                    MoodConceptKey = ActMoodKeys.Intent,
                    ApproachSite = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "BodySite-Mouth",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                    },
                    Method = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "ProcedureMethod-Endoscopy",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.ProcedureTechnique }
                    },
                    Protocols = new List<ActProtocol>()
                    {
                        new ActProtocol()
                        {
                            Protocol = new Protocol()
                            {
                                Name = "GI Investigative",
                                Oid = "1.123.123432432"
                            }
                        }
                    },
                    TargetSite = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "BodySite-Stomach",
                        ConceptSetsXml = new List<Guid>() {  ConceptSetKeys.BodySiteOrSystem }
                    }
                };

                var afterInsert = base.TestInsert(procedure);
                Assert.AreEqual(procedure.ApproachSite.Mnemonic, afterInsert.LoadProperty(o=>o.ApproachSite).Mnemonic);
                Assert.AreEqual(procedure.TargetSite.Mnemonic, afterInsert.LoadProperty(o=>o.TargetSite).Mnemonic);
                Assert.AreEqual(procedure.Method.Mnemonic, afterInsert.LoadProperty(o=>o.Method).Mnemonic);

                base.TestQuery<Procedure>(o => o.ApproachSite.Mnemonic == "BodySite-Mouth" && o.TargetSite.Mnemonic == "BodySite-Stomach", 1);
                base.TestQuery<Procedure>(o => o.ApproachSite.Mnemonic == "BodySite-Mouths" && o.TargetSite.Mnemonic == "BodySite-Stomach", 0);
                var afterQuery = base.TestQuery<Procedure>(o => o.Method.Mnemonic == "ProcedureMethod-Endoscopy", 1).FirstOrDefault();
                Assert.IsNull(afterQuery.Method);
                Assert.IsNull(afterQuery.TargetSite);
                Assert.IsNull(afterQuery.ApproachSite);
                Assert.AreEqual("BodySite-Mouth", afterQuery.LoadProperty(o => o.ApproachSite).Mnemonic);
                Assert.AreEqual("BodySite-Stomach", afterQuery.LoadProperty(o => o.TargetSite).Mnemonic);

                // Test updating
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.TargetSiteKey = null;
                    o.TargetSite = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "BodySite-UpperIntestine",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                    };
                    return o;
                });
                Assert.AreEqual("BodySite-UpperIntestine", afterUpdate.LoadProperty(o=>o.TargetSite).Mnemonic);

                // Test re-query 
                afterQuery = base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-Stomach", 0).FirstOrDefault();
                afterQuery = base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 1).FirstOrDefault();
                Assert.IsNotNull(afterQuery.PreviousVersionKey);
                Assert.IsNotNull(afterQuery.GetPreviousVersion());
                Assert.AreEqual("BodySite-Stomach", (afterQuery.GetPreviousVersion() as Procedure).LoadProperty(o => o.TargetSite).Mnemonic);

                // Test delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 0);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime.HasValue, 1);

                // Un-Delete
                afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Notes.Add(new Core.Model.DataTypes.ActNote(Guid.Parse(AuthenticationContext.SystemUserSid), "THIS IS A TEST!!!! IT SHOULD UNDELETE THE RECORD"));
                    return o;
                });

                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 1);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime.HasValue, 0);

                // Test perma delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 0);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime.HasValue, 0);


            }
        }

        /// <summary>
        /// Tests that using the Act class to persistence, read and query the procedures works properly
        /// </summary>
        public void TestWithImproper()
        {

        }
    }
}
