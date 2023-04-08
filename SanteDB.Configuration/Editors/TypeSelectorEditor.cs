/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SanteDB.Configuration.Editors
{
    /// <summary>
    /// Type selection wrapper
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class TypeSelectionWrapper
    {
        /// <summary>
        /// Creates a new wrapper
        /// </summary>
        public TypeSelectionWrapper(Type t)
        {
            this.Type = t;
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Name of the type
        /// </summary>
        public override string ToString()
        {
            return this.Type.GetCustomAttribute<ServiceProviderAttribute>()?.Name ?? this.Type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? this.Type.Name;
        }
    }

    /// <summary>
    /// Represents a type selector type editor
    /// </summary>
    [ExcludeFromCodeCoverage]
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
                    var itemType = context.PropertyDescriptor.PropertyType.GetGenericArguments()[0];
                    var list = new ListView
                    {
                        View = View.Details,
                        FullRowSelect = true,
                        CheckBoxes = true,
                        HeaderStyle = ColumnHeaderStyle.None,
                        Sorting = SortOrder.Ascending
                    };
                    list.Columns.Add("default");

                    var listValue = value as IEnumerable<TypeReferenceConfiguration>;
                    // Get the types
                    try
                    {
                        var bind = context.PropertyDescriptor.Attributes.OfType<BindingAttribute>().FirstOrDefault();
                        if (bind != null)
                        {
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                                .Where(t => bind.Binding.IsAssignableFrom(t) && !t.IsInterface && !t.IsGenericTypeDefinition && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                                .Select(o => new ListViewItem(new TypeSelectionWrapper(o).ToString())
                                {
                                    Checked = listValue?.Any(v => v.Type == o) == true,
                                    Tag = new TypeReferenceConfiguration(o)
                                })
                                .ToArray());
                        }
                        else
                        {
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                                .Where(t => context.PropertyDescriptor.PropertyType.StripGeneric().IsAssignableFrom(t) && !t.IsGenericTypeDefinition && !t.IsInterface && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                                .Select(o => new ListViewItem(new TypeSelectionWrapper(o).ToString())
                                {
                                    Checked = listValue?.Any(v => v.Type == o) == true,
                                    Tag = new TypeReferenceConfiguration(o)
                                })
                                .ToArray());
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error retrieving available types: {e.Message}");
                        return value;
                    }

                    list.Columns[0].Width = -2;
                    winService.DropDownControl(list);

                    return Activator.CreateInstance(context.PropertyDescriptor.PropertyType, list.CheckedItems.OfType<ListViewItem>().Select(o => o.Tag).OfType<TypeReferenceConfiguration>());
                }
                else // Single select
                {
                    var list = new ListBox();
                    list.Click += (o, e) => winService.CloseDropDown();

                    // Get the databases
                    try
                    {
                        var bind = context.PropertyDescriptor.Attributes.OfType<BindingAttribute>().FirstOrDefault();
                        if (bind != null)
                        {
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                                .Where(t => bind.Binding.IsAssignableFrom(t) && !t.IsInterface && !t.IsGenericTypeDefinition && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                                .Select(o => new TypeSelectionWrapper(o))
                                .ToArray());
                        }
                        else
                        {
                            list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                                .Where(t => context.PropertyDescriptor.PropertyType.IsAssignableFrom(t) && !t.IsGenericTypeDefinition && !t.IsInterface && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                                .Select(o => new TypeSelectionWrapper(o))
                                .ToArray());
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error retrieving available types : {e.Message}");
                        return value;
                    }

                    winService.DropDownControl(list);

                    if (list.SelectedItem != null)
                    {
                        if (context.PropertyDescriptor.PropertyType == typeof(TypeReferenceConfiguration))
                        {
                            return new TypeReferenceConfiguration((list.SelectedItem as TypeSelectionWrapper)?.Type);
                        }

                        return (list.SelectedItem as TypeSelectionWrapper)?.Type;
                    }
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