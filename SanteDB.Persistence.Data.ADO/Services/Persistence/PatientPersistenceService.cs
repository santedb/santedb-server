/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Roles;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Roles;
using System;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Persistence service which is used to persist patients
    /// </summary>
    public class PatientPersistenceService : EntityDerivedPersistenceService<Patient, DbPatient, CompositeResult<DbPatient, DbPerson, DbEntityVersion, DbEntity>>
    {

        public PatientPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
            this.m_personPersister = new PersonPersistenceService(settingsProvider);
        }

        private PersonPersistenceService m_personPersister;

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Patient modelInstance, DataContext context)
        {
            var dbPatient = base.FromModelInstance(modelInstance, context) as DbPatient;

            if (modelInstance.DeceasedDatePrecision.HasValue)
                dbPatient.DeceasedDatePrecision = PersonPersistenceService.PrecisionMap[modelInstance.DeceasedDatePrecision.Value];
            return dbPatient;
        }

        /// <summary>
        /// Model instance
        /// </summary>
        public Core.Model.Roles.Patient ToModelInstance(DbPatient patientInstance, DbPerson personInstance, DbEntityVersion entityVersionInstance, DbEntity entityInstance, DataContext context)
        {
            var retVal = this.m_entityPersister.ToModelInstance<Core.Model.Roles.Patient>(entityVersionInstance, entityInstance, context);
            if (retVal == null) return null;

            retVal.DeceasedDate = patientInstance.DeceasedDate;
            // Reverse lookup
            if (!String.IsNullOrEmpty(patientInstance.DeceasedDatePrecision))
                retVal.DeceasedDatePrecision = PersonPersistenceService.PrecisionMap.Where(o => o.Value == patientInstance.DeceasedDatePrecision).Select(o => o.Key).First();

            retVal.MultipleBirthOrder = (int?)patientInstance.MultipleBirthOrder;
            retVal.GenderConceptKey = patientInstance.GenderConceptKey;
            retVal.OccupationKey = personInstance.OccupationKey;
            retVal.LivingArrangementKey = patientInstance.LivingArrangementKey;
            retVal.VipStatusKey = patientInstance.VipStatusKey;
            retVal.EducationLevelKey = patientInstance.EducationLevelKey;
            retVal.EthnicGroupCodeKey = patientInstance.EthnicGroupCodeKey;
            retVal.MaritalStatusKey = patientInstance.MaritalStatusKey;
            
            // Copy from person 
            retVal.DateOfBirth = personInstance?.DateOfBirth;

            // Reverse lookup
            if (!String.IsNullOrEmpty(personInstance?.DateOfBirthPrecision))
                retVal.DateOfBirthPrecision = PersonPersistenceService.PrecisionMap.Where(o => o.Value == personInstance.DateOfBirthPrecision).Select(o => o.Key).First();

            return retVal;
        }

        /// <summary>
        /// Insert the specified person into the database
        /// </summary>
        public override Core.Model.Roles.Patient InsertInternal(DataContext context, Core.Model.Roles.Patient data)
        {
            if (data.GenderConcept != null) data.GenderConcept = data.GenderConcept?.EnsureExists(context) as Concept;
            data.GenderConceptKey = data.GenderConcept?.Key ?? data.GenderConceptKey;
            if (data.VipStatus != null) data.VipStatus = data.VipStatus?.EnsureExists(context, false) as Concept;
            data.VipStatusKey = data.VipStatus?.Key ?? data.VipStatusKey;
            this.m_personPersister.InsertInternal(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified person
        /// </summary>
        public override Core.Model.Roles.Patient UpdateInternal(DataContext context, Core.Model.Roles.Patient data)
        {
            // Ensure exists
            if (data.GenderConcept != null) data.GenderConcept = data.GenderConcept?.EnsureExists(context) as Concept;
            data.GenderConceptKey = data.GenderConcept?.Key ?? data.GenderConceptKey;
            if (data.VipStatus != null) data.VipStatus = data.VipStatus?.EnsureExists(context, false) as Concept;
            data.VipStatusKey = data.VipStatus?.Key ?? data.VipStatusKey;
            this.m_personPersister.UpdateInternal(context, data);
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override Core.Model.Roles.Patient ObsoleteInternal(DataContext context, Core.Model.Roles.Patient data)
        {
            this.m_personPersister.ObsoleteInternal(context, data);
            return data;
        }

    }
}
