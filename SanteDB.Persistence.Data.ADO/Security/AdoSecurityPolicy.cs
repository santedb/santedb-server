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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;

namespace SanteDB.Persistence.Data.PSQL.Security
{
    /// <summary>
    /// Represents a local policy
    /// </summary>
    public class AdoSecurityPolicy : ILocalPolicy, IIdentifiedEntity
    {

        // Handler cache
        private static Dictionary<String, IPolicyHandler> s_handlers = new Dictionary<String, IPolicyHandler>();
        private static Object s_lockObject = new object();

        // Policy handler
        private IPolicyHandler m_handler;


        /// <summary>
        /// Create a local security policy
        /// </summary>
        public AdoSecurityPolicy(DbSecurityPolicy policy)
        {

            this.CanOverride = policy.CanOverride;
            this.Key = policy.Key;
            this.Name = policy.Name;
            this.Oid = policy.Oid;
            this.IsActive = policy.ObsoletionTime == null || policy.ObsoletionTime < DateTimeOffset.Now;

            if(!String.IsNullOrEmpty(policy.Handler) && !s_handlers.TryGetValue(policy.Handler, out this.m_handler))
            {
                Type handlerType = Type.GetType(policy.Handler);
                if (handlerType == null)
                    throw new InvalidOperationException("Cannot find policy handler");
                var ci = handlerType.GetConstructor(Type.EmptyTypes);
                if (ci == null)
                    throw new InvalidOperationException("Cannot find parameterless constructor");
                this.m_handler = ci.Invoke(null) as IPolicyHandler;
                if (this.m_handler == null)
                    throw new InvalidOperationException("Policy handler does not implement IPolicyHandler");
                lock(s_lockObject)
                    s_handlers.Add(policy.Handler, this.m_handler);
            }
        }
        
        /// <summary>
        /// Gets an indicator of whether the policy can be overridden
        /// </summary>
        public bool CanOverride { get; private set; }

        /// <summary>
        /// Gets or sets the policy identifier
        /// </summary>
        public Guid? Key { get; set; }

        /// <summary>
        /// Gets the name of the policy
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the OID of the policy
        /// </summary>
        public string Oid { get; private set; }

        /// <summary>
        /// Policy handler
        /// </summary>
        public IPolicyHandler Handler
        {
            get
            {
                return this.m_handler;
            }
        }

        /// <summary>
        /// Is active?
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Will never be null
        /// </summary>
        public bool IsLogicalNull
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the load state
        /// </summary>
        public LoadState LoadState
        {
            get
            {
                return LoadState.FullLoad;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets the key for the policy
        /// </summary>
        Guid IPolicy.Key => this.Key.Value;

    }
}
