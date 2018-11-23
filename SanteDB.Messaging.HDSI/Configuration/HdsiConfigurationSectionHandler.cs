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
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI.Resources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SanteDB.Messaging.HDSI.Configuration
{
    /// <summary>
    /// Configuration handler for the HDSI section
    /// </summary>
    public class HdsiConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Create the configuration
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            
            // Section
            XmlElement serviceElement = section.SelectSingleNode("./*[local-name() = 'service']") as XmlElement;
            
            var resourceHandlers = section.SelectNodes("./*[local-name() = 'resourceHandler']/*[local-name() = 'add']");
            List<Type> epHandlers = new List<Type>();
            foreach (XmlElement xel in resourceHandlers)
            {
                var type = xel.Attributes["type"]?.Value;
                if (type == null)
                    throw new ConfigurationErrorsException("Resource handler must carry @type attribute");
                var t = Type.GetType(type);
                if (t == null)
                    throw new ConfigurationErrorsException($"Cannot find type described by {type}");
                epHandlers.Add(t);
            }
            if (epHandlers.Count == 0) // Use all resource handlers in "this"
                epHandlers = typeof(PatientResourceHandler).Assembly.ExportedTypes.Where(t => !t.IsAbstract && !t.IsInterface && typeof(IResourceHandler).IsAssignableFrom(t)).ToList();

            return new HdsiConfiguration(epHandlers);
        }
    }
}
