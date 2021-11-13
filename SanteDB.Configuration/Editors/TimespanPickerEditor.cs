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
 * Date: 2021-8-27
 */

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Date time picker
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TimespanPickerEditor : UITypeEditor
    {
        private IWindowsFormsEditorService editorService;
        private readonly DateTimePicker picker = new DateTimePicker();

        public TimespanPickerEditor()
        {
            this.picker.Format = DateTimePickerFormat.Time;
            this.picker.MinDate = DateTime.MinValue;
            this.picker.MaxDate = DateTime.MaxValue;
            this.picker.ShowUpDown = true;
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

            if ((DateTime) value == DateTime.MinValue)
            {
                value = DateTime.Now;
            }

            if (this.editorService != null)
            {
                this.picker.Value = DateTime.Today.Add((TimeSpan) value);
                this.editorService.DropDownControl(this.picker);
                value = this.picker.Value.TimeOfDay;
            }

            return value;
        }

        /// <summary>
        /// Get the edit style
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }
}