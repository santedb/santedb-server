/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2021-8-27
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Messaging.HDSI.Client;
using SanteDB.Server.AdminConsole.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Server.AdminConsole.Util;
using System.Linq.Expressions;
using SanteDB.Core.Model.Export;
using System.Xml.Serialization;
using SanteDB.Core.Interop;

namespace SanteDB.Server.AdminConsole.Shell.CmdLets
{
    [AdminCommandlet]
    public static class DataCmdlet
    {

        // hdsi client
        private static HdsiServiceClient m_client = new HdsiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.HealthDataService));

        private static XmlSerializer m_xsz = new XmlSerializer(typeof(Dataset));

        /// <summary>
        /// Base HDSI parameters
        /// </summary>
        internal abstract class BaseHdsiParameters
        {

            /// <summary>
            /// The resource type being operated on
            /// </summary>
            [Description("The type of resource to operate on")]
            [Parameter("resourceType")]
            [Parameter("r")]
            public String ResourceType { get; set; }

            /// <summary>
            /// Emit as dataset
            /// </summary>
            [Description("Emit result in dataset format")]
            [Parameter("dataset")]
            public bool AsDataSet { get; set; }
        }

        /// <summary>
        /// Query parameters
        /// </summary>
        internal class HdsiQueryParameters : BaseHdsiParameters
        {

            /// <summary>
            /// Offset of first record
            /// </summary>
            [Parameter("offset")]
            [Parameter("o")]
            [Description("Offset of result set")]
            public String Offset { get; set; }

            /// <summary>
            /// Count of result set
            /// </summary>
            [Parameter("count")]
            [Parameter("c")]
            public String Count { get; set; }

            /// <summary>
            /// Display 
            /// </summary>
            [Parameter("d")]
            [Parameter("display")]
            [Description("Display the specified field")]
            public System.Collections.Specialized.StringCollection Display { get; set; }

            /// <summary>
            /// Filter to be applied
            /// </summary>
            [Parameter("*")]
            public System.Collections.Specialized.StringCollection Filter { get; set; }

            /// <summary>
            /// Expand paraeters
            /// </summary>
            [Parameter("expand")]
            public System.Collections.Specialized.StringCollection Expand { get; set; }
        }

        [AdminCommand("cdr.query", "Query data from CDR")]
        [Description("Query data from the clinical data repository of the CDR (note: you may need to authenticate yourself as a user that can read clinical data")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        internal static void QueryData(HdsiQueryParameters parms)
        {

            if (String.IsNullOrEmpty(parms.ResourceType))
                throw new ArgumentNullException("Require --resourceType or -r");

            // Get the type
            var type = new ModelSerializationBinder().BindToType(null, parms.ResourceType);
            if (type == null)
                throw new InvalidOperationException($"Cannot find reosurce type {parms.ResourceType}");

            if(!parms.AsDataSet)
                Console.WriteLine("Type: {0}", type);

            // Build the parameter list
            NameValueCollection nvc = new NameValueCollection();
            if(parms.Filter != null)
            {
                foreach (var kv in parms.Filter)
                {
                    var f = kv.Split('=');
                    nvc.Add(f[0], f[1]);
                }
            }

            Int32.TryParse(parms.Offset ?? "0", out int offset);
            Int32.TryParse(parms.Count ?? "25", out int count);

            if (parms.Display == null)
                parms.Display = new System.Collections.Specialized.StringCollection() { "id", "ToString" };

            // Get the specified lambda expression
            var builderMethod = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { type }, new Type[] { typeof(NameValueCollection) });
            var linqExpression = builderMethod.Invoke(null, new object[] { nvc });

            if (!parms.AsDataSet)
                Console.WriteLine("Filter: {0}", linqExpression);

            // Fetch results
            
            var queryMethod = m_client.GetType().GetGenericMethod(nameof(HdsiServiceClient.Query), new Type[] { type }, new Type[] { linqExpression.GetType(), typeof(int), typeof(int?), typeof(String[]), typeof(Guid?), typeof(ModelSort<>).MakeGenericType(type).MakeArrayType() });
            var result = queryMethod.Invoke(m_client, new object[] { linqExpression, offset, count, parms.Expand?.OfType<String>().ToArray(), null, null }) as Bundle;


            if (!parms.AsDataSet)
            {
                Console.WriteLine("Result: {0} .. {1} of {2}", result.Offset, result.Item.Count, result.TotalResults);
                var displayCols = parms.Display.OfType<String>().Select(o =>
                {
                    return (Expression<Func<IdentifiedData, Object>>)(col => o == "ToString" ? col.ToString() : QueryExpressionParser.BuildPropertySelector(type, o, true).Compile().DynamicInvoke(col));
                }).ToArray();

                DisplayUtil.TablePrint<IdentifiedData>(result.Item, parms.Display.OfType<String>().ToArray(), parms.Display.OfType<String>().Select(o => 40).ToArray(), displayCols);
            }
            else
            {
                Dataset ds = new Dataset($"sdbac Dataset for {type} filter {nvc}");

                Delegate displaySelector = null;
                if (parms.Display.Count > 0)
                    displaySelector = QueryExpressionParser.BuildPropertySelector(type, parms.Display.OfType<String>().FirstOrDefault(), true).Compile();

                foreach (var itm in result.Item)
                    ds.Action.Add(new DataUpdate()
                    {
                        InsertIfNotExists = true,
                        IgnoreErrors = true,
                        Element = (IdentifiedData)(displaySelector != null ? displaySelector.DynamicInvoke(itm) : itm)
                    }) ;

                m_xsz.Serialize(Console.Out, ds);
                
            }
        }
    }
}
