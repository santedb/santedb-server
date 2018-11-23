using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Globalization;
using System.Xml;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Date precisions
    /// </summary>
    public enum DatePrecision
    {
        Unspecified = 0,
        Year = 4,
        Month = 7, 
        Day = 10,
        Full = 28
    }

    /// <summary>
    /// Date
    /// </summary>
    [XmlType("dateTime", Namespace = "http://hl7.org/fhir")]
    public class FhirDateTime : FhirString
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public FhirDateTime() { }

        /// <summary>
        /// Create a date 
        /// </summary>
        public FhirDateTime(DateTime value)
            : base()
        {
            this.DateValue = value;
        }

        /// <summary>
        /// Gets the precision of the date
        /// </summary>
        [XmlIgnore]
        public DatePrecision Precision { get; set; }

        /// <summary>
        /// Start time
        /// </summary>
        [XmlIgnore()]
        public DateTime? DateValue
        {
            get;
            set;
        }

        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator DateTime(FhirDateTime v)
        {
            return v.DateValue.Value;
        }

        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator FhirDateTime(DateTime v)
        {
            return new FhirDateTime(v) ;
        }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [XmlIgnore]
        public override string Value
        {
            get
            {

                string dateFormat = @"yyyy-MM-dd\THH:mm:ss.ffffzzz";
                if (this.Precision == DatePrecision.Unspecified)
                    this.Precision = DatePrecision.Full;
                dateFormat = dateFormat.Substring(0, (int)this.Precision);
                return this.DateValue.Value.ToString(dateFormat);
            }
            set
            {
                if (value != null)
                {
                    string dateFormat = @"yyyy-MM-dd\THH:mm:ss.ffffzzz";
                    this.Precision = (DatePrecision)value.Length;
                    if (this.Precision > DatePrecision.Full)
                        this.Precision = DatePrecision.Full;

                    // Correct parse
                    
                    if (this.Precision == DatePrecision.Year)
                        this.DateValue = XmlConvert.ToDateTime(value, "yyyy");
                    else
                        this.DateValue = XmlConvert.ToDateTime(value);

                }
                else
                    this.DateValue = XmlConvert.ToDateTime(value);
            }
        }

        /// <summary>
        /// Date string
        /// </summary>
        public override string ToString()
        {
            return this.XmlValue;
        }
    }

    /// <summary>
    /// Date only
    /// </summary>
    [XmlType("date", Namespace = "http://hl7.org/fhir")]
    public class FhirDate : FhirDateTime
    {
        /// <summary>
        /// reate FHIR date
        /// </summary>
        public FhirDate()
        {

        }

        /// <summary>
        /// Create FHIR date
        /// </summary>
        public FhirDate(DateTime dt) : base(dt)
        {

        }
        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator DateTime(FhirDate v)
        {
            return v.DateValue.Value;
        }

        /// <summary>
        /// Convert this date to a date
        /// </summary>
        public static implicit operator FhirDate(DateTime v)
        {
            return new FhirDate(v);
        }

        /// <summary>
        /// Only date is permitted
        /// </summary>
        [XmlIgnore]
        public override string Value
        {
            get
            {
                if (base.Precision > DatePrecision.Day)
                    this.Precision = DatePrecision.Day;
                return base.Value;
            }
            set
            {
                base.Value = value;
                if (base.Precision > DatePrecision.Day)
                    this.Precision = DatePrecision.Day;
            }
        }
    }
}
