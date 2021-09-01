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
 * Date: 2021-8-5
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections;
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
            var msb = new ModelSerializationBinder();

            if (provider != null)
            {
                var winService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (typeof(IList).IsAssignableFrom(context.PropertyDescriptor.PropertyType)) // multi-select
                {

                    var itemType = context.PropertyDescriptor.PropertyType.GetGenericArguments()[0];
                    var list = new ListView()
                    {
                        View = View.Details,
                        FullRowSelect = true,
                        CheckBoxes = true,
                        HeaderStyle = ColumnHeaderStyle.None,
                        Sorting = SortOrder.Ascending
                    };
                    list.Columns.Add("default");

                    var listValue = value as IEnumerable<ResourceTypeReferenceConfiguration>;
                    // Get the types
                    try
                    {

                        list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                            .Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && t.GetCustomAttribute<XmlRootAttribute>() != null && !t.IsGenericTypeDefinition && !t.IsInterface && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                            .Select(o =>
                            {
                                msb.BindToName(o, out string asm, out string type);
                                return new ListViewItem(type)
                                {
                                    Checked = listValue?.Any(v => v.TypeXml == type) == true,
                                    Tag = new ResourceTypeReferenceConfiguration() { TypeXml = type }
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

                    return Activator.CreateInstance(context.PropertyDescriptor.PropertyType, list.CheckedItems.OfType<ListViewItem>().Select(o => o.Tag).OfType<ResourceTypeReferenceConfiguration>());
                }
                else // Single select
                {
                    var list = new ListBox();
                    list.Click += (o, e) => winService.CloseDropDown();

                    // Get the databases
                    try
                    {

                        list.Items.AddRange(AppDomain.CurrentDomain.GetAllTypes()
                            .Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && t.GetCustomAttribute<XmlRootAttribute>() != null && !t.IsGenericTypeDefinition && !t.IsInterface && t.GetCustomAttribute<ObsoleteAttribute>() == null && !t.IsAbstract)
                            .Select(o =>
                            {
                                msb.BindToName(o, out string asm, out string type);
                                return new ResourceTypeReferenceConfiguration() { TypeXml = type };
                            })
                            .ToArray());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error retrieving available types : {e.Message}");
                        return value;
                    }

                    winService.DropDownControl(list);

                    if (list.SelectedItem != null)
                    {
                        return list.SelectedItem as ResourceTypeReferenceConfiguration;
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
