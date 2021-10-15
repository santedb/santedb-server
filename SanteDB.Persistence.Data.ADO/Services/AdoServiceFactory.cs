using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.OrmLite;
using System.Collections;
using System.Reflection;
using SanteDB.Core.Model.Attributes;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Services.Persistence;
using SanteDB.Core.Interfaces;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Service factory for ADO services
    /// </summary>
    public class AdoServiceFactory : IServiceFactory
    {
        // Tracer
        private Tracer m_tracer = new Tracer(AdoDataConstants.TraceSourceName);

        // Cache
        private Dictionary<Type, IAdoPersistenceService> m_persistenceCache = new Dictionary<Type, IAdoPersistenceService>();

        private IServiceProvider m_serviceManager;

        /// <summary>
        /// ADO Persistence Service
        /// </summary>
        public AdoServiceFactory(IServiceProvider serviceManager)
        {
            this.m_serviceManager = serviceManager;
        }

        /// <summary>
        /// Generic versioned persister service for any non-customized persister
        /// </summary>
        internal class GenericBasePersistenceService<TModel, TDomain> : BaseDataPersistenceService<TModel, TDomain>
            where TDomain : class, IDbBaseData, new()
            where TModel : BaseEntityData, new()
        {
            public GenericBasePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Ensure exists
            /// </summary>
            public override TModel InsertInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));
                }
                return base.InsertInternal(context, data);
            }

            /// <summary>
            /// Update the specified object
            /// </summary>
            public override TModel UpdateInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));
                }
                return base.UpdateInternal(context, data);
            }
        }

        /// <summary>
        /// Generic versioned persister service for any non-customized persister
        /// </summary>
        internal class GenericIdentityPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain>
            where TModel : IdentifiedData, new()
            where TDomain : class, IDbIdentified, new()
        {
            public GenericIdentityPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Ensure exists
            /// </summary>
            public override TModel InsertInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));
                }
                return base.InsertInternal(context, data);
            }

            /// <summary>
            /// Update the specified object
            /// </summary>
            public override TModel UpdateInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));
                }
                return base.UpdateInternal(context, data);
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericBaseAssociationPersistenceService<TModel, TDomain> :
            GenericBasePersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : BaseEntityData, ISimpleAssociation, new()
            where TDomain : class, IDbBaseData, new()
        {
            public GenericBaseAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericBaseVersionedAssociationPersistenceService<TModel, TDomain> :
            GenericBasePersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : BaseEntityData, IVersionedAssociation, new()
            where TDomain : class, IDbBaseData, new()
        {
            public GenericBaseVersionedAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                // TODO: Check that this query is actually building what it is supposed to.
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId, versionSequenceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericIdentityAssociationPersistenceService<TModel, TDomain> :
            GenericIdentityPersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : IdentifiedData, ISimpleAssociation, new()
            where TDomain : class, IDbIdentified, new()
        {
            public GenericIdentityAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericIdentityVersionedAssociationPersistenceService<TModel, TDomain> :
            GenericIdentityPersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : IdentifiedData, IVersionedAssociation, new()
            where TDomain : class, IDbIdentified, new()
        {
            public GenericIdentityVersionedAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                // TODO: Check that this query is actually building what it is supposed to.
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId, versionSequenceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Try to create service <typeparamref name="TService"/>
        /// </summary>
        public bool TryCreateService<TService>(out TService serviceInstance)
        {
            if (this.TryCreateService(typeof(TService), out object service))
            {
                serviceInstance = (TService)service;
                return true;
            }
            serviceInstance = default(TService);
            return false;
        }

        /// <summary>
        /// Try to create service <paramref name="serviceType"/>
        /// </summary>
        public bool TryCreateService(Type serviceType, out object serviceInstance)
        {
            // Look for concrete services
            var st = AppDomain.CurrentDomain.GetAllTypes().FirstOrDefault(o => typeof(IAdoPersistenceService).IsAssignableFrom(o) && serviceType.IsAssignableFrom(o));
            if (st != null)
            {
                var adoManager = this.m_serviceManager.GetService(typeof(AdoPersistenceService));
                serviceInstance = Activator.CreateInstance(st, adoManager);
                return true;
            }
            else if (st == null && serviceType.IsGenericType && (serviceType.GetGenericTypeDefinition() == typeof(IDataPersistenceService<>) || typeof(IDataPersistenceService).IsAssignableFrom(serviceType)))
            {
                var adoManager = this.m_serviceManager.GetService(typeof(AdoPersistenceService));
                serviceInstance = null;
                var wrappedType = serviceType.GetGenericArguments()[0];
                var map = ModelMap.Load(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName));
                var classMapData = map.Class.FirstOrDefault(m => m.ModelType == wrappedType);
                if (classMapData != null) // We have a map!!!
                {
                    // Is there a persistence service?
                    var idpType = typeof(IDataPersistenceService<>);
                    Type modelClassType = Type.GetType(classMapData.ModelClass),
                        domainClassType = Type.GetType(classMapData.DomainClass);

                    idpType = idpType.MakeGenericType(modelClassType);

                    if (modelClassType.IsAbstract || domainClassType.IsAbstract)
                    {
                        return false;
                    }

                    this.m_tracer.TraceVerbose("Creating map {0} > {1}", modelClassType, domainClassType);

                    if (this.m_persistenceCache.ContainsKey(modelClassType))
                        this.m_tracer.TraceWarning("Duplicate initialization of {0}", modelClassType);
                    else if (modelClassType.Implements(typeof(IBaseEntityData)) &&
                       domainClassType.Implements(typeof(IDbBaseData)))
                    {
                        // Construct a type
                        Type pclass = null;
                        if (modelClassType.Implements(typeof(IVersionedAssociation)))
                            pclass = typeof(GenericBaseVersionedAssociationPersistenceService<,>);
                        else if (modelClassType.Implements(typeof(ISimpleAssociation)))
                            pclass = typeof(GenericBaseAssociationPersistenceService<,>);
                        else
                            pclass = typeof(GenericBasePersistenceService<,>);
                        pclass = pclass.MakeGenericType(modelClassType, domainClassType);
                        serviceInstance = Activator.CreateInstance(pclass, adoManager);
                        // Add to cache since we're here anyways
                        this.m_persistenceCache.Add(modelClassType, serviceInstance as IAdoPersistenceService);
                        return true;
                    }
                    else if (modelClassType.Implements(typeof(IIdentifiedEntity)) &&
                        domainClassType.Implements(typeof(IDbIdentified)))
                    {
                        // Construct a type
                        Type pclass = null;
                        if (modelClassType.Implements(typeof(IVersionedAssociation)))
                            pclass = typeof(GenericIdentityVersionedAssociationPersistenceService<,>);
                        else if (modelClassType.Implements(typeof(ISimpleAssociation)))
                            pclass = typeof(GenericIdentityAssociationPersistenceService<,>);
                        else
                            pclass = typeof(GenericIdentityPersistenceService<,>);

                        pclass = pclass.MakeGenericType(modelClassType, domainClassType);
                        serviceInstance = Activator.CreateInstance(pclass, adoManager);
                        this.m_persistenceCache.Add(modelClassType, serviceInstance as IAdoPersistenceService);
                        return true;
                    }
                    else
                    {
                        this.m_tracer.TraceWarning("Classmap {0}>{1} cannot be created, ignoring", modelClassType, domainClassType);
                        return false;
                    }
                }
            }
            serviceInstance = null;
            return false;
        }
    }
}