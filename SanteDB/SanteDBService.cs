/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Server;
using SanteDB.Server.Core.Services.Impl;
using System.ServiceProcess;

namespace SanteDB
{
    /// <summary>
    /// SanteDB service.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    public partial class SanteDBService : ServiceBase
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="SanteDBService"/> class.
		/// </summary>
		public SanteDBService()
		{
			InitializeComponent();
			this.ServiceName = "SanteDB Host Process";
		}

		/// <summary>
		/// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
		/// </summary>
		/// <param name="args">Data passed by the start command.</param>
		protected override void OnStart(string[] args)
		{
			ExitCode = ServiceUtil.Start(typeof(Program).GUID, new FileConfigurationService(null));
			if (ExitCode != 0)
				Stop();
		}

		/// <summary>
		/// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
		/// </summary>
		protected override void OnStop()
		{
			ServiceUtil.Stop();
		}
	}
}
