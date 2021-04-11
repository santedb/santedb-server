﻿using SanteDB.Core.Security;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents a local policy
    /// </summary>
    internal sealed class AdoSecurityPolicy : IHandledPolicy
    {

        // Handler cache
        private static IDictionary<String, IPolicyHandler> s_handlers = new ConcurrentDictionary<String, IPolicyHandler>();

        // Policy handler
        private readonly IPolicyHandler m_handler;

        /// <summary>
        /// New ADO Security policy
        /// </summary>
        internal AdoSecurityPolicy()
        {

        }

        /// <summary>
        /// Create a local security policy
        /// </summary>
        internal AdoSecurityPolicy(DbSecurityPolicy policy)
        {

            this.CanOverride = policy.CanOverride;
            this.Key = policy.Key;
            this.Name = policy.Name;
            this.Oid = policy.Oid;
            this.IsActive = policy.ObsoletionTime == null || policy.ObsoletionTime < DateTimeOffset.Now;

            if (!String.IsNullOrEmpty(policy.Handler) && !s_handlers.TryGetValue(policy.Handler, out this.m_handler))
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
                 s_handlers.Add(policy.Handler, this.m_handler);
            }
        }

        /// <summary>
        /// Gets an indicator of whether the policy can be overridden
        /// </summary>
        public bool CanOverride { get; }

        /// <summary>
        /// Gets or sets the policy identifier
        /// </summary>
        public Guid Key { get; }

        /// <summary>
        /// Gets the name of the policy
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the OID of the policy
        /// </summary>
        public string Oid { get; }

        /// <summary>
        /// Policy handler
        /// </summary>
        public IPolicyHandler Handler => this.m_handler;

        /// <summary>
        /// Is active?
        /// </summary>
        public bool IsActive { get;  }

        /// <summary>
        /// Will never be null
        /// </summary>
        public bool IsLogicalNull => false;


    }
}
