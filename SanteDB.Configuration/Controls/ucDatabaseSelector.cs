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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SanteDB.Core.Configuration.Data;
using System.Drawing.Design;
using SanteDB.Core.Configuration;
using SanteDB.Configuration.Editors;

namespace SanteDB.Configuration.Controls
{
    [ExcludeFromCodeCoverage]
    public partial class ucDatabaseSelector : UserControl
    {
        public ucDatabaseSelector()
        {
            InitializeComponent();
        }

        // Connection string
        private ConnectionString m_connectionString = new ConnectionString(String.Empty, String.Empty);
        private bool m_configured = false;

        /// <summary>
        /// Fired when the configuration 
        /// </summary>
        public event EventHandler ConfigurationChanged;

        /// <summary>
        /// True when the configuration is complete
        /// </summary>
        public bool IsConfigured
        {
            get => this.m_configured;
            private set
            {
                this.m_configured = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public ConnectionString ConnectionString
        {
            get => this.m_connectionString;
            set {
                this.m_connectionString = value;
                // Select provider
                this.cbxProviderType.SelectedItem = this.cbxProviderType.Items.OfType<DataProviderWrapper>().FirstOrDefault(o => o.Provider.Invariant == value.Provider);
            }
        }

        /// <summary>
        /// Gets the provider
        /// </summary>
        public IDataConfigurationProvider Provider {
            get
            {
                return (this.cbxProviderType.SelectedItem as DataProviderWrapper)?.Provider as IDataConfigurationProvider;
            }
        }

        /// <summary>
        /// Drop down is dropping down
        /// </summary>
        private void cbxProviderType_DropDown(object sender, EventArgs e)
        {
            if (cbxProviderType.Items.Count == 0)
                cbxProviderType.Items.AddRange(ConfigurationContext.Current.DataProviders.Select(p => new DataProviderWrapper(p)).ToArray());
        }

        /// <summary>
        /// Set the object to a new instance of the class
        /// </summary>
        private void cbxProviderType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get the selector
            this.IsConfigured = false;
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);

            if (this.m_connectionString.Provider != this.Provider?.Invariant)
                this.m_connectionString = this.Provider.CreateConnectionString(new Dictionary<string, object>());

            var extendedValueProperty = new DynamicPropertyClass();
            foreach (var kv in this.Provider.Options) {
                Type typ = typeof(String);
                UITypeEditor uie = null;
                List<Attribute> attrs = new List<Attribute>() {
                    new CategoryAttribute(this.Provider.OptionGroups.FirstOrDefault(o=>o.Value.Contains(kv.Key)).Key ?? "Connection")
                };

                // Values
                switch(kv.Value)
                {
                    case ConfigurationOptionType.Boolean:
                        typ = typeof(bool);
                        break;
                    case ConfigurationOptionType.Numeric:
                        typ = typeof(int);
                        break;
                    case ConfigurationOptionType.Password:
                        attrs.Add(new PasswordPropertyTextAttribute(true));
                        break;
                    case ConfigurationOptionType.FileName:
                        uie = new System.Windows.Forms.Design.FileNameEditor();
                        break;
                    case ConfigurationOptionType.DatabaseName:
                        uie = new DatabaseNameEditor(this.Provider, this.ConnectionString);
                        break;
                }

                extendedValueProperty.Add(kv.Key, typ, uie, attrs.ToArray(), this.ConnectionString?.GetComponent(kv.Key));
            }

            this.pgProperties.SelectedObject = extendedValueProperty;
        }

        /// <summary>
        /// Update the specified property in the connection string
        /// </summary>
        private void pgProperties_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.m_connectionString.SetComponent(e.ChangedItem.PropertyDescriptor.Name, e.ChangedItem.Value?.ToString());
            this.IsConfigured = this.Provider.TestConnectionString(this.m_connectionString);
            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

    }

}
