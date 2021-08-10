using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Date time picker
    /// </summary>
    public class DateTimePickerEditor : UITypeEditor
    {

        IWindowsFormsEditorService editorService;
        DateTimePicker picker = new DateTimePicker();

        public DateTimePickerEditor()
        {
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "dd/MM/yyyy HH:mm:ss";
            picker.MinDate = DateTime.MinValue;
            picker.MaxDate = DateTime.MaxValue;
            
        }

        /// <summary>
        /// Get the edit style
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        /// <summary>
        /// Edit the value of the picker
        /// </summary>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                this.editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            }

            if((DateTime)value == DateTime.MinValue)
            {
                value = DateTime.Now;
            }

            if (this.editorService != null)
            {
                picker.Value = (DateTime)value;
                this.editorService.DropDownControl(picker);
                value = picker.Value;
            }

            return value;
        }
    }
}
