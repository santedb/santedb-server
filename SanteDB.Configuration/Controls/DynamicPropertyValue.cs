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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Configuration.Converters;
using SanteDB.Configuration.Editors;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;

namespace SanteDB.Configuration.Controls
{
    /// <summary>
    /// Extension value
    /// </summary>
    public class DynamicPropertyValue
    {

        /// <summary>
        /// Updatetarget
        /// </summary>
        private Dictionary<String, object> m_updateTarget;

        /// <summary>
        /// The value
        /// </summary>
        private Object m_value;

        public String Name { get; set; }
        public Type ExtensionType { get; set; }

        public Object Value
        {
            get => this.m_value;
            set
            {
                this.m_value = value;
                if (this.m_updateTarget != null)
                    this.m_updateTarget[this.Name] = value;
            }
        }

        public AttributeCollection Attributes { get; set; }
        public Object Editor { get; set; }

        /// <summary>
        /// Extension value ctor
        /// </summary>
        public DynamicPropertyValue(string name, Type type, Object editor, Attribute[] attribute, Object value, Dictionary<string, object> updateTarget)
        {
            this.Editor = editor;
            if (attribute != null)
                this.Attributes = new AttributeCollection(attribute);
            this.Name = name;
            this.ExtensionType = type;
            this.Value = value;
            this.m_updateTarget = updateTarget;
        }
    }

    /// <summary>
    /// Custom property descriptor
    /// </summary>
    public class DynamicPropertyValueDescriptor : PropertyDescriptor
    {
        private DynamicPropertyValue m_extensionValue;

        /// <summary>
        /// Creates the extension description
        /// </summary>
        public DynamicPropertyValueDescriptor(ref DynamicPropertyValue extensionValue, Attribute[] attrs) : base(extensionValue.Name, attrs)
        {
            this.m_extensionValue = extensionValue;
        }

        /// <summary>
        /// Get the specified editor
        /// </summary>
        public override object GetEditor(Type editorBaseType)
        {
            return this.m_extensionValue.Editor;
        }

        /// <summary>
        /// Property type
        /// </summary>
        public override Type PropertyType
        {
            get { return this.m_extensionValue.ExtensionType; }
        }

        /// <summary>
        /// Can reset value
        /// </summary>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Component type
        /// </summary>
        public override Type ComponentType
        {
            get { return this.m_extensionValue.ExtensionType; }
        }

        /// <summary>
        /// Get the value
        /// </summary>
        public override object GetValue(object component)
        {
            return this.m_extensionValue.Value;
        }

        /// <summary>
        /// Is readonly
        /// </summary>
        public override bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Reset the value
        /// </summary>
        public override void ResetValue(object component)
        {
            ;
        }

        /// <summary>
        /// Set value
        /// </summary>
        public override void SetValue(object component, object value)
        {
            this.m_extensionValue.Value = value;
        }

        /// <summary>
        /// Should serialize
        /// </summary>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Get the attributes
        /// </summary>
        public override AttributeCollection Attributes => this.m_extensionValue.Attributes ?? base.Attributes;
    }

    /// <summary>
    /// Property class
    /// </summary>
    public class DynamicPropertyClass : CollectionBase, ICustomTypeDescriptor
    {

        // Configuration
        private GenericFeatureConfiguration m_config;

        /// <summary>
        /// Creates a new generic feature configuration
        /// </summary>
        public DynamicPropertyClass(GenericFeatureConfiguration genericFeatureConfiguration)
        {
            this.m_config = genericFeatureConfiguration;
            foreach (var itm in genericFeatureConfiguration.Options)
            {
                var value = itm.Value();
                var type = typeof(String);
                Attribute[] attribute = null;
                UITypeEditor uiEditor = null;

                if (value is ConfigurationOptionType)
                    switch ((ConfigurationOptionType)value)
                    {
                        case ConfigurationOptionType.Boolean:
                            type = typeof(bool);
                            break;
                        case ConfigurationOptionType.Numeric:
                            type = typeof(Int32);
                            break;
                        case ConfigurationOptionType.Password:
                            attribute = new Attribute[] { new PasswordPropertyTextAttribute() };
                            break;
                        case ConfigurationOptionType.FileName:
                            uiEditor = new System.Windows.Forms.Design.FileNameEditor();
                            break;
                        case ConfigurationOptionType.Object:
                            attribute = new Attribute[] {
                                        new TypeConverterAttribute(typeof(ExpandableObjectConverter))
                                    };
                            type = typeof(Object);
                            break;
                    }
                else if (value is IEnumerable)
                {
                    if ((value as IEnumerable).OfType<Type>().Any())
                        attribute = new Attribute[]
                        {
                             new TypeConverterAttribute(typeof(ServiceProviderTypeConverter))
                        };
                    uiEditor = new DropDownValueEditor(value as IEnumerable);
                }

                var cat = genericFeatureConfiguration.Categories.FirstOrDefault(c => c.Value.Any(o => o == itm.Key));
                if (!String.IsNullOrEmpty(cat.Key))
                    attribute = (attribute ?? new Attribute[0]).Union(new Attribute[] { new CategoryAttribute(cat.Key) }).ToArray();
                this.Add(itm.Key, type, uiEditor, attribute, genericFeatureConfiguration.Values[itm.Key]);
            }
        }

        /// <summary>
        /// Default ctor
        /// </summary>
        public DynamicPropertyClass()
        {
        }

        /// <summary>
        /// Add extensions
        /// </summary>
        public void Add(String extensionName, Type extensionType, Object editor, Attribute[] attribute, Object value)
        {
            base.List.Add(new DynamicPropertyValue(extensionName, extensionType, editor, attribute, value, this.m_config?.Values));
        }

        /// <summary>
        /// Get a property value
        /// </summary>
        public Object this[string name]
        {
            get
            {
                foreach (var itm in this.List)
                    if ((itm as DynamicPropertyValue).Name == name)
                        return (itm as DynamicPropertyValue).Value;
                return null;
            }
            set
            {
                foreach (var itm in this.List)
                    if ((itm as DynamicPropertyValue).Name == name)
                        (itm as DynamicPropertyValue).Value = value;
            }
        }

        #region ICustomTypeDescriptor Members

        /// <summary>
        /// Get attributes for the property class
        /// </summary>
        public AttributeCollection GetAttributes()
        {
            return null;
        }

        /// <summary>
        /// Get the name of the class
        /// </summary>
        public string GetClassName()
        {
            return "Extension";
        }

        /// <summary>
        /// Get the component name
        /// </summary>
        /// <returns></returns>
        public string GetComponentName()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the converter
        /// </summary>
        public TypeConverter GetConverter()
        {
            return null;
        }

        /// <summary>
        /// Get default event
        /// </summary>
        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }

        /// <summary>
        /// Get the default property
        /// </summary>
        public PropertyDescriptor GetDefaultProperty()
        {
            var ev = this.List[0] as DynamicPropertyValue;
            return new DynamicPropertyValueDescriptor(ref ev, null);
        }

        /// <summary>
        /// Get the editor
        /// </summary>
        public object GetEditor(Type editorBaseType)
        {
            return null;
        }

        /// <summary>
        /// Get events of this item
        /// </summary>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return null;
        }

        /// <summary>
        /// Get all events
        /// </summary>
        public EventDescriptorCollection GetEvents()
        {
            return null;
        }

        /// <summary>
        /// Get properties (Extension values)
        /// </summary>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptor[] extensions = new PropertyDescriptor[this.Count];
            for (int i = 0; i < this.Count; i++)
            {
                DynamicPropertyValue ev = this.List[i] as DynamicPropertyValue;
                extensions[i] = new DynamicPropertyValueDescriptor(ref ev, attributes);
            }
            return new PropertyDescriptorCollection(extensions);

        }

        /// <summary>
        /// Get properties
        /// </summary>
        public PropertyDescriptorCollection GetProperties()
        {
            return this.GetProperties(null);
        }

        /// <summary>
        /// Get propert owner
        /// </summary>
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion
    }
}
