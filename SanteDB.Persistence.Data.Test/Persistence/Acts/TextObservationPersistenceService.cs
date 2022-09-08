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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
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
    /// Tests for care plan persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class TextObservationPersistenceService : DataPersistenceTest
    {

        private IdentityDomain CreateAA()
        {
            var aa = base.TestQuery<IdentityDomain>(o => o.DomainName == "TEXT-OBSERVATION", null).FirstOrDefault();
            if (aa == null)
            {
                aa = new IdentityDomain("TEXT-OBSERVATION", "TEXT OBSERVATIONS", "2.25.30302932932032");
            }
            return aa;
        }
        /// <summary>
        /// Tests that persistence of observations with the proper type of persister works
        /// </summary>
        [Test]
        public void TestPersistWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var to = new TextObservation()
                {
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ActTime = DateTimeOffset.Now.Date,
                    GeoTag = new Core.Model.DataTypes.GeoTag(10, 10, true),
                    Identifiers = new List<Core.Model.DataTypes.ActIdentifier>()
                    {
                        new Core.Model.DataTypes.ActIdentifier(this.CreateAA(), "TO-0001")
                    },
                    InterpretationConceptKey = ActInterpretationKeys.Normal,
                    Value = "THIS IS AN OBSERVATION WHICH IS STORED IN THE DATABASE!"
                };

                var afterInsert = base.TestInsert(to);
                Assert.IsNotNull(afterInsert.Value);
                Assert.AreEqual("THIS IS AN OBSERVATION WHICH IS STORED IN THE DATABASE!", afterInsert.Value);
                Assert.AreEqual(ActInterpretationKeys.Normal, afterInsert.InterpretationConceptKey);

                // Query for observation
                base.TestQuery<TextObservation>(o => o.Value == "THIS IS AN OBSERVATION WHICH IS STORED IN THE DATABASE!", 1);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.Normal && o.Value.Contains("THIS IS%"), 1);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalHigh && o.Value.Contains("THIS IS%"), 0);
                var afterQuery = base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.Normal && o.Value.Contains("THIS IS%"), 1).FirstOrDefault();
                Assert.IsNull(afterQuery.InterpretationConcept);
                Assert.AreEqual("Normal", afterQuery.LoadProperty(o => o.InterpretationConcept).Mnemonic);
                Assert.AreEqual("THIS IS AN OBSERVATION WHICH IS STORED IN THE DATABASE!", afterQuery.Value);

                // Attempt update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.InterpretationConceptKey = ActInterpretationKeys.AbnormalLow;
                    return o;
                });
                Assert.AreNotEqual(ActInterpretationKeys.Normal, afterUpdate.InterpretationConceptKey);
                Assert.AreEqual(ActInterpretationKeys.AbnormalLow, afterUpdate.InterpretationConceptKey);

                // Re-query test
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.Normal && o.Value.Contains("THIS IS%"), 0);
                afterQuery = base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalLow && o.Value.Contains("THIS IS%"), 1).FirstOrDefault();
                Assert.AreEqual("AbnormalLow", afterQuery.LoadProperty(o => o.InterpretationConcept).Mnemonic);

                // Delete
                base.TestDelete(afterUpdate, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalLow && o.Value.Contains("THIS IS%"), 0);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalLow && o.Value.Contains("THIS IS %") && o.ObsoletionTime != null, 1);

                // Un-Delete by update
                base.TestUpdate(afterQuery, o =>
                {
                    o.InterpretationConceptKey = ActInterpretationKeys.Normal;
                    return o;
                });

                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.Normal && o.Value.Contains("THIS IS%"), 1);

                // Perma Delete
                base.TestDelete(afterUpdate, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalLow && o.Value.Contains("THIS IS%"), 0);
                base.TestQuery<TextObservation>(o => o.InterpretationConceptKey == ActInterpretationKeys.AbnormalLow && o.Value.Contains("THIS IS %") && o.ObsoletionTime == null, 0);

                // Un-Delete fails
                try
                {
                    base.TestUpdate(afterQuery, o =>
                    {
                        o.InterpretationConceptKey = ActInterpretationKeys.Normal;
                        return o;
                    });
                    Assert.Fail("Should have thrown a key not found exception!");
                }
                catch (KeyNotFoundException) { }
                catch (DataPersistenceException e) when (e.InnerException is KeyNotFoundException) { }
                catch (Exception)
                {
                    Assert.Fail("Should have thrown KeyNotFoundException!");
                }
            }
        }

    }
}
