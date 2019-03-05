/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using MARC.Everest.Connectors;
using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Auditing;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Configuration;
using SanteDB.Messaging.FHIR.Handlers;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using SanteDB.Rest.Common;
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
    /// FHIR service behavior
    /// </summary>
    [ServiceBehavior(Name = "FHIR", InstanceMode = ServiceInstanceMode.PerCall)]
    public class FhirServiceBehavior : IFhirServiceContract
    {

        
        private TraceSource m_tracer = new TraceSource("SanteDB.Messaging.FHIR");

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
        public DomainResourceBase ReadResource(string resourceType, string id, string mimeType)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;
            try
            {

                // Setup outgoing content
                result = this.PerformRead(resourceType, id, null);
                String baseUri = RestOperationContext.Current.IncomingRequest.Url.AbsoluteUri;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}/_history/{1}", baseUri, result.Results[0].VersionId));
                return result.Results[0];
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Read resource with version
        /// </summary>
        public DomainResourceBase VReadResource(string resourceType, string id, string vid, string mimeType)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;
            try
            {
                // Setup outgoing content
                result = this.PerformRead(resourceType, id, vid);
                return result.Results[0];
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Update a resource
        /// </summary>
        public DomainResourceBase UpdateResource(string resourceType, string id, string mimeType, DomainResourceBase target)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;

            try
            {

                // Setup outgoing content/

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                result = handler.Update(id, target, TransactionMode.Commit);
                if (result == null || result.Results.Count == 0) // Create
                    throw new NotSupportedException("Update is not supported on non-existant resource");

                if (result == null || result.Outcome == ResultCode.Rejected)
                    throw new InvalidDataException("Resource structure is not valid");
                else if (result.Outcome == ResultCode.AcceptedNonConformant)
                    throw new ConstraintException("Resource not conformant");
                else if (result.Outcome == ResultCode.TypeNotAvailable)
                    throw new FileNotFoundException(String.Format("Resource {0} not found", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Update failed");

                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.Import, OutcomeIndicator.Success, id, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                String baseUri = RestOperationContext.Current.IncomingRequest.Url.AbsoluteUri;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}{1}/{2}/_history/{3}", baseUri, resourceType, result.Results[0].Id, result.Results[0].VersionId));
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Results[0].Timestamp);
                RestOperationContext.Current.OutgoingResponse.SetETag(result.Results[0].VersionId);

                return result.Results[0];

            }
            catch (Exception e)
            {
                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Update, AuditableObjectLifecycle.Amendment, EventIdentifierType.Import, OutcomeIndicator.EpicFail, id, null);
                throw;
            }
        }

        /// <summary>
        /// Delete a resource
        /// </summary>
        public DomainResourceBase DeleteResource(string resourceType, string id, string mimeType)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;

            try
            {

                // Setup outgoing content/
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                result = handler.Delete(id, TransactionMode.Commit);

                if (result == null || result.Outcome == ResultCode.Rejected)
                    throw new NotSupportedException();
                else if (result.Outcome == ResultCode.TypeNotAvailable)
                    throw new FileNotFoundException(String.Format("Resource {0} not found", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Delete failed");

                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.Import, OutcomeIndicator.Success, id, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());


                return null;

            }
            catch (Exception e)
            {
                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Delete, AuditableObjectLifecycle.LogicalDeletion, EventIdentifierType.Import, OutcomeIndicator.EpicFail, id, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                throw;
            }
        }

        /// <summary>
        /// Create a resource
        /// </summary>
        public DomainResourceBase CreateResource(string resourceType, string mimeType, DomainResourceBase target)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;

            try
            {

                // Setup outgoing content

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                result = handler.Create(target, TransactionMode.Commit);
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;

                if (result == null || result.Outcome == ResultCode.Rejected)
                    throw new InvalidDataException("Resource structure is not valid");
                else if (result.Outcome == ResultCode.AcceptedNonConformant)
                    throw new ConstraintException("Resource not conformant");
                else if (result.Outcome == ResultCode.TypeNotAvailable)
                    throw new FileNotFoundException(String.Format("Resource {0} not found", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Create failed");

                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.Import, OutcomeIndicator.Success, null, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                String baseUri = RestOperationContext.Current.IncomingRequest.Url.AbsoluteUri;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}{1}/{2}/_history/{3}", baseUri, resourceType, result.Results[0].Id, result.Results[0].VersionId));
                RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Results[0].Timestamp);
                RestOperationContext.Current.OutgoingResponse.SetETag(result.Results[0].VersionId);


                return result.Results[0];

            }
            catch (Exception e)
            {
                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Import, ActionType.Create, AuditableObjectLifecycle.Creation, EventIdentifierType.Import, OutcomeIndicator.EpicFail, null, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                throw;
            }
        }

        /// <summary>
        /// Validate a resource (really an update with debugging / non comit)
        /// </summary>
        public OperationOutcome ValidateResource(string resourceType, string id, DomainResourceBase target)
        {
            this.ThrowIfNotReady();

            FhirOperationResult result = null;
            try
            {

                // Setup outgoing content

                // Create or update?
                var handler = FhirResourceHandlerUtil.GetResourceHandler(resourceType);
                if (handler == null)
                    throw new FileNotFoundException(); // endpoint not found!

                result = handler.Update(id, target, TransactionMode.Rollback);
                if (result == null || result.Results.Count == 0) // Create
                {
                    result = handler.Create(target, TransactionMode.Rollback);
                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.Created;
                }

                if (result == null || result.Outcome == ResultCode.Rejected)
                    throw new InvalidDataException("Resource structure is not valid");
                else if (result.Outcome == ResultCode.AcceptedNonConformant)
                    throw new ConstraintException("Resource not conformant");
                else if (result.Outcome == ResultCode.TypeNotAvailable)
                    throw new FileNotFoundException(String.Format("Resource {0} not found", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Validate failed");

                // Return constraint
                return MessageUtil.CreateOutcomeResource(result);

            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Searches a resource from the client registry datastore 
        /// </summary>
        public Bundle SearchResource(string resourceType)
        {
            this.ThrowIfNotReady();

            // Stuff for auditing and exception handling
            List<IResultDetail> details = new List<IResultDetail>();
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

                if (result == null || result.Outcome == ResultCode.Rejected)
                    throw new InvalidDataException("Message was rejected");
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Query failed");

                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.Success, RestOperationContext.Current.IncomingRequest.Url.Query, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());
                // Create the Atom feed
                return MessageUtil.CreateBundle(result);

            }
            catch (Exception e)
            {
                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.EpicFail, RestOperationContext.Current.IncomingRequest.Url.Query, RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                throw;
            }

        }

        /// <summary>
        /// Get conformance
        /// </summary>
        public Conformance GetOptions()
        {
            this.ThrowIfNotReady();

            var retVal = ConformanceUtil.GetConformanceStatement();
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Location", String.Format("{0}Conformance/{1}/_history/{2}", RestOperationContext.Current.IncomingRequest.Url, retVal.Id, retVal.VersionId));
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
            RestOperationContext.Current.OutgoingResponse.Headers.Remove("Content-Disposition");
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", "filename=\"conformance.xml\"");
            return retVal;
        }

        /// <summary>
        /// Posting transaction is not supported
        /// </summary>
        public Bundle PostTransaction(Bundle feed)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a resource's history
        /// </summary>
        public Bundle GetResourceInstanceHistory(string resourceType, string id, string mimeType)
        {
            this.ThrowIfNotReady();

            FhirOperationResult readResult = null;
            try
            {

                readResult = this.PerformRead(resourceType, id, String.Empty);
                RestOperationContext.Current.OutgoingResponse.Headers.Remove("Content-Disposition");
                return MessageUtil.CreateBundle(readResult);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Not implemented result
        /// </summary>
        public Bundle GetResourceHistory(string resourceType, string mimeType)
        {
            this.ThrowIfNotReady();

            throw new NotSupportedException("For security reasons resource history is not supported");

        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public Bundle GetHistory(string mimeType)
        {
            this.ThrowIfNotReady();

            throw new NotSupportedException("For security reasons system history is not supported");
        }


        /// <summary>
        /// Perform a read against the underlying IFhirResourceHandler
        /// </summary>
        private FhirOperationResult PerformRead(string resourceType, string id, string vid)
        {
            this.ThrowIfNotReady();

            // Stuff for auditing and exception handling
            List<IResultDetail> details = new List<IResultDetail>();
            FhirOperationResult result = null;

            try
            {

                // Get query parameters
                var queryParameters = RestOperationContext.Current.IncomingRequest.QueryString;
                var resourceProcessor = FhirResourceHandlerUtil.GetResourceHandler(resourceType);

                if (resourceProcessor == null) // Unsupported resource
                    throw new FileNotFoundException("Specified resource type is not found");

                // TODO: Appropriately format response
                // Process incoming request
                result = resourceProcessor.Read(id, vid);

                if (result.Outcome == ResultCode.Rejected)
                    throw new InvalidDataException("Message was rejected");
                else if (result.Outcome == (ResultCode.NotAvailable | ResultCode.Rejected))
                    throw new FileLoadException(String.Format("Resource {0} is no longer available", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome == ResultCode.TypeNotAvailable ||
                    result.Results == null || result.Results.Count == 0)
                    throw new FileNotFoundException(String.Format("Resource {0} not found", RestOperationContext.Current.IncomingRequest.Url));
                else if (result.Outcome != ResultCode.Accepted)
                    throw new DataException("Read failed");

                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.Success, $"_id={id}&_versionId={vid}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                // Create the result
                if (result.Results != null && result.Results.Count > 0 )
                {
                    RestOperationContext.Current.OutgoingResponse.SetLastModified(result.Results[0].Timestamp);
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", String.Format("filename=\"{0}-{1}-{2}.xml\"", resourceType, result.Results[0].Id, result.Results[0].VersionId));
                    RestOperationContext.Current.OutgoingResponse.SetETag(result.Results[0].VersionId);
                }
                return result;

            }
            catch (Exception e)
            {
                AuditUtil.AuditDataAction<DomainResourceBase>(EventTypeCodes.Query, ActionType.Read, AuditableObjectLifecycle.Disclosure, EventIdentifierType.Export, OutcomeIndicator.EpicFail, $"_id={id}&_versionId={vid}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint.ToString(), result.Results.ToArray());

                throw;
            }
        }

        /// <summary>
        /// Get meta-data
        /// </summary>
        public Conformance GetMetaData()
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
        public DomainResourceBase CreateUpdateResource(string resourceType, string id, string mimeType, DomainResourceBase target)
        {
            return this.UpdateResource(resourceType, id, mimeType, target);
        }

        /// <summary>
        /// Alternate search
        /// </summary>
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
