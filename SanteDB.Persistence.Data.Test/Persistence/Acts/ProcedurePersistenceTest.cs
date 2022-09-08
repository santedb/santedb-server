/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
using NUnit.Framework;
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
using SanteDB.Core.Model.Entities;

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
                    ActTime = DateTimeOffset.Now.Date,
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
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                    }
                };

                var afterInsert = base.TestInsert(procedure);
                Assert.AreEqual(procedure.ApproachSite.Mnemonic, afterInsert.LoadProperty(o => o.ApproachSite).Mnemonic);
                Assert.AreEqual(procedure.TargetSite.Mnemonic, afterInsert.LoadProperty(o => o.TargetSite).Mnemonic);
                Assert.AreEqual(procedure.Method.Mnemonic, afterInsert.LoadProperty(o => o.Method).Mnemonic);

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
                Assert.AreEqual("BodySite-UpperIntestine", afterUpdate.LoadProperty(o => o.TargetSite).Mnemonic);

                // Test re-query 
                afterQuery = base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-Stomach", 0).FirstOrDefault();
                afterQuery = base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 1).FirstOrDefault();
                Assert.IsNotNull(afterQuery.PreviousVersionKey);
                Assert.IsNotNull(afterQuery.GetPreviousVersion());
                Assert.AreEqual("BodySite-Stomach", (afterQuery.GetPreviousVersion() as Procedure).LoadProperty(o => o.TargetSite).Mnemonic);

                // Test delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 0);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime != null, 1);

                // Un-Delete
                afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    
                    return o;
                });

                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 1);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime != null, 0);

                // Test perma delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine", 0);
                base.TestQuery<Procedure>(o => o.TargetSite.Mnemonic == "BodySite-UpperIntestine" && o.ObsoletionTime.HasValue, 0);


            }
        }

        /// <summary>
        /// Tests that using the Act class to persistence, read and query the procedures works properly
        /// </summary>
        [Test]
        public void TestWithImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var procedure = new Procedure()
                {
                    ActTime = DateTimeOffset.Now.Date,
                    MoodConceptKey = ActMoodKeys.Intent,
                    ApproachSite = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "BodySite-LeftLeg",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                    },
                    Method = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "ProcedureMethod-StentInsertion",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.ProcedureTechnique }
                    },
                    TargetSite = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "BodySite-LeftHeart",
                        ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                    }
                };

                var afterInsert = base.TestInsert<Act>(procedure);
                Assert.IsInstanceOf<Procedure>(afterInsert);

                var afterQuery = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).FirstOrDefault();
                Assert.IsInstanceOf<Procedure>(afterQuery);
                Assert.IsNull((afterQuery as Procedure).Method);
                Assert.AreEqual("BodySite-LeftLeg", (afterQuery as Procedure).LoadProperty(o => o.ApproachSite).Mnemonic);
                Assert.AreEqual("BodySite-LeftHeart", (afterQuery as Procedure).LoadProperty(o => o.TargetSite).Mnemonic);

                // Test updating
                var afterUpdate = base.TestUpdate<Act>(afterQuery, o =>
                {
                    if (o is Procedure p)
                    {
                        p.TargetSiteKey = null;
                        p.TargetSite = new Core.Model.DataTypes.Concept()
                        {
                            ClassKey = ConceptClassKeys.Other,
                            Mnemonic = "BodySite-Heart",
                            ConceptSetsXml = new List<Guid>() { ConceptSetKeys.BodySiteOrSystem }
                        };
                        return p;
                    }
                    Assert.Fail();
                    return o;
                });
                Assert.AreEqual("BodySite-Heart", (afterUpdate as Procedure).LoadProperty(o => o.TargetSite).Mnemonic);

            }
        }
    }
}
