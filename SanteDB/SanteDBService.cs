/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2022-5-30
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core;
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
            var parms = new ParameterParser<ConsoleParameters>().Parse(args);
            try
            {
                ServiceUtil.Start(typeof(Program).GUID, new ServerApplicationContext(parms.ConfigFile));
            }
            catch
            {
                Stop();
            }
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
