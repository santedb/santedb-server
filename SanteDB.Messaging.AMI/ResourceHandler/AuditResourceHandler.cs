using SanteDB.Core.Model.AMI.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;
using SanteDB.Core.Model.Query;
using SanteDB.Messaging.AMI.Wcf;
using MARC.HI.EHRS.SVC.Auditing.Data;
using SanteDB.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security;
using SanteDB.Core.Model;
using MARC.HI.EHRS.SVC.Auditing.Services;

namespace SanteDB.Messaging.AMI.ResourceHandler
{
    /// <summary>
    /// Represents a resource handler which can persist and forward audits
    /// </summary>
    public class AuditResourceHandler : IResourceHandler
    {

        // The audit repository
        private IAuditRepositoryService m_repository = null;

        /// <summary>
        /// Initializes the audit resource handler
        /// </summary>
        public AuditResourceHandler()
        {
            ApplicationContext.Current.Started += (o, e) => this.m_repository = ApplicationContext.Current.GetService<IAuditRepositoryService>(); 
        }

        /// <summary>
        /// Get the capabilities of this handler
        /// </summary>
        public ResourceCapability Capabilities => ResourceCapability.Create | ResourceCapability.Get | ResourceCapability.Search;

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string ResourceName => "audit";

        /// <summary>
        /// Get the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type this persists
        /// </summary>
        public Type Type => typeof(AuditInfo);

        /// <summary>
        /// Create the audits in the audit data
        /// </summary>
        /// <param name="data">The audit data to send/insert</param>
        /// <param name="updateIfExists">Ignored for this provider</param>
        /// <returns>void</returns>
        public object Create(object data, bool updateIfExists)
        {
            if (this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var auditData = data as AuditSubmission;
            if (auditData == null) // may be a single audit
            {
                var singleAudit = data as AuditInfo;
                if (singleAudit != null)
                {
                    this.m_repository.Insert(singleAudit);
                    ApplicationContext.Current.GetService<IAuditorService>()?.SendAudit(singleAudit);
                }
            }
            else
            {
                auditData.Audit.ForEach(o =>
                {
                    this.m_repository.Insert(o);
                    ApplicationContext.Current.GetService<IAuditorService>()?.SendAudit(o);
                });
                // Send the audit to the audit repo
            }
            return null;
        }

        /// <summary>
        /// Get the specified audit identifier from the database
        /// </summary>
        /// <param name="id">The identifier of the audit to retrieve</param>
        /// <param name="versionId">Ignored</param>
        /// <returns>The fetched audit information</returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public object Get(object id, object versionId)
        {
            if (this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var retVal = new AuditInfo();
            retVal.CopyObjectData(this.m_repository.Get(id));

            return retVal;
        }

        /// <summary>
        /// Obsolete the audit
        /// </summary>
        /// <param name="key">Not supported</param>
        /// <returns></returns>
        public object Obsolete(object key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for the audit 
        /// </summary>
        /// <param name="queryParameters">The query to perform</param>
        /// <returns>The matching audits</returns>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Perform the query for audits
        /// </summary>
        /// <param name="queryParameters">The filter parameters for the audit</param>
        /// <param name="offset">The first result to retrieve</param>
        /// <param name="count">The count of objects to retrieve</param>
        /// <param name="totalCount">The total couns of objects</param>
        /// <returns>An array of objects matching the query </returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AccessAuditLog)]
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            if(this.m_repository == null)
                throw new InvalidOperationException("No audit repository is configured");

            var filter = QueryExpressionParser.BuildLinqExpression<AuditData>(queryParameters);
            return this.m_repository.Find(filter, offset, count, out totalCount);
        }

        /// <summary>
        /// Perform an update on an audit
        /// </summary>
        /// <param name="data">The data to be updated</param>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
