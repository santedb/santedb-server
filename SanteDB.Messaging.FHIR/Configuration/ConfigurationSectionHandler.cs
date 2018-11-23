using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using MARC.HI.EHRS.SVC.Core.Data;

namespace SanteDB.Messaging.FHIR.Configuration
{
    /// <summary>
    /// Configuration section handler for FHIR
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        #region IConfigurationSectionHandler Members

        /// <summary>
        /// Create the configuration object
        /// </summary>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {

            // Section
            XmlElement serviceElement = section.SelectSingleNode("./*[local-name() = 'service']") as XmlElement;
            XmlNodeList resourceElements = section.SelectNodes("./*[local-name()= 'resourceProcessors']/*[local-name() = 'add']"),
                actionMap = section.SelectNodes("./*[local-name() = 'actionMap']/*[local-name() = 'add']"),
                corsConfig = section.SelectNodes("./*[local-name() = 'cors']/*[local-name() = 'add']");
            XmlAttribute baseUri = section.SelectSingleNode("./*[local-name() = 'resourceProcessors']/@baseUrl") as XmlAttribute;

            string wcfServiceName = String.Empty,
                landingPage = String.Empty;
            
            if (serviceElement != null)
            {
                XmlAttribute serviceName = serviceElement.Attributes["wcfServiceName"],
                    landingPageNode = serviceElement.Attributes["landingPage"];
                if (serviceName != null)
                    wcfServiceName = serviceName.Value;
                else
                    throw new ConfigurationErrorsException("Missing wcfServiceName attribute", serviceElement);
                if (landingPageNode != null)
                    landingPage = landingPageNode.Value;
            }
            else
                throw new ConfigurationErrorsException("Missing serviceElement", section);

            var retVal = new FhirServiceConfiguration(wcfServiceName, landingPage, baseUri == null ? null : new Uri(baseUri.Value));

            // Add instructions
            foreach (XmlElement addInstruction in resourceElements)
            {
                if (addInstruction.Attributes["type"] == null)
                    throw new ConfigurationErrorsException("add instruction missing @type attribute");
                Type tType = Type.GetType(addInstruction.Attributes["type"].Value);
                if (tType == null)
                    throw new ConfigurationErrorsException(String.Format("Could not find type described by '{0}'", addInstruction.Attributes["type"].Value));
                retVal.ResourceHandlers.Add(tType);
            }

            foreach(XmlElement cors in corsConfig)
            {
                FhirCorsConfiguration config = new FhirCorsConfiguration()
                {
                    Domain = cors.Attributes["domain"]?.Value,
                    Actions = cors.SelectNodes("./*[local-name() = 'action']/text()").OfType<XmlText>().Select(o=>o.Value).ToList(),
                    Headers = cors.SelectNodes("./*[local-name() = 'header']/text()").OfType<XmlText>().Select(o => o.Value).ToList()
                };
                retVal.CorsConfiguration.Add(cors.Attributes["resource"]?.Value ?? "*", config);
            }

            foreach (XmlElement mapInstruction in actionMap)
            {
                String resourceName = String.Empty,
                    resourceAction = String.Empty,
                    eventTypeCode = String.Empty,
                    eventTypeCodeSystem = String.Empty,
                    eventTypeCodeName = String.Empty;

                if (mapInstruction.Attributes["resource"] != null)
                    resourceName = mapInstruction.Attributes["resource"].Value;
                if (mapInstruction.Attributes["action"] != null)
                    resourceAction = mapInstruction.Attributes["action"].Value;
                if (mapInstruction.Attributes["code"] != null)
                    eventTypeCode = mapInstruction.Attributes["code"].Value;
                if (mapInstruction.Attributes["codeSystem"] != null)
                    eventTypeCodeSystem = mapInstruction.Attributes["codeSystem"].Value;
                if (mapInstruction.Attributes["displayName"] != null)
                    eventTypeCodeName = mapInstruction.Attributes["displayName"].Value;

                retVal.ActionMap.Add(String.Format("{0} {1}", resourceAction, resourceName), new CodeValue(
                    eventTypeCode, eventTypeCodeSystem) { DisplayName = eventTypeCodeName });
            }
            return retVal;
        }

        #endregion
    }
}
