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
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SanteDB.Persistence.MDM.Services
{
    /// <summary>
    /// Represents a resource listener that handles bundles
    /// </summary>
    public class BundleResourceListener : MdmResourceListener<Bundle>
    {
        // Listeners for chaining
        private IEnumerable<MdmResourceListener> m_listeners;

        /// <summary>
        /// Bundle resource listener
        /// </summary>
        public BundleResourceListener(IEnumerable<MdmResourceListener> listeners) : base(new ResourceMergeConfiguration(typeof(Bundle), "default", false))
        {
            this.m_listeners = listeners;
        }

        /// <summary>
        /// As a bundle, we call the base on the contents of the data
        /// </summary>
        protected override void OnPrePersistenceValidate(object sender, DataPersistingEventArgs<Bundle> e)
        {
            this.ChainInvoke(sender, e, e.Data, nameof(OnPrePersistenceValidate), typeof(DataPersistingEventArgs<>));
        }

        /// <summary>
        /// Chain invoke a bundle on inserted
        /// </summary>
        protected override void OnInserting(object sender, DataPersistingEventArgs<Bundle> e)
        {
            this.ChainInvoke(sender, e, e.Data, nameof(OnInserting), typeof(DataPersistingEventArgs<>));
        }

        /// <summary>
        /// Chain invoke a bundle
        /// </summary>
        protected override void OnSaving(object sender, DataPersistingEventArgs<Bundle> e)
        {
            this.ChainInvoke(sender, e, e.Data, nameof(OnSaving), typeof(DataPersistedEventArgs<>));
        }

        /// <summary>
        /// On obsoleting
        /// </summary>
        protected override void OnObsoleting(object sender, DataPersistingEventArgs<Bundle> e)
        {
            this.ChainInvoke(sender, e, e.Data, nameof(OnObsoleting), typeof(DataPersistingEventArgs<>));
        }

        /// <summary>
        /// Merging a bundle? Impossible
        /// </summary>
        public override Bundle Merge(Bundle master, IEnumerable<Bundle> linkedDuplicates)
        {
            throw new NotSupportedException("Cannot merge bundles");
        }

        /// <summary>
        /// Un-merge a bundle? Even more impossible than merging
        /// </summary>
        public override Bundle Unmerge(Bundle master, Bundle unmergeDuplicate)
        {
            throw new NotSupportedException("Cannot unmerge bundles");
        }

        /// <summary>
        /// Cannot query bundles
        /// </summary>
        protected override void OnQuerying(object sender, QueryRequestEventArgs<Bundle> e)
        {
            throw new NotSupportedException("Cannot query bundles");
        }

        /// <summary>
        /// Cannot query bundles
        /// </summary>
        protected override void OnQueried(object sender, QueryResultEventArgs<Bundle> e)
        {
            throw new NotSupportedException("Cannot query bundles");
        }

        /// <summary>
        /// Cannot query bundles
        /// </summary>
        protected override void OnRetrieved(object sender, DataRetrievedEventArgs<Bundle> e)
        {
            throw new NotSupportedException("Cannot retrieve bundles");
        }

        /// <summary>
        /// Cannot query bundles
        /// </summary>
        protected override void OnRetrieving(object sender, DataRetrievingEventArgs<Bundle> e)
        {
            throw new NotSupportedException("Cannot retrieve bundles");
        }
        /// <summary>
        /// Performs a chain invokation on the bundle's contents
        /// </summary>
        private void ChainInvoke(object sender, object eventArgs, Bundle bundle, string methodName, Type argType)
        {
            for (int i = 0; i < bundle.Item.Count; i++)
            {
                var data = bundle.Item[i];
                var mdmHandler = typeof(MdmResourceListener<>).MakeGenericType(data.GetType());
                var evtArgType = argType.MakeGenericType(data.GetType());
                var evtArgs = Activator.CreateInstance(evtArgType, data, (eventArgs as SecureAccessEventArgs).Principal);

                foreach (var hdlr in this.m_listeners.Where(o => o.GetType() == mdmHandler))
                {
                    var exeMethod = mdmHandler.GetRuntimeMethods().Single(m => m.Name == methodName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new Type[] { typeof(Object), evtArgType }));
                    exeMethod.Invoke(hdlr, new Object[] { bundle, evtArgs });

                    // Cancel?
                    bundle.Item[i] = evtArgType.GetRuntimeProperty("Data").GetValue(evtArgs) as IdentifiedData;

                    if(eventArgs is DataPersistingEventArgs<Bundle>)
                        (eventArgs as DataPersistingEventArgs<Bundle>).Cancel |= (bool)evtArgType.GetRuntimeProperty("Cancel").GetValue(evtArgs);

                }
            }

            // Now that bundle is processed , process it
            if ((eventArgs as DataPersistingEventArgs<Bundle>)?.Cancel == true && (methodName == "OnInserting" || methodName == "OnSaving"))
            {
                var businessRulesSerice = ApplicationServiceContext.Current.GetBusinessRulesService<Bundle>();
                bundle = businessRulesSerice?.BeforeInsert(bundle) ?? bundle;
                // Business rules shouldn't be used for relationships, we need to delay load the sources
                bundle.Item.OfType<EntityRelationship>().ToList().ForEach((i) =>
                {
                    if (i.SourceEntity == null)
                        i.SourceEntity = bundle.Item.Find(o => o.Key == i.SourceEntityKey) as Entity;
                });
                bundle = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Insert(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                bundle = businessRulesSerice?.AfterInsert(bundle) ?? bundle;

            }

        }
    }
}
