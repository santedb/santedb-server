/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Represent an editor which
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DropDownValueEditor : UITypeEditor
    {
        // Values
        private readonly IEnumerable<object> m_ddeValues;

        /// <summary>
        /// Creates a new wrapper for drop down editor
        /// </summary>
        public DropDownValueEditor(IEnumerable values)
        {
            this.m_ddeValues = values.OfType<object>();
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
                    if (!string.IsNullOrEmpty(converterTypeName))
                    {
                        typeConverer = Activator.CreateInstance(Type.GetType(converterTypeName)) as TypeConverter;
                    }

                    // Add ranges
                    list.Items.AddRange(
                        this.m_ddeValues.Select(o => new ConvertedObjectValue
                        {
                            Name = typeConverer?.ConvertTo(context, CultureInfo.InvariantCulture, o, typeof(string))?.ToString() ?? o.ToString(),
                            Value = o
                        }).OfType<object>().ToArray()
                    );
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error retrieving database providers: {e.Message}");
                    return value;
                }

                winService.DropDownControl(list);

                if (list.SelectedItem != null)
                {
                    return (list.SelectedItem as ConvertedObjectValue)?.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// Get edit style
        /// </summary>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        /// <summary>
        /// Gets the type selector
        /// </summary>
        private class ConvertedObjectValue
        {
            /// <summary>
            /// Gets the name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets the type
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            /// Convert to a string
            /// </summary>
            public override string ToString()
            {
                return this.Name;
            }
        }
    }
}