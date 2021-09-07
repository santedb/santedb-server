/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
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
