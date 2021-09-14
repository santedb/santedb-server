/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Hax;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// The AdoArchiveService is an archival service which stores data in a secondary database 
    /// </summary>
    [ServiceProvider("ADO.NET Archiving and Data Shipping")]
    public class AdoArchiveService : IAdoPersistenceSettingsProvider, IDataArchiveService
    {


        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoArchiveService));

        // Archive services
        private ConcurrentDictionary<Type, IAdoPersistenceService> m_archiveServices = new ConcurrentDictionary<Type, IAdoPersistenceService>();

        // Configuration for the archive DB
        private AdoArchiveConfigurationSection m_configuration;

        // The mapper for this service
        private ModelMapper m_mapper;

        // Get the query builder
        private QueryBuilder m_queryBuilder;

        /// <summary>
        /// Data retention archiving service name
        /// </summary>
        public string ServiceName => "ADO.NET Data Retention/Archiving Service";

        /// <summary>
        /// Get the configuration for the archive store
        /// </summary>
        /// <returns></returns>
        public AdoPersistenceConfigurationSection GetConfiguration()
        {
            if (m_configuration == null)
                m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoArchiveConfigurationSection>();
            return m_configuration;
        }

        /// <summary>
        /// Get the mapper for this archive store
        /// </summary>
        /// <returns></returns>
        public ModelMapper GetMapper() => this.m_mapper;

        /// <summary>
        /// Get the archive persister for the specified domain type
        /// </summary>
        /// <param name="tDomain">The domain type</param>
        /// <returns>The persistence provider for the domain type</returns>
        public IAdoPersistenceService GetPersister(Type tDomain)
        {
            if (!this.m_archiveServices.TryGetValue(tDomain, out IAdoPersistenceService retVal))
            {
                // Get the master persistence service from the application context and create a new copy with this as the settings provider
                var sDomain = tDomain;
                var idpType = typeof(IDataPersistenceService<>).MakeGenericType(sDomain);
                retVal = ApplicationServiceContext.Current.GetService(idpType) as IAdoPersistenceService;
                while (retVal == null && sDomain != typeof(object))
                {
                    sDomain = sDomain.BaseType;
                    idpType = typeof(IDataPersistenceService<>).MakeGenericType(sDomain);
                    retVal = ApplicationServiceContext.Current.GetService(idpType) as IAdoPersistenceService;
                }

                if (retVal != null)
                {
                    // Construct a copy with this as the settings provider
                    retVal = Activator.CreateInstance(retVal.GetType(), this) as IAdoPersistenceService;
                    this.m_archiveServices.TryAdd(tDomain, retVal);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Gets the query builder for this object
        /// </summary>
        /// <returns></returns>
        public QueryBuilder GetQueryBuilder() => this.m_queryBuilder;

        /// <summary>
        /// Archive the specified objects from the primary datastore
        /// </summary>
        /// <param name="modelType">The type of data to be archived</param>
        /// <param name="keysToBeArchived">The keys of objects to be archived</param>
        public void Archive(Type modelType, params Guid[] keysToBeArchived)
        {
            using (var targetContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    targetContext.Open();
                    using (var transaction = targetContext.BeginTransaction())
                    {
                        // Get the data persister
                        var idpType = typeof(IDataPersistenceService<>).MakeGenericType(modelType);
                        var persistenceService = ApplicationServiceContext.Current.GetService(idpType) as IAdoCopyProvider;
                        if (persistenceService == null)
                            throw new InvalidOperationException("Cannot archive on this type, ensure the persistence service implements the copy provider interface");
                        persistenceService.CopyTo(keysToBeArchived, targetContext);
                        // Now we want to get the data source persistence service
                        transaction.Commit();
                    }
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error copying objects to archive - {0}", e.Message);
                    throw new DataPersistenceException($"Error copying objects to archive", e);
                }
        }

        /// <summary>
        /// Retrieve the specified object from the archive
        /// </summary>
        /// <param name="modelType">The type of data to retrieve</param>
        /// <param name="keyToRetrieve">The key of the object to retrieve</param>
        /// <returns>The retrieved object</returns>
        public IdentifiedData Retrieve(Type modelType, Guid keyToRetrieve)
        {
            var persister = this.GetPersister(modelType);
            return persister.Get(keyToRetrieve) as IdentifiedData;
        }

        /// <summary>
        /// Determine if the object exists in the archive
        /// </summary>
        /// <param name="modelType">The type of object to retrieve</param>
        /// <param name="keyToCheck">The key to check</param>
        /// <returns>True if the object exists in the archive</returns>
        public bool Exists(Type modelType, Guid keyToCheck)
        {
            var persister = this.GetPersister(modelType);
            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    return persister.Exists(context, keyToCheck);
                }
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error determine existance of {0} - {1}", keyToCheck, e.Message);
                throw new DataPersistenceException($"Cannot determine existance of {keyToCheck} in archive", e);
            }
        }

        /// <summary>
        /// Purge the specified objects from the archive
        /// </summary>
        /// <param name="modelType">The type of data to be purged</param>
        /// <param name="keysToBePurged">The keys of the records to purge</param>
        public void Purge(Type modelType, params Guid[] keysToBePurged)
        {
            var persister = this.GetPersister(modelType) as IBulkDataPersistenceService;
            persister.Purge(TransactionMode.Commit, AuthenticationContext.Current.Principal, keysToBePurged);
        }

        /// <summary>
        /// Construct a new service
        /// </summary>
        public AdoArchiveService()
        {
            try
            {
                this.m_mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName));

                List<IQueryBuilderHack> hax = new List<IQueryBuilderHack>() { new SecurityUserEntityQueryHack(), new RelationshipGuardQueryHack(), new CreationTimeQueryHack(this.m_mapper), new EntityAddressNameQueryHack() };
                if (this.GetConfiguration().DataCorrectionKeys.Any(k => k == "ConceptQueryHack"))
                    hax.Add(new ConceptQueryHack(this.m_mapper));

                this.m_queryBuilder = new QueryBuilder(this.m_mapper, GetConfiguration().Provider,
                    hax.Where(o => o != null).ToArray()
                );

            }
            catch (ModelMapValidationException ex)
            {
                this.m_tracer.TraceError("Error validating model map: {0}", ex);
                throw ex;
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error validating model map: {0}", ex);
                throw ex;
            }
        }
    }
}
