using SanteDB.Configuration.Editors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configuration.Converters
{
    /// <summary>
    /// Represents a type converter for the provider type
    /// </summary>
    public class DataProviderConverter : TypeConverter
    {

        /// <summary>
        /// Can convert
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(String);
        }

        /// <summary>
        /// Convert to display 
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is string)
                return new DataProviderWrapper(ConfigurationContext.Current.DataProviders.FirstOrDefault(o => o.DbProviderType == Type.GetType(value.ToString())))?.Provider.Name;
            else
                return value;
        }

    }
}
