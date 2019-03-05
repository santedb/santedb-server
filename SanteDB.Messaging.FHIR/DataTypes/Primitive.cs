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
using System;
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
                this.Value = MARC.Everest.Connectors.Util.Convert<T>(value);
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
        public FhirDecimal() : base() { }
        public FhirDecimal(Decimal value) : base(value) { }
        public static implicit operator FhirDecimal(Decimal v) { return new FhirDecimal(v); }
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
        public FhirBoolean() : base() { }
        public FhirBoolean(Boolean value) : base(value) { }
        public static implicit operator FhirBoolean(bool v) { return new FhirBoolean(v); }
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
        public FhirUri() : base() { }
        public FhirUri(Uri value) : base(value) { }
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

        public FhirInt() : base() { }
        public FhirInt(Int32 value) : base(value) { }
        public static implicit operator FhirInt(int v) { return new FhirInt(v); }

    }
    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("string", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirString : Primitive<String> {
        public FhirString() : base() { }
        public FhirString(String value) : base(value) { }
        public static implicit operator FhirString(string v) { return v == null ? null : new FhirString(v); }

    }

    /// <summary>
    /// Fhir identifier
    /// </summary>
    [XmlType("id", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirId : FhirString
    {
        public FhirId() : base() { }
        public FhirId(String id) : base(id) { }
        public static implicit operator FhirId(string v) { return v == null ? null : new FhirId(v); }

    }

    /// <summary>
    /// Represents a string
    /// </summary>
    [XmlType("base64Binary", Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirBase64Binary : Primitive<byte[]>
    {
        public FhirBase64Binary() : base() { }
        public FhirBase64Binary(byte[] value) : base(value) { }
        public static implicit operator FhirBase64Binary(byte[] v) { return new FhirBase64Binary(v); }
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
