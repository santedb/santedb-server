using SanteDB.Core.Model.Serialization;
using SanteDB.OrmLite.Attributes;
using SanteDB.OrmLite.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Represents a dropdown that allows users to select ALE fields
    /// </summary>
    public class AleFieldSelectorEditor : UITypeEditor
    {
        /// <inheritdoc/>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.DropDown;

        /// <inheritdoc/>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            var msb = new ModelSerializationBinder();

            if (provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                var list = new ListView
                {
                    View = System.Windows.Forms.View.Details,
                    FullRowSelect = true,
                    CheckBoxes = true,
                    HeaderStyle = ColumnHeaderStyle.None,
                    Sorting = SortOrder.Ascending
                };
                list.Columns.Add("default");

                var listValue = value as IEnumerable<OrmFieldConfiguration>;
                // Get the types
                try
                {
                    var fieldList = AppDomain.CurrentDomain.GetAllTypes()
                        .Where(t => t.HasCustomAttribute<TableAttribute>())
                        .SelectMany(t => t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                        .Where(p => p.HasCustomAttribute<ApplicationEncryptAttribute>())
                        .Select(p => p.GetCustomAttribute<ApplicationEncryptAttribute>().FieldName));

                    list.Items.AddRange(fieldList
                        .Select(o =>
                        {
                            return new ListViewItem(o)
                            {
                                Tag = new OrmFieldConfiguration() { Name = o, Mode = OrmAleMode.Deterministic },
                                Checked = listValue?.Any(v => v.Name == o) == true
                            };
                        })
                        .ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error retrieving available types: {e.Message}");
                    return value;
                }

                list.Columns[0].Width = -2;
                winService.DropDownControl(list);

                return new List<OrmFieldConfiguration>(list.CheckedItems.OfType<ListViewItem>().Select(o => o.Tag as OrmFieldConfiguration));
            }

            return value;
        }
    }
}
