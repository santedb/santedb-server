/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using SanteDB.Core.Configuration.Data;

namespace SanteDB.Configuration.Editors
{

    /// <summary>
    /// Provider wrapper
    /// </summary>
    public class DataProviderWrapper
    {

        /// <summary>
        /// Gets the provider
        /// </summary>
        public IDataConfigurationProvider Provider { get; }

        /// <summary>
        /// Creates a new provider wrapper
        /// </summary>
        public DataProviderWrapper(IDataConfigurationProvider p)
        {
            this.Provider = p;
        }

        /// <summary>
        /// Represent the provider as a string
        /// </summary>
        public override string ToString()
        {
            return this.Provider.Name;
        }
    }

    /// <summary>
    /// Creates a database name editor
    /// </summary>
    public class DatabaseNameEditor : UITypeEditor
    {

        /// <summary>
        /// Reference to current connection string
        /// </summary>
        private ConnectionString m_connectionString;
        /// <summary>
        /// Reference to current data provider
        /// </summary>
        private IDataConfigurationProvider m_provider;

        /// <summary>
        /// Represents the database name editor
        /// </summary>
        public DatabaseNameEditor(IDataConfigurationProvider provider, ConnectionString connectionString)
        {
            this.m_connectionString = connectionString;
            this.m_provider = provider;
        }

        /// <summary>
        /// Edit the value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if(provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                var list = new ListBox();
                list.Click += (o, e) => winService.CloseDropDown();

                // Get the databases
                try
                {
                    list.Items.AddRange(this.m_provider.GetDatabases(this.m_connectionString).OfType<Object>().ToArray());
                    list.Items.Add("New...");
                }
                catch(TargetInvocationException e)
                {
                    MessageBox.Show($"Error retrieving databases: {e.InnerException?.Message}");
                    return value;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error retrieving databases: {e.Message}");
                    return value;
                }

                winService.DropDownControl(list);

                if (list.SelectedItem != null && list.SelectedIndices.Count == 1)
                {
                    if (list.SelectedItem.ToString() == "New...")
                    {
                        var frmNewDatabase = new frmNewDatabase(this.m_connectionString, this.m_provider);
                        if (frmNewDatabase.ShowDialog() == DialogResult.OK)
                            return frmNewDatabase.ConnectionString.GetComponent("database") ?? frmNewDatabase.ConnectionString.GetComponent("initial catalog");
                        else
                            return value;
                    }
                    else
                        return list.SelectedItem;
                }
            }
            return value;

        }

        /// <summary>
        /// Get the edit stype
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }
}