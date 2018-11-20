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
 * User: khannan
 * Date: 2017-9-1
 */
using SanteDB.Core.Interop;
using SanteDB.Messaging.AMI.Wcf;
using SanteDB.Rest.AMI.Resources;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace SanteDB.Messaging.AMI.Configuration
{
	/// <summary>
	/// Configuration section handler
	/// </summary>
	public class AmiConfigurationSectionHandler : IConfigurationSectionHandler
	{
		/// <summary>
		/// Create the configuration object
		/// </summary>
		public object Create(object parent, object configContext, XmlNode section)
		{
			XmlElement caConfigurationElement = section.SelectSingleNode("./*[local-name() = 'ca']") as XmlElement;
			CertificationAuthorityConfiguration caConfiguration = new CertificationAuthorityConfiguration();

            if (caConfigurationElement != null)
			{
				caConfiguration.AutoApprove = caConfigurationElement?.Attributes["autoApprove"]?.Value == "true";
				caConfiguration.Name = caConfigurationElement?.Attributes["cn"]?.Value;
				caConfiguration.ServerName = caConfigurationElement?.Attributes["serverName"]?.Value;
			}

			var endpoints = section.SelectNodes("./*[local-name() = 'endpoint']/*[local-name() = 'add']");
			List<ServiceEndpointOptions> epOptions = new List<ServiceEndpointOptions>();

			foreach (XmlElement xel in endpoints)
			{
				ServiceEndpointCapabilities caps = ServiceEndpointCapabilities.None;
				foreach (var si in xel.Attributes["capabilities"]?.Value?.Split(' ') ?? new String[0])
					caps |= (ServiceEndpointCapabilities)Enum.Parse(typeof(ServiceEndpointCapabilities), si);
				epOptions.Add(new ServiceEndpointOptions()
				{
					BaseUrl = xel.InnerText.Split(' '),
					ServiceType = (ServiceEndpointType)Enum.Parse(typeof(ServiceEndpointType), xel.Attributes["type"]?.Value),
					Capabilities = caps,
				});
			}


            var resourceHandlers = section.SelectNodes("./*[local-name() = 'resourceHandler']/*[local-name() = 'add']");
            List<Type> epHandlers = new List<Type>();
            foreach(XmlElement xel in resourceHandlers)
            {
                var type = xel.Attributes["type"]?.Value;
                if (type == null)
                    throw new ConfigurationErrorsException("Resource handler must carry @type attribute");
                var t = Type.GetType(type);
                if (t == null)
                    throw new ConfigurationErrorsException($"Cannot find type described by {type}");
                epHandlers.Add(t);
            }
            if(epHandlers.Count == 0) // Use all resource handlers in "this"
                epHandlers = typeof(SecurityUserResourceHandler).Assembly.ExportedTypes.Where(t=>!t.IsAbstract && !t.IsInterface && typeof(IResourceHandler).IsAssignableFrom(t)).ToList();
            // Configuration
            return new AmiConfiguration(caConfiguration, epOptions, epHandlers);
		}
	}
}