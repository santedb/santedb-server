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
 * Date: 2018-11-23
 */
using RestSrvr.Attributes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace SanteDB.Messaging.FHIR.Rest
{
    /// <summary>
    /// FHIR Service Contract
    /// </summary>
    [ServiceContract(Name = "FHIR")]
    [ServiceKnownResource(typeof(Patient))]
    [ServiceKnownResource(typeof(Organization))]
    [ServiceKnownResource(typeof(Picture))]
    [ServiceKnownResource(typeof(Practitioner))]
    [ServiceKnownResource(typeof(OperationOutcome))]
    [ServiceKnownResource(typeof(ValueSet))]
    [ServiceKnownResource(typeof(StructureDefinition))]
    [ServiceKnownResource(typeof(Bundle))]
    [ServiceKnownResource(typeof(Immunization))]
    [ServiceKnownResource(typeof(ImmunizationRecommendation))]
    [ServiceKnownResource(typeof(Conformance))]
    [ServiceKnownResource(typeof(RelatedPerson))]
    [ServiceKnownResource(typeof(Encounter))]
    [ServiceKnownResource(typeof(Condition))]
    [ServiceKnownResource(typeof(AdverseEvent))]
    [ServiceKnownResource(typeof(MedicationAdministration))]
    [ServiceKnownResource(typeof(Location))]
    [ServiceKnownResource(typeof(AllergyIntolerance))]
    public interface IFhirServiceContract
    {

        /// <summary>
        /// Get index page
        /// </summary>
        [Get("/")]
        Stream Index();

        /// <summary>
        /// Get the schema
        /// </summary>
        [Get("/?xsd={schemaId}")]
        XmlSchema GetSchema(int schemaId);

        /// <summary>
        /// Gets the current time on the service
        /// </summary>
        /// <returns></returns>
        [Get("/time")]
        DateTime Time();

        /// <summary>
        /// Read a resource
        /// </summary>
        [Get("/{resourceType}/{id}?_format={mimeType}")]
        DomainResourceBase ReadResource(string resourceType, string id, string mimeType);

        /// <summary>
        /// Version read a resource
        /// </summary>
        [Get("/{resourceType}/{id}/_history/{vid}?_format={mimeType}")]
        DomainResourceBase VReadResource(string resourceType, string id, string vid, string mimeType);

        /// <summary>
        /// Update a resource
        /// </summary>
        [RestInvoke(UriTemplate = "/{resourceType}/{id}?_format={mimeType}", Method = "PUT")]
        DomainResourceBase UpdateResource(string resourceType, string id, string mimeType, DomainResourceBase target);

        /// <summary>
        /// Delete a resource
        /// </summary>
        [RestInvoke(UriTemplate = "/{resourceType}/{id}?_format={mimeType}", Method = "DELETE")]
        DomainResourceBase DeleteResource(string resourceType, string id, string mimeType);

        /// <summary>
        /// Create a resource
        /// </summary>
        [RestInvoke(UriTemplate = "/{resourceType}?_format={mimeType}", Method = "POST")]
        DomainResourceBase CreateResource(string resourceType, string mimeType, DomainResourceBase target);

        /// <summary>
        /// Create a resource
        /// </summary>
        [RestInvoke(UriTemplate = "/{resourceType}/{id}?_format={mimeType}", Method = "POST")]
        DomainResourceBase CreateUpdateResource(string resourceType, string id, string mimeType, DomainResourceBase target);

        /// <summary>
        /// Validate a resource
        /// </summary>
        [RestInvoke(UriTemplate = "/{resourceType}/_validate/{id}", Method = "POST")]
        OperationOutcome ValidateResource(string resourceType, string id, DomainResourceBase target);

        /// <summary>
        /// Version read a resource
        /// </summary>
        [Get("/{resourceType}")]
        Bundle SearchResource(string resourceType);


        /// <summary>
        /// Version read a resource
        /// </summary>
        [Get("/{resourceType}/_search")]
        Bundle SearchResourceAlt(string resourceType);

        /// <summary>
        /// Options for this service
        /// </summary>
        [RestInvoke(UriTemplate = "/", Method = "OPTIONS")]
        Conformance GetOptions();

        /// <summary>
        /// Options for this service
        /// </summary>
        [RestInvoke(UriTemplate = "/metadata", Method = "GET")]
        Conformance GetMetaData();

        /// <summary>
        /// Post a transaction
        /// </summary>
        [RestInvoke(UriTemplate = "/", Method = "POST")]
        Bundle PostTransaction(Bundle feed);

        /// <summary>
        /// Get history
        /// </summary>
        [Get("/{resourceType}/{id}/_history?_format={mimeType}")]
        Bundle GetResourceInstanceHistory(string resourceType, string id, string mimeType);

        /// <summary>
        /// Get history
        /// </summary>
        [Get("/{resourceType}/_history?_format={mimeType}")]
        Bundle GetResourceHistory(string resourceType, string mimeType);

        /// <summary>
        /// Get history for all
        /// </summary>
        [Get("/_history?_format={mimeType}")]
        Bundle GetHistory(string mimeType);

    }

}
