/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SanteDB.Core.Security.Privacy
{
    /// <summary>
    /// Local policy enforcement point service
    /// </summary>
    [ServiceProvider("Default Policy Enforcement Service")]
    public class LocalPolicyEnforcementPointService : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Default Policy Enforcement Service";

        // Set to true when the application context has stopped
        private bool m_safeToStop = false;

        // Security tracer
        private Tracer m_tracer = new Tracer(SanteDBConstants.SecurityTraceSourceName);

        // Security configuration
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        // Subscribed listeners
        private Dictionary<IDataPersistenceService, KeyValuePair<Delegate, Delegate>> m_subscribedListeners = new Dictionary<IDataPersistenceService, KeyValuePair<Delegate, Delegate>>();

        /// <summary>
        /// Determines whether the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_subscribedListeners.Count > 0;
            }
        }

        /// <summary>
        /// The service is started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// The service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// The service is stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// The service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Starts the service
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            ApplicationServiceContext.Current.Started += (o, e) => this.BindEvents();

            this.Started?.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Stop the policy enforcement service
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            // Audit tool should never stop!!!!!
            if (!this.m_safeToStop)
                this.UnBindEvents();

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Binds the specified events
        /// </summary>
        private void BindEvents()
        {
            var svcManager = ApplicationServiceContext.Current.GetService<IServiceManager>();

            this.m_tracer.TraceInfo("Starting bind to persistence services...");

            foreach (var t in typeof(Act).Assembly.GetTypes().Where(o => typeof(Act).IsAssignableFrom(o)))
            {

                var svcType = typeof(IDataPersistenceService<>).MakeGenericType(t);
                var svcInstance = ApplicationServiceContext.Current.GetService(svcType);

                // Now comes the tricky dicky part - We need to subscribe to a generic event
                if (svcInstance != null)
                {
                    this.m_tracer.TraceInfo("Binding to {0}...", svcType);

                    // Construct the delegate for query
                    var pqeArgType = typeof(QueryResultEventArgs<>).MakeGenericType(t);
                    var qevtHdlrType = typeof(EventHandler<>).MakeGenericType(pqeArgType);
                    var senderParm = Expression.Parameter(typeof(Object), "o");
                    var eventParm = Expression.Parameter(pqeArgType, "e");
                    var delegateData = Expression.Convert(Expression.MakeMemberAccess(eventParm, pqeArgType.GetRuntimeProperty("Results")), typeof(IEnumerable));
                    var ofTypeMethod = typeof(Enumerable).GetGenericMethod(nameof(Enumerable.OfType), new Type[] { t }, new Type[] { typeof(IEnumerable) }) as MethodInfo;
                    var queriedInstanceDelegate = Expression.Lambda(qevtHdlrType, Expression.Assign(delegateData.Operand, Expression.Convert(Expression.Call(ofTypeMethod, Expression.Call(Expression.Constant(this), typeof(LocalPolicyEnforcementPointService).GetRuntimeMethod(nameof(HandlePostQueryEvent), new Type[] { typeof(IEnumerable) }), delegateData)), delegateData.Operand.Type)), senderParm, eventParm).Compile();

                    // Bind to events
                    svcType.GetRuntimeEvent("Queried").AddEventHandler(svcInstance, queriedInstanceDelegate);

                    // Construct delegate for retrieve
                    pqeArgType = typeof(DataRetrievedEventArgs<>).MakeGenericType(t);
                    qevtHdlrType = typeof(EventHandler<>).MakeGenericType(pqeArgType);
                    senderParm = Expression.Parameter(typeof(Object), "o");
                    eventParm = Expression.Parameter(pqeArgType, "e");
                    delegateData = Expression.Convert(Expression.MakeMemberAccess(eventParm, pqeArgType.GetRuntimeProperty("Data")), t);
                    var retrievedInstanceDelegate = Expression.Lambda(qevtHdlrType, Expression.Assign(delegateData.Operand, Expression.Convert(Expression.Call(Expression.Constant(this), typeof(LocalPolicyEnforcementPointService).GetRuntimeMethod(nameof(HandlePostRetrieveEvent), new Type[] { t }), delegateData), t)), senderParm, eventParm).Compile();

                    // Bind to events
                    svcType.GetRuntimeEvent("Retrieved").AddEventHandler(svcInstance, retrievedInstanceDelegate);

                    this.m_subscribedListeners.Add(svcInstance as IDataPersistenceService, new KeyValuePair<Delegate, Delegate>(queriedInstanceDelegate, retrievedInstanceDelegate));
                }

            }

        }


        /// <summary>
        /// Handle post query event
        /// </summary>
        public IEnumerable HandlePostQueryEvent(IEnumerable results)
        {
            // If the current authentication context is a device (not a user) then we should allow the data to flow to the device
            switch(this.m_configuration.PepExemptionPolicy)
            {
                case PolicyEnforcementExemptionPolicy.AllExempt:
                    return results;
                case PolicyEnforcementExemptionPolicy.DevicePrincipalsExempt:
                    if (AuthenticationContext.Current.Principal.Identity is DeviceIdentity || AuthenticationContext.Current.Principal.Identity is ApplicationIdentity)
                        return results;
                    break;
                case PolicyEnforcementExemptionPolicy.UserPrincipalsExempt:
                    if (!(AuthenticationContext.Current.Principal.Identity is DeviceIdentity || AuthenticationContext.Current.Principal.Identity is ApplicationIdentity))
                        return results;
                    break;
            }

            // this is a very simple PEP which will enforce active policies in the result set.
            var pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();

            var decisions = results.OfType<Object>().AsParallel().Select(o=>new { Securable = o, Decision = pdp.GetPolicyDecision(AuthenticationContext.Current.Principal, o) });
            
            return decisions
                // We want to mask ELEVATE
                .Where(o => o.Decision.Outcome == PolicyGrantType.Elevate && o.Securable is Act).Select(
                    o => {
                        AuditUtil.AuditMasking(o.Securable as Act);
                        return new Act() {
                            ReasonConceptKey = NullReasonKeys.Masked,
                            Key = (o.Securable as IdentifiedData).Key
                        };
                    }
                )
                // We want to include all grant
                .Union(
                    decisions.Where(o => o.Decision.Outcome == PolicyGrantType.Grant).Select(o => o.Securable)
                )
                .ToList();
        }


        /// <summary>
        /// Handle post query event
        /// </summary>
        public Object HandlePostRetrieveEvent(Object result)
        {
            return result;
        }

        /// <summary>
        /// Removes bindings for the specified events
        /// </summary>
        private void UnBindEvents()
        {
            foreach(var i in this.m_subscribedListeners)
            {
                i.Key.GetType().GetRuntimeEvent("Queried").RemoveEventHandler(i.Key, i.Value.Key);
                i.Key.GetType().GetRuntimeEvent("Retrieved").RemoveEventHandler(i.Key, i.Value.Value);
            }
        }
    }
}
