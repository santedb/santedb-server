using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using SanteDB.Core.Model;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Messaging.Metadata.Composer
{

    /// <summary>
    /// Documentation element type
    /// </summary>
    public enum MetaDataElementType
    {
        Summary,
        Remarks,
        Example
    }

    /// <summary>
    /// Utility for managing documentation composer
    /// </summary>
    public static class MetadataComposerUtil
    {
        /// <summary>
        /// Trace source name
        /// </summary>
        private static Tracer s_traceSource = new Tracer(MetadataConstants.TraceSourceName);

        // Composers
        private static Dictionary<String, IMetadataComposer> s_composers = null;

        /// <summary>
        /// Documentation files
        /// </summary>
        private static Dictionary<Assembly, XmlDocument> s_documentationFiles = new Dictionary<Assembly, XmlDocument>();

        // Documentation cache
        private static Dictionary<String, String> s_documentationCache = new Dictionary<string, string>();

        /// <summary>
        /// Get the composer by format name
        /// </summary>
        /// <param name="name">The format that the composer creates</param>
        public static IMetadataComposer GetComposer(String name)
        {
            if(s_composers == null)
            {
                s_composers = ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes()
                    .Where(t => typeof(IMetadataComposer).IsAssignableFrom(t) && !t.IsGenericTypeDefinition && !t.IsAbstract && !t.IsInterface)
                    .Select(t => Activator.CreateInstance(t) as IMetadataComposer)
                    .ToDictionary(o => o.Name, o => o);
            }

            IMetadataComposer retVal = null;
            if (!s_composers.TryGetValue(name, out retVal))
                throw new KeyNotFoundException($"Composer {name} not found");
            else
                return retVal;
        }

        /// <summary>
        /// Get element documentation
        /// </summary>
        public static string GetElementDocumentation(Type type, MetaDataElementType docType = MetaDataElementType.Summary)
        {
            var docName = $"T:{type.FullName}";
            var cacheName = $"{docName}@{docType}";
            var retVal = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (String.IsNullOrEmpty(retVal) && !s_documentationCache.TryGetValue(cacheName, out retVal)) // Attempt to load the documentation from disk 
            {
                retVal = GetElementDocumentation(type.Assembly, docName, docType.ToString().ToLower());
                lock (s_documentationCache)
                    if (!s_documentationCache.ContainsKey(cacheName))
                        s_documentationCache.Add(cacheName, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Get element documentation
        /// </summary>
        public static string GetElementDocumentation(PropertyInfo property, MetaDataElementType docType = MetaDataElementType.Summary)
        {
            var declType = property.DeclaringType.FullName;
            if(property.DeclaringType.IsGenericType)
            {
                declType = declType.Substring(0, declType.IndexOf("["));
            }
            var docName = $"P:{declType}.{property.Name}";
            var cacheName = $"{docName}@{docType}";
            var retVal = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (String.IsNullOrEmpty(retVal) && !s_documentationCache.TryGetValue(cacheName, out retVal)) // Attempt to load the documentation from disk 
            {
                retVal = GetElementDocumentation(property.DeclaringType.Assembly, docName, docType.ToString().ToLower());
                lock (s_documentationCache)
                    if (!s_documentationCache.ContainsKey(cacheName))
                        s_documentationCache.Add(cacheName, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Get the service capabilities
        /// </summary>
        public static ServiceEndpointCapabilities GetServiceCapabilities(ServiceEndpointType endpointType)
        {
            var svc = ApplicationServiceContext.Current.GetService<IServiceManager>().GetServices().OfType<IApiEndpointProvider>().FirstOrDefault(o => o.ApiType == endpointType);
            return svc.Capabilities;
        }

        /// <summary>
        /// Create a schema reference
        /// </summary>
        public static String CreateSchemaReference(Type type)
        {
            String schemaName = type.GetCustomAttribute<XmlTypeAttribute>()?.TypeName;
            if (String.IsNullOrEmpty(schemaName))
                schemaName = type.GetCustomAttribute<JsonObjectAttribute>()?.Id;
            if (String.IsNullOrEmpty(schemaName))
                schemaName = type.StripNullable().Name;
            return schemaName;
        }

        /// <summary>
        /// Get the specified parameter documentation
        /// </summary>
        public static string GetElementDocumentation(MethodInfo method, ParameterInfo parameter)
        {
            var docName = $"M:{method.DeclaringType.FullName}.{method.Name}({String.Join(",", method.GetParameters().Select(o => o.ParameterType.FullName))})";
            var cacheName = $"{docName}/{parameter.Name}";
            var retVal = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (String.IsNullOrEmpty(retVal) && !s_documentationCache.TryGetValue(cacheName, out retVal)) // Attempt to load the documentation from disk 
            {
                retVal = GetElementDocumentation(method.DeclaringType.Assembly, docName, $"param[@name='{parameter.Name}']");
                lock (s_documentationCache)
                    if (!s_documentationCache.ContainsKey(cacheName))
                        s_documentationCache.Add(cacheName, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Get element documentation
        /// </summary>
        public static string GetElementDocumentation(MethodInfo method, MetaDataElementType docType = MetaDataElementType.Summary)
        {
            var docName = $"M:{method.DeclaringType.FullName}.{method.Name}({String.Join(",", method.GetParameters().Select(o=>o.ParameterType.FullName))})";
            var cacheName = $"{docName}@{docType}";
            var retVal = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (String.IsNullOrEmpty(retVal) && !s_documentationCache.TryGetValue(cacheName, out retVal)) // Attempt to load the documentation from disk 
            {
                retVal = GetElementDocumentation(method.DeclaringType.Assembly, docName, docType.ToString().ToLower());
                lock (s_documentationCache)
                    if (!s_documentationCache.ContainsKey(cacheName))
                        s_documentationCache.Add(cacheName, retVal);
            }
            return retVal;

        }


        /// <summary>
        /// Get the documentation summary
        /// </summary>
        public static string GetElementDocumentation(Assembly assembly, String docKey, String docPath)
        {
            XmlDocument documentation = null;
            // Try to get the cached documentation
            if(!s_documentationFiles.TryGetValue(assembly, out documentation))
            {
                var docFile = Path.ChangeExtension(assembly.Location, "xml");
                if (!File.Exists(docFile))
                    return null;

                try
                {
                    documentation = new XmlDocument();
                    documentation.Load(docFile);
                    lock (s_documentationFiles)
                        if (!s_documentationFiles.ContainsKey(assembly))
                            s_documentationFiles.Add(assembly, documentation);
                }
                catch (Exception e)
                {
                    s_traceSource.TraceEvent(EventLevel.Error,  "Could not load documentation file {0}: {1}", docFile, e);
                    throw;
                }
            }

            // Now, get summary
            lock(documentation)
                return documentation.SelectSingleNode($"/doc/members/member[@name='{docKey}']/{docPath}/text()")?.Value?.Trim();
        }

        /// <summary>
        /// Converts an HTTP verb to a capability
        /// </summary>
        public static ResourceCapabilityType VerbToCapability(string verb, int argl)
        {
            switch(verb.ToLower())
            {
                case "get":
                    return argl == 0 ? ResourceCapabilityType.Search : ResourceCapabilityType.Get;
                case "post":
                    return ResourceCapabilityType.Create;
                case "put":
                    return ResourceCapabilityType.Update;
                case "delete":
                    return ResourceCapabilityType.Delete;
                case "patch":
                    return ResourceCapabilityType.Patch;
                default:
                    return ResourceCapabilityType.None;
            }
        }

        /// <summary>
        /// Resolves the service from name
        /// </summary>
        public static ServiceEndpointOptions ResolveService(String serviceName)
        {
            // Look for external services?
            IEnumerable<ServiceEndpointOptions> services = ApplicationServiceContext.Current.GetService<MetadataMessageHandler>().Configuration.Services;
            if (services.Count() == 0)
                services = ApplicationServiceContext.Current.GetService<IServiceManager>().GetServices().OfType<IApiEndpointProvider>().Select(o => new ServiceEndpointOptions(o));

            var serviceEndpoint = typeof(ServiceEndpointType).GetFields().FirstOrDefault(o => o.GetCustomAttribute<XmlEnumAttribute>()?.Name == serviceName)?.Name;
            if (!String.IsNullOrEmpty(serviceEndpoint))
            {
                ServiceEndpointType serviceEndpointType = (ServiceEndpointType)Enum.Parse(typeof(ServiceEndpointType), serviceEndpoint);
                return services.FirstOrDefault(o => o.ServiceType == serviceEndpointType);
            }
            else
                return null;
        }
    }
}
