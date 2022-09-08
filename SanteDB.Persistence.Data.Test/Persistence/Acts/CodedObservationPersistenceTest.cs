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
    public class CodedObservationPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Test persistence with proper persistence classes
        /// </summary>
        [Test]
        public void TestPersistWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                // Goal => Cause of Death to be "Awesome"
                var codedObservation = new CodedObservation()
                {
                    ActTime = DateTimeOffset.Now.Date,
                    MoodConceptKey = ActMoodKeys.Goal,
                    TypeConceptKey = ObservationTypeKeys.CauseOfDeath,
                    InterpretationConceptKey = ActInterpretationKeys.AbnormalHigh,
                    Value = new Core.Model.DataTypes.Concept()
                    {
                        ClassKey = ConceptClassKeys.Other,
                        Mnemonic = "CauseOfDeath-Awesome"
                    }
                };

                var afterInsert = base.TestInsert(codedObservation);
                Assert.IsNotNull(afterInsert.ValueKey);
                Assert.AreEqual("CauseOfDeath-Awesome", afterInsert.LoadProperty(o => o.Value).Mnemonic);

                // Test for querying
                base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Awesome", 1);
                base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Other", 0);
                var afterQuery = base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Awesome", 1).First();
                Assert.IsNull(afterQuery.Value);
                Assert.IsNull(afterQuery.InterpretationConcept);

                Assert.AreEqual("CauseOfDeath-Awesome", afterQuery.LoadProperty(o => o.Value).Mnemonic);
                Assert.AreEqual("AbnormalHigh", afterQuery.LoadProperty(o => o.InterpretationConcept).Mnemonic);

                // Test update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.ValueKey = NullReasonKeys.Unknown;
                    return o;
                });
                Assert.AreEqual(NullReasonKeys.Unknown, afterUpdate.ValueKey);

                base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Awesome", 0);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown", 1);

                // Delete
                base.TestDelete(afterInsert, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Awesome", 0);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown", 0);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown" && o.ObsoletionTime != null, 1);

                // Un-delete
                base.TestUpdate(afterQuery, o =>
                {
                    return o;
                });
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown", 1);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown" && o.ObsoletionTime != null, 0);

                // Test perma delete
                base.TestDelete(afterInsert, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<CodedObservation>(o => o.Value.Mnemonic == "CauseOfDeath-Awesome", 0);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown", 0);
                base.TestQuery<CodedObservation>(o => o.TypeConceptKey == ObservationTypeKeys.CauseOfDeath && o.Value.Mnemonic == "NullFlavor-Unknown" && o.ObsoletionTime != null, 0);

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

        /// <summary>
        /// Tests that persistence with the generic persisters work properly
        /// </summary>
        [Test]
        public void TestPersistWithImproper()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var codedObservation = new CodedObservation()
                {
                    ActTime = DateTimeOffset.Now.Date,
                    MoodConceptKey = ActMoodKeys.Promise,
                    InterpretationConceptKey = ActInterpretationKeys.Normal,
                    TypeConceptKey = ObservationTypeKeys.ClinicalState,
                    ValueKey = NullReasonKeys.Unknown
                };

                var afterInsert = base.TestInsert<Observation>(codedObservation);
                Assert.IsInstanceOf<CodedObservation>(afterInsert);
                Assert.AreEqual(NullReasonKeys.Unknown, (afterInsert as CodedObservation).ValueKey);

                var afterQuery = base.TestQuery<Act>(o => o.MoodConceptKey == ActMoodKeys.Promise, 1).FirstOrDefault();
                Assert.IsInstanceOf<CodedObservation>(afterQuery);
                Assert.AreEqual(NullReasonKeys.Unknown, (afterQuery as CodedObservation).ValueKey);
                afterQuery = base.TestQuery<Act>(o => o.MoodConceptKey == ActMoodKeys.Promise, 1).FirstOrDefault();
                Assert.IsInstanceOf<CodedObservation>(afterQuery);

            }
        }

    }
}
