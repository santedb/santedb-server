/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Security.Configuration;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Creates a database name editor
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class X509Certificate2Editor : UITypeEditor
    {
        /// <summary>
        /// Edit the value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                var instanceType = context.Instance.GetType();
                var findStore = (StoreName)instanceType.GetProperty("StoreName").GetValue(context.Instance, null);
                var findLocation = (StoreLocation)instanceType.GetProperty("StoreLocation").GetValue(context.Instance, null);
                var store = new X509Store(findStore, findLocation);
                try
                {
                    var needsPrivateKey = true;
                    // X509 Config element
                    if (context.Instance is X509ConfigurationElement x509)
                    {
                        needsPrivateKey = !x509.ValidationOnly;
                    }

                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    var coll = new X509Certificate2Collection();
                    foreach (var c in store.Certificates)
                    {
                        if (c.HasPrivateKey || !needsPrivateKey)
                        {
                            coll.Add(c);
                        }
                    }

                    store.Close();

                    var cert = X509Certificate2UI.SelectFromCollection(coll, "Select Certificate", "Please select the X.509 certificate for this configuration option", X509SelectionFlag.SingleSelection);
                    if (cert.Count > 0)
                    {
                        value = cert[0];
                    }
                    else
                    {
                        value = null;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error getting certificate list: {e.Message}");
                    Trace.TraceError($"Error getting certificate list: {e}");
                }
                finally
                {
                    store.Close();
                }
            }

            return value;
        }

        /// <summary>
        /// Get the edit stype
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
    }
}