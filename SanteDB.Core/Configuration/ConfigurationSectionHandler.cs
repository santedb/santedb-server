/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {

        /// <summary>
        /// Trace source name
        /// </summary>
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.ServiceTraceSourceName);

        /// <summary>
        /// Create the configuration
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            SanteDBServerConfiguration retVal = new SanteDBServerConfiguration();

            if (section == null)
                throw new InvalidOperationException("Can't find configuration section");

            retVal.ServiceProviders = new List<Type>();

            XmlNode serviceProviderSection = section.SelectSingleNode("./*[local-name() = 'serviceProviders']");

            if (serviceProviderSection != null) // Load providers data
                foreach (XmlNode nd in serviceProviderSection.SelectNodes("./*[local-name() = 'add']/@type"))
                {
                    Type t = Type.GetType(nd.Value);
                    if (t != null)
                    {
                        ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                        if (ci != null)
                        {
                            retVal.ServiceProviders.Add(t);
                            this.m_traceSource.TraceInfo("Added provider {0}", t.FullName);
                        }
                        else
                            this.m_traceSource.TraceInfo("Can't find parameterless constructor on type {0}", t.FullName);
                    }
                    else
                        this.m_traceSource.TraceInfo("Can't find type described by '{0}'", nd.Value);
                }



            return retVal;
        }
    }
}
