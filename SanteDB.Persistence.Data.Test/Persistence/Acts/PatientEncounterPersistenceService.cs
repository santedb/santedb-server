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
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Tests for the patient encounter resource
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class PatientEncounterPersistenceService : DataPersistenceTest
    {

        /// <summary>
        /// Tests that persistence of a simple patient encounter will work properly
        /// </summary>
        [Test]
        public void TestSimplePersistence()
        {

            using(AuthenticationContext.EnterSystemContext())
            {

                var encounter = new PatientEncounter()
                {
                    StartTime = DateTimeOffset.Now.Date,
                    AdmissionSourceTypeKey = NullReasonKeys.Unknown,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = PatientEncounterTypeKeys.Ambulatory,
                    MoodConceptKey = MoodConceptKeys.Eventoccurrence
                };

                var afterInsert = base.TestInsert(encounter);
                Assert.IsNull(afterInsert.AdmissionSourceType);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.AdmissionSourceType));
                Assert.IsNull(afterInsert.TypeConcept);
                Assert.IsNull(afterInsert.StopTime);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.TypeConcept));
                Assert.AreEqual(StatusKeys.Active, afterInsert.StatusConceptKey);
                Assert.AreEqual(PatientEncounterTypeKeys.Ambulatory, afterInsert.TypeConceptKey);

                // Test appropriate filtering 
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Active && o.AdmissionSourceType.Mnemonic == "NullFlavor-Unknown", 1);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Active && o.DischargeDisposition.Mnemonic == "Disposition-Died", 0);
                var afterQuery = base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Active && o.AdmissionSourceType.Mnemonic == "NullFlavor-Unknown", 1).First();
                Assert.IsNull(afterQuery.AdmissionSourceType);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.AdmissionSourceType));
                Assert.IsNull(afterQuery.TypeConcept);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.TypeConcept));
                Assert.AreEqual(StatusKeys.Active, afterQuery.StatusConceptKey);
                Assert.AreEqual(PatientEncounterTypeKeys.Ambulatory, afterQuery.TypeConceptKey);
                Assert.IsNull(afterQuery.StopTime);

                // Test updating (discharge)
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.StopTime = DateTimeOffset.Now.AddDays(1).Date;
                    o.StatusConceptKey = StatusKeys.Completed;
                    o.DischargeDispositionKey = DischargeDispositionKeys.Died;
                    return o;
                });
                Assert.IsNotNull(afterUpdate.DischargeDispositionKey);
                Assert.IsNotNull(afterUpdate.StopTime);
                Assert.AreEqual(StatusKeys.Completed, afterUpdate.StatusConceptKey);

                // Test re-query
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Active && o.AdmissionSourceType.Mnemonic == "NullFlavor-Unknown", 0);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died", 1);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.StopTime > DateTimeOffset.Now && o.DischargeDisposition.Mnemonic == "Disposition-Died", 1);
                afterQuery = base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died", 1).First();
                Assert.IsNull(afterQuery.AdmissionSourceType);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.AdmissionSourceType));
                Assert.IsNull(afterQuery.TypeConcept);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.TypeConcept));
                Assert.IsNotNull(afterQuery.DischargeDispositionKey);
                Assert.IsNull(afterQuery.DischargeDisposition);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.DischargeDisposition));
                Assert.AreEqual(StatusKeys.Completed, afterQuery.StatusConceptKey);
                Assert.AreEqual(PatientEncounterTypeKeys.Ambulatory, afterQuery.TypeConceptKey);
                Assert.IsNotNull(afterQuery.StopTime);
                Assert.IsNull(afterQuery.SpecialArrangements);

                // Test the deletion of the encounter
                base.TestDelete<PatientEncounter>(afterQuery, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died", 0);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died" && o.ObsoletionTime != null, 1);

                // Test the un-delete of the encounter
                base.TestUpdate(afterQuery, o => o);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died" && o.ObsoletionTime != null, 0);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Active && o.AdmissionSourceType.Mnemonic == "NullFlavor-Unknown", 0);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died", 1);
                afterQuery = base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.StopTime > DateTimeOffset.Now && o.DischargeDisposition.Mnemonic == "Disposition-Died", 1).First();
                Assert.IsNull(afterQuery.AdmissionSourceType);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.AdmissionSourceType));
                Assert.IsNull(afterQuery.TypeConcept);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.TypeConcept));
                Assert.IsNotNull(afterQuery.DischargeDispositionKey);
                Assert.IsNull(afterQuery.DischargeDisposition);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.DischargeDisposition));
                Assert.AreEqual(StatusKeys.Completed, afterQuery.StatusConceptKey);
                Assert.AreEqual(PatientEncounterTypeKeys.Ambulatory, afterQuery.TypeConceptKey);
                Assert.IsNotNull(afterQuery.StopTime);
                Assert.IsNull(afterQuery.SpecialArrangements);

                // Test perma-delete
                base.TestDelete(afterQuery, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died", 0);
                base.TestQuery<PatientEncounter>(o => o.StatusConceptKey == StatusKeys.Completed && o.DischargeDisposition.Mnemonic == "Disposition-Died" && o.ObsoletionTime != null, 0);

                try
                {
                    base.TestUpdate(afterQuery, o => o);
                    Assert.Fail("Should have thrown exception");
                }
                catch(DataPersistenceException e) when (e.InnerException is KeyNotFoundException) { }
                catch
                {
                    Assert.Fail("Threw wrong exception type");
                }


            }

        }

        /// <summary>
        /// Test that the extended properties of special arrangements have been persisted and loaded properly
        /// </summary>
        [Test]
        public void TestWithSpecialArrangements()
        {

            using(AuthenticationContext.EnterSystemContext())
            {
                var encounter = new PatientEncounter()
                {
                    StartTime = DateTimeOffset.Now.Date,
                    AdmissionSourceTypeKey = NullReasonKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = PatientEncounterTypeKeys.Inpatient,
                    SpecialArrangements = new List<PatientEncounterArrangement>()
                    {
                        new PatientEncounterArrangement(NullReasonKeys.AskedUnknown)
                    },
                    MoodConceptKey = MoodConceptKeys.Eventoccurrence
                };

                var afterInsert = base.TestInsert(encounter);
                Assert.AreEqual(1, afterInsert.SpecialArrangements.Count);

                // Test querying by special arrangements
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown), 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other), 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other) && o.StatusConceptKey == StatusKeys.Active, 0);
                var afterQuery = base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 1).First();
                Assert.IsNull(afterQuery.SpecialArrangements);

                using(DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    afterQuery = base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 1).First();
                    Assert.IsNotNull(afterQuery.SpecialArrangements);
                    Assert.AreEqual(1, afterQuery.SpecialArrangements.Count);
                    Assert.IsNull(afterQuery.SpecialArrangements.First().ArrangementType);
                }

                using (DataPersistenceControlContext.Create(LoadMode.FullLoad))
                {
                    afterQuery = base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 1).First();
                    Assert.IsNotNull(afterQuery.SpecialArrangements);
                    Assert.AreEqual(1, afterQuery.SpecialArrangements.Count);
                    Assert.IsNotNull(afterQuery.SpecialArrangements.First().ArrangementType);
                }

                // Test the updating of arrangements to add keys
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.SpecialArrangements.Add(new PatientEncounterArrangement(NullReasonKeys.Other));
                    return o;
                });
                Assert.AreEqual(2, afterUpdate.SpecialArrangements.Count);

                // Test query on new
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown), 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other), 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other) && o.StatusConceptKey == StatusKeys.Active, 1);

                // Test update to remove
                afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.SpecialArrangements.RemoveAll(x=>x.ArrangementTypeKey == NullReasonKeys.AskedUnknown);
                    return o;
                });
                Assert.AreEqual(1, afterUpdate.SpecialArrangements.Count);
                // Test query on new
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown), 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other), 1);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other) && o.StatusConceptKey == StatusKeys.Active, 1);

                // Test perma-delete cascades
                base.TestDelete(afterQuery, DeleteMode.PermanentDelete);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown), 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.AskedUnknown) && o.StatusConceptKey == StatusKeys.Active, 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other), 0);
                base.TestQuery<PatientEncounter>(o => o.SpecialArrangements.Any(s => s.ArrangementTypeKey == NullReasonKeys.Other) && o.StatusConceptKey == StatusKeys.Active, 0);

            }
        }

    }
}
