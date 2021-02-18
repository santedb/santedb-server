/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
using Hl7.Fhir.Model;
using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Auditing;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Configuration;
using SanteDB.Messaging.FHIR.Handlers;
using SanteDB.Messaging.FHIR.Util;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Rest
{
    /// <summary>
    /// HL7 Fast Health Interoperability Resources (FHIR) R3
    /// </summary>
    /// <remarks>SanteSB Server implementation of the HL7 FHIR R3 Contract</remarks>
    [ServiceBehavior(Name = "FHIR", InstanceMode = ServiceInstanceMode.PerCall)]
    public class FhirServiceBehavior : IFhirServiceContract
    {

        
        private Tracer m_tracer = new Tracer(FhirConstants.TraceSourceName);

        #region IFhirServiceContract Members

        /// <summary>
        /// Get schema
        /// </summary>
        public XmlSchema GetSchema(int schemaId)
        {
            this.ThrowIfNotReady();

            XmlSchemas schemaCollection = new XmlSchemas();

            XmlReflectionImporter importer = new XmlReflectionImporter("http://hl7.org/fhir");
            XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

            foreach (var cls in typeof(FhirServiceBehavior).Assembly.GetTypes().Where(o => o.GetCustomAttribute<XmlRootAttribute>() != null && !o.IsGenericTypeDefinition))
                exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://hl7.org/fhir"));

            return schemaCollection[schemaId];
        }

        /// <summary>
        /// Get the index
        /// </summary>
        public Stream Index()
        {
            this.ThrowIfNotReady();

            try
            {
                RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", "filename=\"index.html\"");
                RestOperationContext.Current.OutgoingResponse.SetLastModified(DateTime.UtcNow);
                FhirServiceConfigurationSection config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<FhirServiceConfigurationSection>();
                if (!String.IsNullOrEmpty(config.LandingPage))
                {
                    using (var fs = File.OpenRead(config.LandingPage))
                    {
                        MemoryStream ms = new MemoryStream();
                        int br = 1024;
                        byte[] buffer = new byte[1024];
                        while (br == 1024)
                        {
                            br = fs.Read(buffer, 0, 1024);
                            ms.Write(buffer, 0, br);
                        }
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                }
                else
                    return typeof(FhirServiceBehavior).Assembly.GetManifestResourceStream("SanteDB.Messaging.FHIR.index.htm");
            }
            catch (IOException)
            {
                throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// Read a reasource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public Resource ReadResource(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {

                // Setup outgoing content
                var result = this.PerformRead(resourceType, id, null);
                String baseUri = RestOperationContext.Current.IncomingRequest.Url.AbsoluteUri;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}/_history/{1}", baseUri, result.VersionId));
                return result;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error reading FHIR resource {0}({1}): {2}", resourceType, id, e);
                throw;
            }
        }

        /// <summary>
        /// Read resource with version
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public Resource VReadResource(string resourceType, string id, string vid)
        {
            this.ThrowIfNotReady();

            try
            {
                // Setup outgoing content
                var result = this.PerformRead(resourceType, id, vid);
                return result;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error vreading FHIR resource {0}({1},{2}): {3}", resourceType, id, vid, e);
                throw;
            }
        }

        /// <summary>
        /// Update a resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public Resource UpdateResource(string resourceType, string id, Resource target)
        {
            this.ThrowIfNotReady();

            try
            {

                // Setup outgoing content/

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                var result = handler.Update(id, target, TransactionMode.Commit);
               
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.Import, OutcomeIndicator.Success, id, result);

                String baseUri = MessageUtil.GetBaseUri();
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}/{1}/_history/{2}", baseUri, result.Id, result.VersionId));
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Meta.LastUpdated.Value.DateTime);
                RestOperationContext.Current.OutgoingResponse.SetETag($"W/\"{result.VersionId}\"");

                return result;

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error updating FHIR resource {0}({1}): {2}", resourceType, id, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.Import, OutcomeIndicator.EpicFail, id);
                throw;
            }
        }

        /// <summary>
        /// Delete a resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public Resource DeleteResource(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {

                // Setup outgoing content/
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                var result = handler.Delete(id, TransactionMode.Commit);

                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.Import, OutcomeIndicator.Success, id, result);
                return null;

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error deleting FHIR resource {0}({1}): {2}", resourceType, id, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.Import, OutcomeIndicator.EpicFail, id);
                throw;
            }
        }

        /// <summary>
        /// Create a resource
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public Resource CreateResource(string resourceType, Resource target)
        {
            this.ThrowIfNotReady();
            try
            {

                // Setup outgoing content

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                var result = handler.Create(target, TransactionMode.Commit);
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;


                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.Import, OutcomeIndicator.Success, null, result);

                String baseUri = MessageUtil.GetBaseUri();
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}/{1}/{2}/_history/{3}", baseUri, resourceType, result.Id, result.VersionId));
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Meta.LastUpdated.Value.DateTime);
                RestOperationContext.Current.OutgoingResponse.SetETag($"W/\"{result.VersionId}\"");


                return result;

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error creating FHIR resource {0}: {1}", resourceType, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Import, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.Import, OutcomeIndicator.EpicFail, null);
                throw;
            }
        }

        /// <summary>
        /// Validate a resource (really an update with debugging / non comit)
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public OperationOutcome ValidateResource(string resourceType, string id, Resource target)
        {
            this.ThrowIfNotReady();
            try
            {

                // Setup outgoing content

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                var result = handler.Update(id, target, TransactionMode.Rollback);
                if (result == null) // Create
                {
                    result = handler.Create(target, TransactionMode.Rollback);
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                }

                // Return constraint
                return new OperationOutcome()
                {
                    Issue = new List<OperationOutcome.IssueComponent>()
                    {
                        new OperationOutcome.IssueComponent() { Severity = OperationOutcome.IssueSeverity.Information, Diagnostics = "Resource validated" }
                    }
                };
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error validating FHIR resource: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Searches a resource from the client registry datastore 
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public Bundle SearchResource(string resourceType)
        {
            this.ThrowIfNotReady();

            // Stuff for auditing and exception handling
            FhirQueryResult result = null;

            try
            {

                // Get query parameters
                var queryParameters = RestOperationContext.Current.IncomingRequest.Url;
                var resourceProcessor = FhirResourceHandlerUtil.GetResourceHandler(resourceType);

                // Setup outgoing content
                RestOperationContext.Current.OutgoingResponse.SetLastModified(DateTime.Now);

                if (resourceProcessor == null) // Unsupported resource
                    throw new FileNotFoundException();

                // TODO: Appropriately format response
                // Process incoming request
                result = resourceProcessor.Query(RestOperationContext.Current.IncomingRequest.QueryString);
                
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.Success, RestOperationContext.Current.IncomingRequest.Url.Query, result.Results.ToArray());
                // Create the Atom feed
                return MessageUtil.CreateBundle(result);

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error searching FHIR resource {0}: {1}", resourceType, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.EpicFail, RestOperationContext.Current.IncomingRequest.Url.Query,  result.Results.ToArray());
                throw;
            }

        }

        /// <summary>
        /// Get conformance
        /// </summary>
        public CapabilityStatement GetOptions()
        {
            this.ThrowIfNotReady();
            var retVal = ConformanceUtil.GetConformanceStatement();
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}metadata", RestOperationContext.Current.IncomingRequest.Url));
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
            return retVal;
        }

        /// <summary>
        /// Posting transaction is not supported
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public Bundle PostTransaction(Bundle feed)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a resource's history
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public Bundle GetResourceInstanceHistory(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            // Stuff for auditing and exception handling
            try
            {

                // Get query parameters
                var queryParameters = RestOperationContext.Current.IncomingRequest.QueryString;
                var resourceProcessor = FhirResourceHandlerUtil.GetResourceHandler(resourceType);

                if (resourceProcessor == null) // Unsupported resource
                    throw new FileNotFoundException("Specified resource type is not found");

                // TODO: Appropriately format response
                // Process incoming request
                var result = resourceProcessor.History(id);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.Success, $"_id={id}", result.Results.ToArray());

                // Create the result
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Results[0].Meta.LastUpdated.Value.DateTime);
                return MessageUtil.CreateBundle(result);

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error getting FHIR resource history {0}({1}): {2}", resourceType, id, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.EpicFail, $"_id={id}", null);
                throw;
            }
        }

        /// <summary>
        /// Not implemented result
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public Bundle GetResourceHistory(string resourceType)
        {
            this.ThrowIfNotReady();

            throw new NotSupportedException("For security reasons resource history is not supported");

        }

        /// <summary>
        /// Not implemented
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public Bundle GetHistory(string mimeType)
        {
            this.ThrowIfNotReady();

            throw new NotSupportedException("For security reasons system history is not supported");
        }


        /// <summary>
        /// Perform a read against the underlying IFhirResourceHandler
        /// </summary>
        private Resource PerformRead(string resourceType, string id, string vid)
        {
            this.ThrowIfNotReady();

            // Stuff for auditing and exception handling

            try
            {

                // Get query parameters
                var queryParameters = RestOperationContext.Current.IncomingRequest.QueryString;
                var resourceProcessor = FhirResourceHandlerUtil.GetResourceHandler(resourceType);

                if (resourceProcessor == null) // Unsupported resource
                    throw new FileNotFoundException("Specified resource type is not found");

                // TODO: Appropriately format response
                // Process incoming request
                var result = resourceProcessor.Read(id, vid);

                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.Success, $"_id={id}&_versionId={vid}", result);

                // Create the result
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Meta.LastUpdated.Value.DateTime);
                RestOperationContext.Current.OutgoingResponse.SetETag($"W/\"{result.VersionId}\"");
                
                return result;

            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error reading FHIR resource {0}({1},{2}): {3}", resourceType, id, vid, e);
                AuditUtil.AuditDataAction<Resource>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.EpicFail, $"_id={id}&_versionId={vid}",  null);
                throw;
            }
        }

        /// <summary>
        /// Get meta-data
        /// </summary>
        public CapabilityStatement GetMetaData()
        {
            return this.GetOptions();
        }

        /// <summary>
        /// Get the current time
        /// </summary>
        public DateTime Time()
        {
            return DateTime.Now;
        }

        #endregion

        /// <summary>
        /// Create or update
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public Resource CreateUpdateResource(string resourceType, string id, Resource target)
        {
            return this.UpdateResource(resourceType, id, target);
        }

        /// <summary>
        /// Alternate search
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public Bundle SearchResourceAlt(string resourceType)
        {
            return this.SearchResource(resourceType);
        }

        /// <summary>
        /// Throws an exception if the service is not yet ready
        /// </summary>
        private void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
                throw new DomainStateException();

        }
    }

}
