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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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

            RestServiceConfiguration restConfiguration = new RestServiceConfiguration();

            restConfiguration.ServiceBehaviors = serviceElement.SelectNodes("./*[local-name() = 'behavior']").OfType<XmlElement>().Select(b=>
            {
                Type authType = Type.GetType(b.InnerText);
                if (authType == null)
                    throw new ConfigurationErrorsException($"Cannot find the authorization policy type {b.InnerText}");
                return authType;
            }).ToList();

            // Add the endpoints
            restConfiguration.Endpoints.AddRange(serviceElement.SelectNodes("./*[local-name() = 'endpoint']").OfType<XmlElement>().Select(o => o.InnerText));

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
                epHandlers = typeof(HdsiConfiguration).Assembly.ExportedTypes.Where(t => !t.IsAbstract && !t.IsInterface && typeof(IResourceHandler).IsAssignableFrom(t)).ToList();

            return new HdsiConfiguration(restConfiguration, epHandlers);
        }
    }
}
