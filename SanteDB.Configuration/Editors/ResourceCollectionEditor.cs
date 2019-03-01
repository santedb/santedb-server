using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml.Serialization;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Resource collection editor
    /// </summary>
    public class ResourceCollectionEditor : UITypeEditor
    {
        /// <summary>
        /// Edit the value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                var list = new ListView()
                {
                    View = View.Details,
                    FullRowSelect = true,
                    CheckBoxes = true,
                    HeaderStyle = ColumnHeaderStyle.None,
                    Sorting = SortOrder.Ascending
                };
                list.Columns.Add("default");

                var listValue = (value as IEnumerable<String>).ToArray();
                // Get the databases
                try
                {
                    list.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a=>!a.IsDynamic)
                        .SelectMany(a=>a.ExportedTypes)
                        .Where(t=>typeof(IdentifiedData).IsAssignableFrom(t) && t.GetCustomAttribute<XmlRootAttribute>(false) != null)
                        .Select(o=>new ListViewItem(o.Name)
                        {
                            Checked = listValue.Contains(o.Name)
                        })
                        .ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error retrieving resources providers: {e.Message}");
                    return value;
                }

                list.Columns[0].Width = -2;
                winService.DropDownControl(list);

                return Activator.CreateInstance(context.PropertyDescriptor.PropertyType, list.CheckedItems.OfType<ListViewItem>().Select(o => o.Text));
                
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
