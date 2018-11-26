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
using MARC.Everest.Connectors;
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Configuration;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Resources.Attributes;
using System;
using System.Collections.Generic;
using System.IO;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Represents a series of message processing utilities
    /// </summary>
    public static class MessageUtil
    {

        // Escape characters
        private static readonly Dictionary<String, String> s_escapeChars = new Dictionary<string, string>()
        {
            { "\\,", "\\#002C" },
            { "\\$", "\\#0024" },
            { "\\|", "\\#007C" },
            { "\\\\", "\\#005C" },
        };

        /// <summary>
        /// Escape a string
        /// </summary>
        public static String Escape(String str)
        {
            string retVal = str;
            foreach (var itm in s_escapeChars)
                retVal = retVal.Replace(itm.Key, itm.Value);
            return retVal;
        }

        /// <summary>
        /// Un-escape a string
        /// </summary>
        public static string UnEscape(String str)
        {
            string retVal = str;
            foreach (var itm in s_escapeChars)
                retVal = retVal.Replace(itm.Value, itm.Key);
            return retVal;
        }

        /// <summary>
        /// Create a feed
        /// </summary>
        internal static Bundle CreateBundle(FhirOperationResult result)
        {

            Bundle retVal = new Bundle();
            FhirQueryResult queryResult = result as FhirQueryResult;

            int pageNo = queryResult == null || queryResult.Query.Quantity == 0 ? 0 : queryResult.Query.Start / queryResult.Query.Quantity,
                nPages = queryResult == null || queryResult.Query.Quantity == 0 ? 1 : (queryResult.TotalResults / queryResult.Query.Quantity);

            retVal.Type = BundleType.SearchResults;

            retVal.Id = String.Format("urn:uuid:{0}", Guid.NewGuid());

            // Make the Self uri
            String baseUri = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<FhirServiceConfigurationSection>()?.ResourceBaseUri ?? RestOperationContext.Current.IncomingRequest.Url.AbsoluteUri;
            if (baseUri.Contains("?"))
                baseUri = baseUri.Substring(0, baseUri.IndexOf("?") + 1);
            else
                baseUri += "?";

            // Self uri
            if (queryResult != null)
            {
                for (int i = 0; i < queryResult.Query.ActualParameters.Count; i++)
                    foreach (var itm in queryResult.Query.ActualParameters.GetValues(i))
                        switch(queryResult.Query.ActualParameters.GetKey(i))
                        {
                            case "_stateid":
                            case "_page":
                            case "_count":
                                break;
                            default:
                                baseUri += string.Format("{0}={1}&", queryResult.Query.ActualParameters.GetKey(i), itm);
                                break;
                        }

                if(!baseUri.Contains("_stateid=") && queryResult.Query.QueryId != Guid.Empty)
                    baseUri += String.Format("_stateid={0}&", queryResult.Query.QueryId);
            }

            // Format
            string format = RestOperationContext.Current.IncomingRequest.QueryString["_format"];
            if (String.IsNullOrEmpty(format))
                format = "xml";
            else if (format == "application/fhir+xml")
                format = "xml";
            else if (format == "application/fhir+json")
                format = "json";

            if (!baseUri.Contains("_format"))
                baseUri += String.Format("_format={0}&", format);

            // Self URI
            if (queryResult != null && queryResult.TotalResults > queryResult.Results.Count)
            {
                retVal.Link.Add(new BundleLink(new Uri(String.Format("{0}_page={1}&_count={2}", baseUri, pageNo, queryResult?.Query.Quantity ?? 100)), "self"));
                if (pageNo > 0)
                {
                    retVal.Link.Add(new BundleLink(new Uri(String.Format("{0}_page=0&_count={1}", baseUri, queryResult?.Query.Quantity ?? 100)), "first"));
                    retVal.Link.Add(new BundleLink(new Uri(String.Format("{0}_page={1}&_count={2}", baseUri, pageNo - 1, queryResult?.Query.Quantity ?? 100)), "previous"));
                }
                if (pageNo <= nPages)
                {
                    retVal.Link.Add(new BundleLink(new Uri(String.Format("{0}_page={1}&_count={2}", baseUri, pageNo + 1, queryResult?.Query.Quantity ?? 100)), "next"));
                    retVal.Link.Add(new BundleLink(new Uri(String.Format("{0}_page={1}&_count={2}", baseUri, nPages + 1, queryResult?.Query.Quantity ?? 100)), "last"));
                }
            }
            else
                retVal.Link.Add(new BundleLink(new Uri(baseUri), "self"));

            // Updated
            retVal.Timestamp = DateTime.Now;
            //retVal.Generator = "MARC-HI Service Core Framework";

            // HACK: Remove me
            if(queryResult != null)
                retVal.Total = queryResult.TotalResults;

            
            // Results
            if (result.Results != null)
            {
                var feedItems = new List<BundleEntry>();
                foreach (DomainResourceBase itm in result.Results)
                {
                    Uri resourceUrl = new Uri(String.Format("{0}/{1}?_format={2}", RestOperationContext.Current.IncomingRequest.Url, String.Format("{0}/{1}/_history/{2}", itm.GetType().Name, itm.Id, itm.VersionId), format));
                    BundleEntry feedResult = new BundleEntry(); //new Bundleentry(String.Format("{0} id {1} version {2}", itm.GetType().Name, itm.Id, itm.VersionId), null ,resourceUrl);

                    feedResult.FullUrl = resourceUrl;

                    string summary = "<div xmlns=\"http://www.w3.org/1999/xhtml\">" + itm.Text.ToString() + "</div>";

                    // Add confidence if the attribute permits
                    ConfidenceAttribute confidence = itm.Attributes.Find(a => a is ConfidenceAttribute) as ConfidenceAttribute;
                    if (confidence != null)
                        feedResult.Search = new BundleSearch()
                        {
                            Score = confidence.Confidence
                        };

                    feedResult.Resource = new BundleResrouce(itm);
                    feedItems.Add(feedResult);
                }
                retVal.Entry = feedItems;
            }

            // Outcome
            //if (result.Details.Count > 0 || result.Issues != null && result.Issues.Count > 0)
            //{
            //    var outcome = CreateOutcomeResource(result);
            //    retVal.ElementExtensions.Add(outcome, new XmlSerializer(typeof(OperationOutcome)));
            //    retVal.Description = new TextSyndicationContent(outcome.Text.ToString(), TextSyndicationContentKind.Html);
            //}
            return retVal;

        }



        /// <summary>
        /// Create an operation outcome resource
        /// </summary>
        public static OperationOutcome CreateOutcomeResource(FhirOperationResult result)
        {
            var retVal = new OperationOutcome();

            Uri fhirIssue = new Uri("http://hl7.org/fhir/issue-type");

            // Add issues for each of the details
            foreach (var dtl in result.Details)
            {
                Issue issue = new Issue()
                {
                    Diagnostics = new DataTypes.FhirString(dtl.Message),
                    Severity = (IssueSeverity)Enum.Parse(typeof(IssueSeverity), dtl.Type.ToString())
                };

                if (!String.IsNullOrEmpty(dtl.Location))
                    issue.Location.Add(new DataTypes.FhirString(dtl.Location));

                // Type
                if (dtl.Exception is TimeoutException)
                    issue.Code = new DataTypes.FhirCoding(fhirIssue, "timeout");
                else if (dtl is FixedValueMisMatchedResultDetail)
                    issue.Code = new DataTypes.FhirCoding(fhirIssue, "value");
                else if (dtl is IOException)
                    issue.Code = new DataTypes.FhirCoding(fhirIssue, "no-store");
                else
                    issue.Code = new DataTypes.FhirCoding(fhirIssue, "exception");

                retVal.Issue.Add(issue);
            }

            
            return retVal;
        }

    }
}
