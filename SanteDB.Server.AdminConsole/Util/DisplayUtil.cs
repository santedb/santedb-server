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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Server.AdminConsole.Shell;

namespace SanteDB.Server.AdminConsole.Util
{
    /// <summary>
    /// Display utilities
    /// </summary>
    public static class DisplayUtil
    {
        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// Print table 
        /// </summary>
        public static void TablePrint<T>(IEnumerable<T> data, params Expression<Func<T, Object>>[] columns)
        {
            TablePrint(data, null, null, columns);
        }

        /// <summary>
        /// Print data as columns
        /// </summary>
        public static void TablePrint<T>(IEnumerable<T> data, String[] colNames, int[] colWidths, params Expression<Func<T, Object>>[] columns)
        {

            if (colNames != null && colNames.Length != columns.Length)
                throw new ArgumentException("When specified, colNames must match columns");

            // Column width
            int defaultWidth = (Console.WindowWidth - columns.Length) / columns.Length,
                c = 0;
            int[] cWidths = colWidths ?? columns.Select(o=>defaultWidth).ToArray();

            foreach(var col in columns)
            {
                // Only process lambdas
                if(colNames != null)

                if (col.NodeType != ExpressionType.Lambda) continue;
                var body = (col as LambdaExpression).Body;
                if (body.NodeType == ExpressionType.Convert)
                    body = (body as UnaryExpression).Operand;

                var member = (body as MemberExpression)?.Member;
                string colName = colNames?[c] ?? member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? member?.Name ?? "??";
                if (colName.Length > colWidths[c])
                    Console.Write("{0}... ", colName.Substring(0, colWidths[c] - 3));
                else
                    Console.Write("{0}{1} ", colName, new String(' ', colWidths[c] - colName.Length));
                c++;
            }

            Console.WriteLine();

            // Now output data
            foreach(var tuple in data)
            {
                c = 0;
                foreach (var col in columns)
                {
                    try
                    {
                        Object value = col.Compile().DynamicInvoke(tuple);
                        String stringValue = value?.ToString();
                        if (stringValue == null)
                            Console.Write(new string(' ', colWidths[c] + 1));
                        else if (stringValue.Length > colWidths[c])
                            Console.Write("{0}... ", stringValue.Substring(0, colWidths[c] - 3));
                        else
                            Console.Write("{0}{1} ", stringValue, new String(' ', colWidths[c] - stringValue.Length));
                    }
                    catch
                    {
                        Console.Write(new string(' ', colWidths[c] + 1));
                    }
                    finally
                    {
                        c++;
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print policy information
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="user"></param>
        public static void PrintPolicies<T>(ISecurityEntityInfo<T> user, String[] dataLabels, params Expression<Func<T, object>>[] data)
            where T : SecurityEntity
        {

            int d = 0;
            foreach(var dat in data)
            {
                try
                {
                    Console.WriteLine("{0}: {1}", dataLabels[d], dat.Compile().DynamicInvoke(user.Entity));
                }
                catch
                {
                }
                finally
                {
                    d++;
                }
            }

            List<SecurityPolicyInfo> policies = m_client.GetPolicies(o => o.ObsoletionTime == null).CollectionItem.OfType<SecurityPolicy>().OrderBy(o => o.Oid).Select(o => new SecurityPolicyInfo(o)).ToList();
            policies.ForEach(o => o.Grant = (PolicyGrantType)10);
            foreach (var pol in user.Policies)
            {
                var existing = policies.FirstOrDefault(o => o.Oid == pol.Oid);
                if (pol.Grant < existing.Grant)
                    existing.Grant = pol.Grant;
            }

            Console.WriteLine("\tEffective Policies:");
            foreach (var itm in policies)
            {
                Console.Write("\t\t{0} [{1}] : ", itm.Name,itm.Oid);
                if (itm.Grant == (PolicyGrantType)10) // Lookup parent
                {
                    var parent = policies.LastOrDefault(o => itm.Oid.StartsWith(o.Oid + ".") && itm.Oid != o.Oid);
                    if (parent != null && parent.Grant <= PolicyGrantType.Grant)
                        Console.WriteLine("{0} (inherited from {1})", parent.Grant, parent.Name);
                    else
                        Console.WriteLine("--- (default DENY)");
                }
                else
                    Console.WriteLine("{0} (explicit)", itm.Grant);
            }
        }

        /// <summary>
        /// Prompt for a masked password prompt
        /// </summary>
        internal static string PasswordPrompt(string prompt)
        {
            Console.Write(prompt);

            var c = (ConsoleKey)0;
            StringBuilder passwd = new StringBuilder();
            while (c != ConsoleKey.Enter)
            {
                var ki = Console.ReadKey();
                c = ki.Key;

                if (c == ConsoleKey.Backspace)
                {
                    if (passwd.Length > 0)
                    {
                        passwd = passwd.Remove(passwd.Length - 1, 1);
                        Console.Write(" \b");
                    }
                    else
                        Console.CursorLeft = Console.CursorLeft + 1;
                }
                else if (c == ConsoleKey.Escape)
                    return String.Empty;
                else if (c != ConsoleKey.Enter)
                {
                    passwd.Append(ki.KeyChar);
                    Console.Write("\b*");
                }
            }
            Console.WriteLine();
            return passwd.ToString();
        }
    }
}
