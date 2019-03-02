using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Represent an editor which
    /// </summary>
    public class DropDownValueEditor : UITypeEditor
    {

        /// <summary>
        /// Gets the type selector
        /// </summary>
        private class ConvertedObjectValue
        {
            /// <summary>
            /// Gets the type
            /// </summary>
            public Object Value { get; set; }

            /// <summary>
            /// Gets the name
            /// </summary>
            public String Name { get; set; }

            /// <summary>
            /// Convert to a string
            /// </summary>
            public override string ToString() => this.Name;
        }

        // Values
        private IEnumerable<Object> m_ddeValues;

        /// <summary>
        /// Creates a new wrapper for drop down editor
        /// </summary>
        public DropDownValueEditor(IEnumerable values)
        {
            this.m_ddeValues = values.OfType<Object>();
        }

        /// <summary>
        /// Get edit style
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        /// <summary>
        /// Edit the value
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                var list = new ListBox();
                list.Click += (o, e) => winService.CloseDropDown();

                // Get the databases
                try
                {
                    var converterTypeName = context.PropertyDescriptor.Attributes.OfType<TypeConverterAttribute>().FirstOrDefault()?.ConverterTypeName;
                    TypeConverter typeConverer = null;
                    if (!String.IsNullOrEmpty(converterTypeName))
                        typeConverer = Activator.CreateInstance(Type.GetType(converterTypeName)) as TypeConverter;

                    // Add ranges
                    list.Items.AddRange(
                        this.m_ddeValues.
                            Select(o=>new ConvertedObjectValue()
                            {
                                Name = typeConverer?.ConvertTo(context, CultureInfo.InvariantCulture, o, typeof(String))?.ToString() ?? o.ToString(),
                                Value = o
                            }).OfType<Object>().ToArray()
                    );
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error retrieving database providers: {e.Message}");
                    return value;
                }

                winService.DropDownControl(list);

                if (list.SelectedItem != null)
                    return (list.SelectedItem as ConvertedObjectValue)?.Value;
            }
            return value;
        }
    }
}
