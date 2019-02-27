using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configurator.Util
{
    /// <summary>
    /// Extension value
    /// </summary>
    public class DynamicPropertyValue
    {
        public String Name { get; set; }
        public Type ExtensionType { get; set; }
        public Object Value { get; set; }
        public AttributeCollection Attributes { get; set; }
        public Object Editor { get; set; }

        /// <summary>
        /// Extension value ctor
        /// </summary>
        public DynamicPropertyValue(string name, Type type, Object editor, Attribute[] attribute, Object value)
        {
            this.Editor = editor;
            if(attribute != null)
                this.Attributes = new AttributeCollection(attribute);
            this.Name = name;
            this.ExtensionType = type;
            this.Value = value;
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
        /// <summary>
        /// Add extensions
        /// </summary>
        public void Add(String extensionName, Type extensionType, Object editor, Attribute[] attribute, Object value)
        {
            base.List.Add(new DynamicPropertyValue(extensionName, extensionType, editor, attribute, value));
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
