using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using SanteDB.Core.Configuration.Data;

namespace SanteDB.Configurator.Controls
{
    /// <summary>
    /// Creates a database name editor
    /// </summary>
    internal class DatabaseNameEditor : UITypeEditor
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
                        var frmNewDatabase = new frmNewDatabase(this.m_connectionString);
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