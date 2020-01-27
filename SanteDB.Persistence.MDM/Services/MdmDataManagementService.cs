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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Persistence.MDM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.MDM.Services
{
    /// <summary>
    /// The MdmRecordDaemon is responsible for subscribing to MDM targets in the configuration 
    /// and linking/creating master records whenever a record of that type is created.
    /// </summary>
    [ServiceProvider("MIDM Data Repository")]
    public class MdmDataManagementService : IDaemonService, IDisposable
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "MIDM Daemon";

        // TRace source
        private Tracer m_traceSource = new Tracer(MdmConstants.TraceSourceName);

        // Configuration
        private ResourceMergeConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<ResourceMergeConfigurationSection>();

        // Listeners
        private List<MdmResourceListener> m_listeners = new List<MdmResourceListener>();

        // True if the service is running
        public bool IsRunning => this.m_listeners.Count > 0;

        /// <summary>
        /// Daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Daemon is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Daemon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Daemon has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Dispose of this object
        /// </summary>
        public void Dispose()
        {
            foreach (var i in this.m_listeners)
                i.Dispose();
            this.m_listeners.Clear();
        }

        /// <summary>
        /// Start the daemon
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            // Pre-register types for serialization
            foreach(var itm in this.m_configuration.ResourceTypes)
            {
                var rt = itm.ResourceType;
                string typeName = $"{rt.Name}Master";
                if (typeof(Entity).IsAssignableFrom(rt))
                    rt = typeof(EntityMaster<>).MakeGenericType(rt);
                else if (typeof(Act).IsAssignableFrom(rt))
                    rt = typeof(ActMaster<>).MakeGenericType(rt);
                ModelSerializationBinder.RegisterModelType(typeName, rt);
            }
            // Wait until application context is started
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                if (ApplicationServiceContext.Current.GetService<IRecordMatchingService>() == null)
                    throw new InvalidOperationException("MDM requires a matching service to be configured");
                else if (ApplicationContext.Current.GetService<SimResourceMergeService>() != null)
                    throw new System.Configuration.ConfigurationException("Cannot use MDM and SIM merging strategies at the same time. Please disable one or the other");

                foreach(var itm in this.m_configuration.ResourceTypes)
                {
                    this.m_traceSource.TraceInfo("Adding MDM listener for {0}...", itm.ResourceType.Name);
                    var idt = typeof(MdmResourceListener<>).MakeGenericType(itm.ResourceType);
                    this.m_listeners.Add(Activator.CreateInstance(idt, itm) as MdmResourceListener);
                }

                // Add an MDM listener for subscriptions
                var subscService = ApplicationServiceContext.Current.GetService<ISubscriptionExecutor>();
                if(subscService != null)
                    subscService.Executed += MdmSubscriptionExecuted;

                this.m_listeners.Add(new BundleResourceListener(this.m_listeners));
            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Fired when the MDM Subscription has been executed
        /// </summary>
        private void MdmSubscriptionExecuted(object sender, Core.Event.QueryResultEventArgs<Core.Model.IdentifiedData> e)
        {
            var lresults = e.Results.ToList();

            foreach(var itm in this.m_configuration.ResourceTypes)
            {
                var exprFunc = typeof(Func<,>).MakeGenericType(itm.ResourceType, typeof(Boolean));
                exprFunc = typeof(Expression<>).MakeGenericType(exprFunc);
                if (!exprFunc.IsAssignableFrom(e.Query.GetType())) continue;

                // We have a resource type that matches
                foreach(var res in e.Results)
                {
                    // Result is taggable and a tag exists for MDM
                    if(res is Entity entity && entity.Tags.Any(o=>o.TagKey == "mdm.type" && o.Value != "M"))
                    {
                        // Attempt to load the master and add to the results
                        var master = entity.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship);
                        lresults.Add(master.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity)));
                    }
                    else if(res is Act act && act.Tags.Any(o=>o.TagKey == "mdm.type" && o.Value != "M"))
                    {
                        // Attempt to load the master and add to the results
                        var master = act.LoadCollection<ActRelationship>(nameof(Act.Relationships)).FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship);
                        lresults.Add(master.LoadProperty<Act>(nameof(ActRelationship.TargetAct)));
                    }
                }
            }

            e.Results = lresults;
        }

        /// <summary>
        /// Stop the daemon
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            // Unregister
            foreach (var i in this.m_listeners)
                i.Dispose();
            this.m_listeners.Clear();

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
