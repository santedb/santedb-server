/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Map;
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive values
    /// </summary>
    [XmlType(Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public abstract class Primitive<T> : FhirElement
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public Primitive()
        {
        }

        /// <summary>
        /// Primitive value
        /// </summary>
        public Primitive(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of the primitive
        /// </summary>
        [XmlIgnore]
        public virtual T Value { get; set; }

        /// <summary>
        /// Gets or sets the XML Value of the data
        /// </summary>
        [XmlAttribute("value")]
        public virtual String XmlValue
        {
            get {
                if (typeof(T).IsEnum)
                    return typeof(T).GetField(this.Value.ToString()).GetCustomAttribute<XmlEnumAttribute>().Name;
                if (this.Value is byte[])
                    return Convert.ToBase64String(this.Value as Byte[]);
                if (this.Value != null)
                    return this.Value.ToString();
                else
                    return null;
            
            }
            set {

                // From xmlenum
                if (typeof(T).IsEnum)
                    this.Value = (T)typeof(T).GetFields().FirstOrDefault(o => o.GetCustomAttribute<XmlEnumAttribute>()?.Name == value || o.Name == value).GetValue(null);
                else {
                    object res = null;
                    if (MapUtil.TryConvert(value, typeof(T), out res))
                        this.Value = (T)res;
                    else throw new ArgumentException($"{value} cannot be translated to {typeof(T)}", nameof(value));
                }
            }
        }

        /// <summary>
        /// Convert Primitive to wrapped
        /// </summary>
        public static implicit operator T(Primitive<T> v)
        {
            if (v == null)
                return default(T);
            return v.Value;
        }

        /// <summary>
        /// Cast as string
        /// </summary>
        public override string ToString()
        {
            return this.XmlValue;
        }

        /// <summary>
        /// Write the text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteString(this.XmlValue);
        }
    }

    /// <summary>
    /// Represents a Uri
    /// </summary>
    [XmlType("decimal", Namespace = "http://hl7.org/fhir")]
    public class FhirDecimal : Primitive<Decimal?>
    {
        /// <summary>
        /// Creates a new instance of the decimal wrapper
        /// </summary>
        public FhirDecimal() : base() { }
        /// <summary>
        /// Creates a new wrapper of the decimal with specified value
        /// </summary>
        /// <param name="value">The value of the wrapper</param>
        public FhirDecimal(Decimal value) : base(value) { }

        /// <summary>
        /// Converts a decimal to a wrapped decimal
        /// </summary>
        /// <param name="v">The value to be converted</param>
        public static implicit operator FhirDecimal(Decimal v) { return new FhirDecimal(v); }

        /// <summary>
        /// Gets or sets the value of the decimal
        /// </summary>
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if (this.Value != null)
                    return XmlConvert.ToString(this.Value.Value);
                return null;
            }
            set
            {
                if (value != null)
                    base.Value = XmlConvert.ToDecimal(value);
                else
                    this.Value = default(decimal);
            }
        }
    }

    /// <summary>
    /// Represents a boolean
    /// </summary>
    [XmlType("boolean", Namespace = "http://hl7.org/fhir")]
    public class FhirBoolean : Primitive<Boolean?> {

        /// <summary>
        /// Construct for FHIR boolean wrapper
        /// </summary>
        public FhirBoolean() : base() { }

        /// <summary>
        /// Creates a new boolean wrapper with specified  value
        /// </summary>
        /// <param name="value">The initial value</param>
        public FhirBoolean(Boolean value) : base(value) { }

        /// <summary>
        /// Converts a boolean to a wrapped boolean
        /// </summary>
        /// <param name="v">The value to be converted/wrapped</param>
        public static implicit operator FhirBoolean(bool v) { return new FhirBoolean(v); }

        /// <summary>
        /// Gets or sets the wire representation of the valuie
        /// </summary>
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if(this.Value != null)
                    return XmlConvert.ToString(this.Value.Value);
                return null;
            }
            set
            {
                if(value != null)
                    base.Value = XmlConvert.ToBoolean(value);
                else
                    base.Value = null;
            }
        }
    }
    /// <summary>
    /// Represents a Uri
    /// </summary>
    [XmlType("uri", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirUri : Primitive<Uri> { 

        /// <summary>
        /// Creates a new wrapped URI object
        /// </summary>
        public FhirUri() : base() { }
        /// <summary>
        /// Creates a new wrapped URI object with specified <paramref name="value"/>
        /// </summary>
        /// <param name="value">The URI value to be wrapped</param>
        public FhirUri(Uri value) : base(value) { }

        /// <summary>
        /// Converts the specified URI to a wrapped URI
        /// </summary>
        /// <param name="v">The URI to be wrapped</param>
        public static implicit operator FhirUri(Uri v) { return new FhirUri(v); }

        /// <summary>
        /// Write as text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            if (this.Value.Scheme == "http" ||
                this.Value.Scheme == "https")
            {
                w.WriteStartElement("a", NS_XHTML);
                w.WriteAttributeString("href", this.Value.ToString());
                w.WriteString(this.Value.ToString());
                w.WriteEndElement(); // a
            }
            else
                w.WriteString(this.Value.ToString());

        }

    }
    /// <summary>
    /// Represents an int
    /// </summary>
    [XmlType("integer")]
    [Serializable]
    public class FhirInt : Primitive<Int32?> {

        /// <summary>
        /// Creates a new FHIR integer wrapper
        /// </summary>
        public FhirInt() : base() { }
        /// <summary>
        /// Creates a new integer wrapper with specified <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to be wrapped</param>
        public FhirInt(Int32 value) : base(value) { }

        /// <summary>
        /// Converts the specified integer to a wrapped integer
        /// </summary>
        /// <param name="v">The integer value to be wrapped</param>
        public static implicit operator FhirInt(int v) { return new FhirInt(v); }

    }
    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("string", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirString : Primitive<String> {

        /// <summary>
        /// Creates a new wrapped string
        /// </summary>
        public FhirString() : base() { }
        /// <summary>
        /// Creates a new wrapped string
        /// </summary>
        /// <param name="value">The value to be wrapped</param>
        public FhirString(String value) : base(value) { }
        
        /// <summary>
        /// Converts a .net string to a wrapped string
        /// </summary>
        /// <param name="v">The string to be wrapped</param>
        public static implicit operator FhirString(string v) { return v == null ? null : new FhirString(v); }

    }

    /// <summary>
    /// Fhir identifier
    /// </summary>
    [XmlType("id", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirId : FhirString
    {
        /// <summary>
        /// Creates a new FHIR identifier
        /// </summary>
        public FhirId() : base() { }

        /// <summary>
        /// Creates a new FHIR identifier with specified <paramref name="id"/>
        /// </summary>
        /// <param name="id">The id value to be set</param>
        public FhirId(String id) : base(id) { }

        /// <summary>
        /// Converts a string to a FHIR identifier
        /// </summary>
        /// <param name="v">The string representing the simple identiifer</param>
        public static implicit operator FhirId(string v) { return v == null ? null : new FhirId(v); }

    }

    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("base64Binary", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirBase64Binary : Primitive<byte[]>
    {
        /// <summary>
        /// Default ctor for base64 binary data
        /// </summary>
        public FhirBase64Binary() : base() { }
        /// <summary>
        /// Constructs a base64 binary data structure from byte array
        /// </summary>
        /// <param name="value"></param>
        public FhirBase64Binary(byte[] value) : base(value) { }

        /// <summary>
        /// Converts a base64 binary structure to a byte array
        /// </summary>
        /// <param name="v">The value to be converted</param>
        public static implicit operator FhirBase64Binary(byte[] v) { return new FhirBase64Binary(v); }

        /// <summary>
        /// Gets or sets the wire representation of the value
        /// </summary>
        [XmlAttribute("value")]
        public override string XmlValue
        {
            get
            {
                if (this.Value != null)
                    return Convert.ToBase64String(this.Value);
                return null;
            }
            set
            {
                if (value != null)
                    this.Value = Convert.FromBase64String(value);
                else
                    this.Value = null;
            }
        }
    }
}
