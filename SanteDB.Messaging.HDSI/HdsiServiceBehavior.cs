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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using SanteDB.Core.Model;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Diagnostics;
using System.IO;
using SanteDB.Core.Model.Attributes;
using System.Xml.Serialization;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Entities;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Net;
using System.Data;
using System.Collections;
using SanteDB.Core.Security.Attribute;
using System.Security.Permissions;
using SanteDB.Core.Security;
using SanteDB.Messaging.HDSI.Util;
using SanteDB.Core.Model.Interfaces;
using MARC.Everest.Threading;
using System.Collections.Specialized;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Interop;
using SanteDB.Core;
using System.Data.Linq;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using System.ServiceModel.Channels;
using System.ComponentModel;
using RestSrvr.Attributes;
using SanteDB.Rest.HDSI;
using RestSrvr;
using SanteDB.Rest.Common;

namespace SanteDB.Messaging.HDSI.Wcf
{
    /// <summary>
    /// Data implementation
    /// </summary>
    [RestBehavior(Name = "HDSI")]
    [Description("Health Data Service Interface")]
    public class HdsiServiceBehavior : IHdsiServiceContract
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.HDSI");
       
        /// <summary>
        /// Ping the server
        /// </summary>
        public void Ping()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
        }

        /// <summary>
        /// Create the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            this.ThrowIfNotReady();

            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {

                    var retVal = handler.Create(body, false) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", retVal.Tag);
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address,  e.ToString()));
                throw;
            }
        }

        /// <summary>
        /// Create or update the specified object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Create(body, true) as IdentifiedData;
                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData Get(string resourceType, string id)
        {
            this.ThrowIfNotReady();

            try
            {


                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Get(Guid.Parse(id), Guid.Empty) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(id);

                    RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", retVal.Tag);
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("Last-Modified", retVal.ModifiedOn.DateTime.ToString("r"));

                    // HTTP IF headers?
                    if (RestOperationContext.Current.IncomingRequest.Headers["If-Modified-Since"] != null &&
                        retVal.ModifiedOn <= DateTime.Parse(RestOperationContext.Current.IncomingRequest.Headers["If-Modified-Since"]) ||
                        RestOperationContext.Current.IncomingRequest.Headers["If-None-Match"].Split(',')?.Any(o => retVal.Tag == o) == true)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                        return null;
                    }
                    
                    else if (RestOperationContext.Current.IncomingRequest.QueryString["_bundle"] == "true" ||
                        RestOperationContext.Current.IncomingRequest.QueryString["_all"] == "true")
                    {
                        retVal = retVal.GetLocked();
                        ObjectExpander.ExpandProperties(retVal, SanteDB.Core.Model.Query.NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query));
                        ObjectExpander.ExcludeProperties(retVal, SanteDB.Core.Model.Query.NameValueCollection.ParseQueryString(RestOperationContext.Current.IncomingRequest.Url.Query));
                        return Bundle.CreateBundle(retVal);
                    }
                    else
                    {
                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Gets a specific version of a resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Get(Guid.Parse(id), Guid.Parse(versionId)) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(id);



                    if (RestOperationContext.Current.IncomingRequest.QueryString["_bundle"] == "true")
                        return Bundle.CreateBundle(retVal);
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", retVal.Tag);

                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get the schema which defines this service
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public XmlSchema GetSchema(int schemaId)
        {
            this.ThrowIfNotReady();
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/model");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);


                foreach (var cls in HdsiMessageHandler.ResourceHandler.Handlers.Where(o=>o.Scope == typeof(IHdsiServiceContract)).Select(o => o.Type))
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/model"));

                if (schemaId > schemaCollection.Count)
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                    return null;
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
                    return schemaCollection[schemaId];
                }
            }
            catch (Exception e)
            {
//                RestOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                return null;
            }
        }

        /// <summary>
        /// Gets the recent history an object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData History(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = RestOperationContext.Current.IncomingRequest.QueryString["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query 
                    var retVal = handler.Get(Guid.Parse(id), Guid.Empty) as IVersionedEntity;
                    List<IVersionedEntity> histItm = new List<IVersionedEntity>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(id), retVal.PreviousVersionKey.Value) as IVersionedEntity;
                        if(retVal != null)
                            histItm.Add(retVal);
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                            break;

                    }

                    // Lock the item
                    return BundleUtil.CreateBundle(histItm.OfType<IdentifiedData>(), histItm.Count, 0, false);
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Perform a search on the specified resource type
        /// </summary>
        [PolicyPermissionAttribute(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {
                    String offset = RestOperationContext.Current.IncomingRequest.QueryString["_offset"],
                        count = RestOperationContext.Current.IncomingRequest.QueryString["_count"];

                    var query = RestOperationContext.Current.IncomingRequest.QueryString.ToQuery();

                    // Modified on?
                    if (RestOperationContext.Current.IncomingRequest.Headers["If-Modified-Since"] != null)
                        query.Add("modifiedOn", ">" + DateTime.Parse(RestOperationContext.Current.IncomingRequest.Headers["If-Modified-Since"]).ToString("o"));

                    // No obsoletion time?
                    if (typeof(BaseEntityData).IsAssignableFrom(handler.Type) && !query.ContainsKey("obsoletionTime"))
                        query.Add("obsoletionTime", "null");

                    int totalResults = 0;

                    // Lean mode
                    var lean = RestOperationContext.Current.IncomingRequest.QueryString["_lean"];
                    bool parsedLean = false;
                    bool.TryParse(lean, out parsedLean);


                    var retVal = handler.Query(query, Int32.Parse(offset ?? "0"), Int32.Parse(count ?? "100"), out totalResults).OfType<IdentifiedData>().Select(o=>o.GetLocked()).ToList();
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("Last-Modified", (retVal.OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now).ToString("r"));


                    // Last modification time and not modified conditions
                    if ((RestOperationContext.Current.IncomingRequest.Headers["If-Modified-Since"] != null ||
                        RestOperationContext.Current.IncomingRequest.Headers["If-None-Match"] != null) &&
                        totalResults == 0)
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 304;
                        return null;
                    }
                    else
                    {
                        if (query.ContainsKey("_all") || query.ContainsKey("_expand") || query.ContainsKey("_exclude"))
                        {
                            using (WaitThreadPool wtp = new WaitThreadPool())
                            {
                                foreach (var itm in retVal)
                                {
                                    wtp.QueueUserWorkItem((o) => {
                                        try
                                        {
                                            var i = o as IdentifiedData;
                                            ObjectExpander.ExpandProperties(i, query);
                                            ObjectExpander.ExcludeProperties(i, query);
                                        }
                                        catch(Exception e)
                                        {
                                            this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error setting properties: {0}", e);
                                        }
                                    }, itm);
                                }
                                wtp.WaitOne();
                            }
                        }

                       
                        return BundleUtil.CreateBundle(retVal, totalResults, Int32.Parse(offset ?? "0"), parsedLean);
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }


        /// <summary>
        /// Get the server's current time
        /// </summary>
        public DateTime Time()
        {
            this.ThrowIfNotReady();
            return DateTime.Now;
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {

                    var retVal = handler.Update(body) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", retVal.Tag);

                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

     
        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public IdentifiedData Delete(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
                if (handler != null)
                {

                    var retVal = handler.Obsolete(Guid.Parse(id)) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;

                    RestOperationContext.Current.OutgoingResponse.StatusCode = 201;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            RestOperationContext.Current.IncomingRequest.Url,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Perform the search but only return the headers
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public void HeadSearch(string resourceType)
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.IncomingRequest.QueryString.Add("_count", "1");
            this.Search(resourceType);
        }

        /// <summary>
        /// Get just the headers
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public void Head(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            this.Get(resourceType, id);
        }

        /// <summary>
        /// Perform a patch on the serviceo
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="id"></param>
        /// <param name="body"></param>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public void Patch(string resourceType, string id, Patch body)
        {
            this.ThrowIfNotReady();
            try
            {
                // Validate
                var match = RestOperationContext.Current.IncomingRequest.Headers["If-Match"];
                if (match == null)
                    throw new InvalidOperationException("Missing If-Match header");

                // Match bin
                var versionId = Guid.ParseExact(match, "N");

                // First we load
                var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);

                if (handler == null)
                    throw new FileNotFoundException(resourceType);

                // Next we get the current version
                var existing = handler.Get(Guid.Parse(id), Guid.Empty) as IdentifiedData;
                var force = Convert.ToBoolean(RestOperationContext.Current.IncomingRequest.Headers["X-Patch-Force"] ?? "false");

                if (existing == null)
                    throw new FileNotFoundException($"/{resourceType}/{id}/history/{versionId}");
                else if (existing.Tag != match && !force)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, -3049, "Object {0} ETAG is {1} but If-Match specified {2}", existing.Key, existing.Tag, match);
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 409;
                    RestOperationContext.Current.OutgoingResponse.StatusDescription = ApplicationContext.Current.GetLocaleString("DBPE002");
                    return;
                }
                else if (body == null)
                    throw new ArgumentNullException(nameof(body));
                else
                {
                    // Force load all properties for existing
                    var applied = ApplicationContext.Current.GetService<IPatchService>().Patch(body, existing, force);
                    var data = handler.Update(applied) as IdentifiedData;
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 204;
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", data.Tag);
                    RestOperationContext.Current.OutgoingResponse.AppendHeader("Last-Modified", applied.ModifiedOn.DateTime.ToString("r"));
                    var versioned = (data as IVersionedEntity)?.VersionKey;
                    if (versioned != null)
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                resourceType,
                                id,
                                versioned));
                    else
                        RestOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                                RestOperationContext.Current.IncomingRequest.Url,
                                resourceType,
                                id));
                }
            }
            catch(PatchAssertionException e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Warning, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address,  e.ToString()));
                throw;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Gets the specifieed patch id
        /// </summary>
        public Patch GetPatch(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get options
        /// </summary>
        public ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            try
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = 200;
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");
                if (ApplicationContext.Current.GetService<IPatchService>() != null)
                    RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+oiz-patch");

                // Service options
                var retVal = new ServiceOptions()
                {
                    InterfaceVersion = "1.0.0.0",
                    Services = new List<ServiceResourceOptions>()
                    {
                        new ServiceResourceOptions()
                        {
                            ResourceName = null
                        },
                        new ServiceResourceOptions()
                        {
                            ResourceName = "time",
                            Capabilities = ResourceCapability.Get
                        }
                    }
                };

                // Get the resources which are supported
                foreach (var itm in HdsiMessageHandler.ResourceHandler.Handlers)
                {
                    var svc = new ServiceResourceOptions()
                    {
                        ResourceName = itm.ResourceName,
                        Capabilities = itm.Capabilities
                    };
                    if (ApplicationContext.Current.GetService<IPatchService>() != null)
                        svc.Capabilities |= ResourceCapability.Patch;
                    retVal.Services.Add(svc);
                }

                return retVal;
            }
            catch (Exception e)
            {
                var remoteEndpoint = RestOperationContext.Current.IncomingRequest.RemoteEndPoint;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;
            }
        }


        /// <summary>
        /// Throw if the service is not ready
        /// </summary>
        public void ThrowIfNotReady()
        {
            if (!ApplicationContext.Current.IsRunning)
                throw new DomainStateException();
        }

        /// <summary>
        /// Options resource
        /// </summary>
        public ServiceResourceOptions ResourceOptions(string resourceType)
        {

            var handler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
            else
                return new ServiceResourceOptions(resourceType, handler.Capabilities);
        }
    }
}
