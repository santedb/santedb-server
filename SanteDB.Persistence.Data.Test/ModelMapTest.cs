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
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Roles;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Roles;
using SanteDB.Persistence.Data.Services;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Model mapping test
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ModelMapTest
    {
        /// <summary>
        /// Test process model map
        /// </summary>
        [Test]
        public void TestProcessModelMap()
        {
            var mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream("SanteDB.Persistence.Data.Map.ModelMap.xml"), "AdoModelMap");

            var patient = new Patient()
            {
                Key = Guid.NewGuid(),
                VersionKey = Guid.NewGuid(),
                GenderConceptKey = Guid.NewGuid(),
                CreatedByKey = Guid.NewGuid(),
                DateOfBirth = DateTime.Now,
                DateOfBirthPrecision = Core.Model.DataTypes.DatePrecision.Day,
                EducationLevelKey = Guid.NewGuid(),
                DeceasedDate = DateTime.Now
            };

            var dbPatient = mapper.MapModelInstance<Patient, DbPatient>(patient);
            Assert.AreEqual(patient.DeceasedDate, dbPatient.DeceasedDate);

            var dbPerson = mapper.MapModelInstance<Person, DbPerson>(patient);
            Assert.AreEqual(patient.GenderConceptKey, dbPerson.GenderConceptKey);
            Assert.AreEqual(patient.DateOfBirth, dbPerson.DateOfBirth);

            var dbEntity = mapper.MapModelInstance<Entity, DbEntityVersion>(patient);
            Assert.AreEqual(patient.Key, dbEntity.Key);
            Assert.AreEqual(patient.VersionKey, dbEntity.VersionKey);

            // Reverse map
            var revPatient = mapper.MapDomainInstance<DbPatient, Patient>(dbPatient);
            Assert.AreEqual(dbPatient.DeceasedDate, revPatient.DeceasedDate);

            var revPerson = mapper.MapDomainInstance<DbPerson, Person>(dbPerson);
            Assert.AreEqual(dbPerson.DateOfBirth, revPerson.DateOfBirth);
            Assert.AreEqual(patient.DateOfBirthPrecision, revPerson.DateOfBirthPrecision);

            var revEntity = mapper.MapDomainInstance<DbEntityVersion, Entity>(dbEntity);
            Assert.AreEqual(patient.Key, revEntity.Key);
            Assert.AreEqual(patient.VersionKey, revEntity.VersionKey);
        }

        /// <summary>
        /// Test process model map
        /// </summary>
        [Test]
        public void TestProcessModelMapReflector()
        {
            var mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream("SanteDB.Persistence.Data.Map.ModelMap.xml"), "AdoModelMapRef", true);

            var patient = new Patient()
            {
                Key = Guid.NewGuid(),
                VersionKey = Guid.NewGuid(),
                GenderConceptKey = Guid.NewGuid(),
                CreatedByKey = Guid.NewGuid(),
                DateOfBirth = DateTime.Now,
                DateOfBirthPrecision = Core.Model.DataTypes.DatePrecision.Day,
                EducationLevelKey = Guid.NewGuid(),
                DeceasedDate = DateTime.Now
            };

            var dbPatient = mapper.MapModelInstance<Patient, DbPatient>(patient);
            Assert.AreEqual(patient.DeceasedDate, dbPatient.DeceasedDate);

            var dbPerson = mapper.MapModelInstance<Person, DbPerson>(patient);
            Assert.AreEqual(patient.GenderConceptKey, dbPerson.GenderConceptKey);
            Assert.AreEqual(patient.DateOfBirth, dbPerson.DateOfBirth);

            var dbEntity = mapper.MapModelInstance<Entity, DbEntityVersion>(patient);
            Assert.AreEqual(patient.Key, dbEntity.Key);
            Assert.AreEqual(patient.VersionKey, dbEntity.VersionKey);

            // Reverse map
            var revPatient = mapper.MapDomainInstance<DbPatient, Patient>(dbPatient);
            Assert.AreEqual(dbPatient.DeceasedDate, revPatient.DeceasedDate);

            var revPerson = mapper.MapDomainInstance<DbPerson, Person>(dbPerson);
            Assert.AreEqual(dbPerson.DateOfBirth, revPerson.DateOfBirth);
            Assert.AreEqual(patient.DateOfBirthPrecision, revPerson.DateOfBirthPrecision);

            var revEntity = mapper.MapDomainInstance<DbEntityVersion, Entity>(dbEntity);
            Assert.AreEqual(patient.Key, revEntity.Key);
            Assert.AreEqual(patient.VersionKey, revEntity.VersionKey);
        }
    }
}