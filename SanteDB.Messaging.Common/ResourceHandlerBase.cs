/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * Date: 2017-9-1
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using System.Xml.Serialization;
using System.Reflection;
using SanteDB.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Interop;
using SanteDB.Core.Interfaces;

namespace SanteDB.Messaging.Common
{
    /// <summary>
    /// Resource handler base
    /// </summary>
    public abstract class ResourceHandlerBase<TResource> : IResourceHandler, IAuditEventSource where TResource : IdentifiedData
    {

        /// <summary>
        /// IRepository service
        /// </summary>
        private IRepositoryService<TResource> m_repository = null;

        /// <summary>
        /// Constructs the resource handler base
        /// </summary>
        public ResourceHandlerBase()
        {
            ApplicationContext.Current.Started += (o, e) => this.m_repository = ApplicationContext.Current.GetService<IRepositoryService<TResource>>();
        }

        /// <summary>
        /// Gets the scope of the resource handler
        /// </summary>
        public abstract Type Scope { get; }

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public virtual string ResourceName
        {
            get
            {
                return typeof(TResource).GetCustomAttribute<XmlRootAttribute>().ElementName;
            }
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(TResource);
            }
        }

        /// <summary>
        /// Gets the capabilities of this resource handler
        /// </summary>
        public virtual ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Create |
                    ResourceCapability.CreateOrUpdate |
                    ResourceCapability.Delete |
                    ResourceCapability.Get |
                    ResourceCapability.GetVersion |
                    ResourceCapability.History |
                    ResourceCapability.Search |
                    ResourceCapability.Update;
            }
        }

        /// <summary>
        /// Fired when data is created
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataCreated;
        /// <summary>
        /// Fired when data is updated
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataUpdated;
        /// <summary>
        /// Fired when data is obsoleted
        /// </summary>
        public event EventHandler<AuditDataEventArgs> DataObsoleted;
        /// <summary>
        /// Fired when data is disclosed
        /// </summary>
        public event EventHandler<AuditDataDisclosureEventArgs> DataDisclosed;

        /// <summary>
        /// Create a resource
        /// </summary>
        public virtual Object Create(Object data, bool updateIfExists)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            else if ((this.Capabilities & ResourceCapability.Create) == 0 &&
                (this.Capabilities & ResourceCapability.CreateOrUpdate) == 0)
                throw new NotSupportedException();

            var bundle = data as Bundle;

            bundle?.Reconstitute();

            var processData = bundle?.Entry ?? data;

            if (processData is Bundle)
                throw new InvalidOperationException("Bundle must have an entry point");

            try
            {
                if (processData is TResource)
                {
                    var resourceData = processData as TResource;
                    resourceData = updateIfExists ? this.m_repository.Save(resourceData) : this.m_repository.Insert(resourceData);
                    this.DataCreated?.Invoke(this, new AuditDataEventArgs(resourceData));
                    return resourceData;
                }
            }
            catch(Exception e)
            {
                this.DataCreated?.Invoke(this, new AuditDataEventArgs(data, e) { Success = false });
                throw e;
            }

            throw new ArgumentException(nameof(data), "Invalid data type");
        }

        /// <summary>
        /// Read clinical data
        /// </summary>
        public virtual Object Get(object id, object versionId)
        {
            if ((this.Capabilities & ResourceCapability.Get) == 0 &&
                (this.Capabilities & ResourceCapability.GetVersion) == 0)
                throw new NotSupportedException();

            try
            {
                var retVal = this.m_repository.Get((Guid)id, (Guid)versionId);
                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(id.ToString(), new object[] { retVal }));
                return retVal;
            }
            catch(Exception e)
            {
                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(id.ToString(), new Object[] { e }) { Success = false });
                throw e;
            }
        }

        /// <summary>
        /// Obsolete data
        /// </summary>
        public virtual Object Obsolete(object key)
        {
            if ((this.Capabilities & ResourceCapability.Delete) == 0)
                throw new NotSupportedException();

            try
            {
                var retVal = this.m_repository.Obsolete((Guid)key);
                this.DataObsoleted?.Invoke(this, new AuditDataEventArgs(retVal));
                return retVal;
            }
            catch(Exception e)
            {
                this.DataObsoleted?.Invoke(this, new AuditDataEventArgs(key) { Success = false });
                throw e;
            }
        }

        /// <summary>
        /// Perform a query 
        /// </summary>
        public virtual IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            if ((this.Capabilities & ResourceCapability.Search) == 0)
                throw new NotSupportedException();

            try
            {
                int tr = 0;
                var retVal = this.Query(queryParameters, 0, 100, out tr);
                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(queryParameters.ToString(), retVal));
                return retVal;
            }
            catch (Exception e)
            {
                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(queryParameters.ToString(), new object[] { e }) { Success = false });
                throw e;
            }

        }

        /// <summary>
        /// Perform the actual query
        /// </summary>
        public virtual IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            if ((this.Capabilities & ResourceCapability.Search) == 0)
                throw new NotSupportedException();
            try
            {

                var queryExpression = QueryExpressionParser.BuildLinqExpression<TResource>(queryParameters, null, false);
                List<String> query = null, id = null;

                IEnumerable<TResource> retVal = null;
                if (queryParameters.TryGetValue("_id", out id)) {
                    var obj = this.m_repository.Get(Guid.Parse(id.First()));
                    if (obj != null)
                        retVal = new List<TResource>() { obj };
                    else
                        retVal = new List<TResource>();
                    totalCount = retVal.Count();
                }
                else if (queryParameters.TryGetValue("_queryId", out query) && this.m_repository is IPersistableQueryRepositoryService)
                {
                    Guid queryId = Guid.Parse(query[0]);
                    List<String> lean = null;
                    if (queryParameters.TryGetValue("_lean", out lean) && lean[0] == "true" && this.m_repository is IFastQueryRepositoryService)
                        retVal = (this.m_repository as IFastQueryRepositoryService).FindFast<TResource>(queryExpression, offset, count, out totalCount, queryId);
                    else
                        retVal = (this.m_repository as IPersistableQueryRepositoryService).Find<TResource>(queryExpression, offset, count, out totalCount, queryId);
                }
                else
                {
                    List<String> lean = null;
                    if (queryParameters.TryGetValue("_lean", out lean) && lean[0] == "true" && this.m_repository is IFastQueryRepositoryService)
                        retVal = (this.m_repository as IFastQueryRepositoryService).FindFast<TResource>(queryExpression, offset, count, out totalCount, Guid.Empty);
                    else
                        retVal = this.m_repository.Find(queryExpression, offset, count, out totalCount);
                }

                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(queryParameters.ToString(), retVal));
                return retVal;
            }
            catch (Exception e)
            {
                this.DataDisclosed?.Invoke(this, new AuditDataDisclosureEventArgs(queryParameters.ToString(), new object[] { e }) { Success = false });
                throw e;
            }
        }

        /// <summary>
        /// Perform an update
        /// </summary>
        public virtual Object Update(Object data)
        {

            if ((this.Capabilities & ResourceCapability.Update) == 0)
                throw new NotSupportedException();

            Bundle bundleData = data as Bundle;
            bundleData?.Reconstitute();
            var processData = bundleData?.Entry ?? data;

            try
            {
                if (processData is Bundle)
                    throw new InvalidOperationException(string.Format("Bundle must have entry of type {0}", typeof(TResource).Name));
                else if (processData is TResource)
                {
                    var entityData = data as TResource;

                    var retVal = this.m_repository.Save(entityData);
                    this?.DataUpdated(this, new AuditDataEventArgs(retVal));
                    return retVal;
                }
                else
                {
                    throw new ArgumentException("Invalid persistence type");
                }
            }
            catch (Exception e)
            {
                this.DataUpdated?.Invoke(this, new AuditDataEventArgs(data, e) { Success = false });
                throw e;
            }
        }
    }
}
