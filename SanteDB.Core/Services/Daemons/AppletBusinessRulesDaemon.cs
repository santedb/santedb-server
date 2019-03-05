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
using SanteDB.BusinessRules.JavaScript;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Interfaces;
using System;
using System.ComponentModel;

namespace SanteDB.Core.Services.Daemons
{
    /// <summary>
    /// A daemon which loads business rules from the applet manager
    /// </summary>
    [ServiceProvider("Applet JavaScript BRE", Dependencies = new Type[] { typeof(IAppletManagerService) })]
    public class AppletBusinessRulesDaemon : IDaemonService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Applet JavaScript BRE";

        /// <summary>
        /// Indicates whether the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Service has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Service is starting
        /// </summary>
        public event EventHandler Starting;
       /// <summary>
       /// Service has stopped
       /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// Service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the service which will register items with the business rules handler
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                ApplicationServiceContext.Current = ApplicationServiceContext.Current;
                if (ApplicationServiceContext.Current.GetService<IDataReferenceResolver>() == null)
                    (ApplicationServiceContext.Current as IServiceManager).AddServiceProvider(typeof(AppletDataReferenceResolver));
                new AppletBusinessRuleLoader().LoadRules();
            };
            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stopping
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
