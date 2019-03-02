﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SanteDB.Core.Configuration.Data;
using SanteDB.Configurator.Util;
using System.Drawing.Design;
using SanteDB.Core.Configuration;

namespace SanteDB.Configurator.Controls
{
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
                this.cbxProviderType.SelectedItem = this.cbxProviderType.Items.OfType<ProviderWrapper>().FirstOrDefault(o => o.Provider.Invariant == value.Provider);
            }
        }

        /// <summary>
        /// Gets the provider
        /// </summary>
        public IDataConfigurationProvider Provider {
            get
            {
                return (this.cbxProviderType.SelectedItem as ProviderWrapper)?.Provider as IDataConfigurationProvider;
            }
        }

        /// <summary>
        /// Drop down is dropping down
        /// </summary>
        private void cbxProviderType_DropDown(object sender, EventArgs e)
        {
            if (cbxProviderType.Items.Count == 0)
                cbxProviderType.Items.AddRange(ConfigurationContext.Current.DataProviders.Select(p => new ProviderWrapper(p)).ToArray());
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

    /// <summary>
    /// Provider wrapper
    /// </summary>
    internal class ProviderWrapper
    {

        /// <summary>
        /// Gets the provider
        /// </summary>
        public IDataConfigurationProvider Provider { get; }

        /// <summary>
        /// Creates a new provider wrapper
        /// </summary>
        public ProviderWrapper(IDataConfigurationProvider p)
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
}