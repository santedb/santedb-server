using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Test
{
    /// <summary>
    /// Implements a matcher service which only matches date of birth
    /// </summary>
    public class DummyMatcherService : IRecordMatchingService
    {
        /// <summary>
        /// Perform blocking
        /// </summary>
        public IEnumerable<T> Block<T>(T input, string configurationName) where T : IdentifiedData
        {
            if (typeof(T) == typeof(Patient)) {
                Patient p = (Patient)((Object)input);
                return ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.DateOfBirth == p.DateOfBirth, AuthenticationContext.SystemPrincipal).OfType<T>();
            }
            return new List<T>();
        }

        /// <summary>
        /// Classify the patient records
        /// </summary>
        public IEnumerable<IRecordMatchResult<T>> Classify<T>(T input, IEnumerable<T> blocks, string configurationName) where T : IdentifiedData
        {
            return blocks.Select(o => new DummyMatchResult<T>(input, o));
        }

        /// <summary>
        /// Match existing records with others
        /// </summary>
        public IEnumerable<IRecordMatchResult<T>> Match<T>(T input, string configurationName) where T : IdentifiedData
        {
            Assert.AreEqual("default", configurationName);
            return this.Classify(input, this.Block(input, configurationName), configurationName);
        }
    }

    /// <summary>
    /// Represent a dummy match result
    /// </summary>
    public class DummyMatchResult<T> : IRecordMatchResult<T>
    {
        // The record
        private T m_record;

        /// <summary>
        /// Get the score
        /// </summary>
        public double Score => 1.0;

        /// <summary>
        /// Gets the matching record
        /// </summary>
        public T Record => this.m_record;

        /// <summary>
        /// Match classification
        /// </summary>
        public RecordMatchClassification Classification { get; private set; }

        /// <summary>
        /// Create a dummy match
        /// </summary>
        public DummyMatchResult(T input, T record)
        {
            this.m_record = record;

            // Patient?
            if (input is Patient)
            {
                var pInput = (Patient)(object)input;
                var pRecord = (Patient)(object)record;
                // Classify
                if (pInput.MultipleBirthOrder.HasValue && pInput.MultipleBirthOrder != pRecord.MultipleBirthOrder)
                    this.Classification = RecordMatchClassification.Probable;
                else
                    this.Classification = RecordMatchClassification.Match;
            }
            else
                this.Classification = RecordMatchClassification.Match;
        }
    }
}
