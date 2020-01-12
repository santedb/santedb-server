using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using SanteDB.Configuration.Attributes;
using SanteDB.Core.Model;

namespace SanteDB.Configuration.Editors
{

    /// <summary>
    /// Type selection wrapper
    /// </summary>
    public class TypeSelectionWrapper
    {
        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Creates a new wrapper
        /// </summary>
        public TypeSelectionWrapper(Type t)
        {
            this.Type = t;
        }

        /// <summary>
        /// Name of the type
        /// </summary>
        public override string ToString() => this.Type.Name;
    }

    /// <summary>
    /// Represents a type selector type editor
    /// </summary>
    public class TypeSelectorEditor : UITypeEditor
    {
        /// <summary>
        /// Edit the value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (typeof(IList).IsAssignableFrom(context.PropertyDescriptor.PropertyType)) // multi-select
                {
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
                    // Get the types
                    try
                    {

                        var bind = context.PropertyDescriptor.Attributes.OfType<TypeSelectorBindAttribute>().FirstOrDefault();
                        if (bind != null)
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.IsDynamic)
                                .SelectMany(a => a.ExportedTypes)
                                .Where(t => bind.BindType.IsAssignableFrom(t))
                                .Select(o => new ListViewItem(o.Name)
                                {
                                    Checked = listValue.Contains(o.Name)
                                })
                                .ToArray());
                        else
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.IsDynamic)
                                .SelectMany(a => a.ExportedTypes)
                                .Where(t => context.PropertyDescriptor.PropertyType.StripGeneric().IsAssignableFrom(t))
                                .Select(o => new ListViewItem(o.Name)
                                {
                                    Checked = listValue.Contains(o.Name)
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

                    return Activator.CreateInstance(context.PropertyDescriptor.PropertyType, list.CheckedItems.OfType<ListViewItem>().Select(o => o.Text));
                }
                else // Single select
                {
                    var list = new ListBox();
                    list.Click += (o, e) => winService.CloseDropDown();

                    // Get the databases
                    try
                    {
                        var bind = context.PropertyDescriptor.Attributes.OfType<TypeSelectorBindAttribute>().FirstOrDefault();
                        if (bind != null)
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.IsDynamic)
                                .SelectMany(a => a.ExportedTypes)
                                .Where(t => bind.BindType.IsAssignableFrom(t))
                                .Select(o => new TypeSelectionWrapper(o))
                                .ToArray());
                        else
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                                .Where(a => !a.IsDynamic)
                                .SelectMany(a => a.ExportedTypes)
                                .Where(t => context.PropertyDescriptor.PropertyType.IsAssignableFrom(t))
                                .Select(o => new TypeSelectionWrapper(o))
                                .ToArray());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error retrieving available types : {e.Message}");
                        return value;
                    }

                    winService.DropDownControl(list);

                    if (list.SelectedItem != null)
                        return (list.SelectedItem as TypeSelectionWrapper)?.Type;
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
