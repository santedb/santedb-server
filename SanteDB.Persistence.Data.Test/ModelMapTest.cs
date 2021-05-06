using NUnit.Framework;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Roles;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Roles;
using SanteDB.Persistence.Data.Services;
using System;

namespace SanteDB.Persistence.Data.Test
{
    [TestFixture]
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
        }
    }
}
