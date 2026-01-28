/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;

namespace SanteDB
{
    /// <summary>
    /// SanteDB service.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    [ExcludeFromCodeCoverage]
    public partial class SanteDBService : ServiceBase
    {
        private ConsoleParameters _CommandLineParameters;


        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDBService"/> class.
        /// </summary>
        public SanteDBService()
        {
            InitializeComponent();
            this.ServiceName = "SanteDB Host Process";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SanteDBService"/> class with the specified parameters from the command line.
        /// </summary>
        /// <param name="commandLineParameters">The command line parameters to use when starting the service.</param>
        public SanteDBService(ConsoleParameters commandLineParameters)
        {
            _CommandLineParameters = commandLineParameters;
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            if (null == _CommandLineParameters)
            {
                _CommandLineParameters = new ParameterParser<ConsoleParameters>().Parse(args);
            }

            try
            {
                ServiceUtil.Start(typeof(Program).GUID, new ServerApplicationContext(_CommandLineParameters.ConfigFile));
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
