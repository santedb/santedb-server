using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configuration.Converters
{
    /// <summary>
    /// Service provider type converter
    /// </summary>
    public class ServiceProviderTypeConverter : TypeConverter
    {

        /// <summary>
        /// Can convert from
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(String) || sourceType == typeof(Type);
        }

        /// <summary>
        /// Convert to 
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Type)
                return (value as Type)?.GetCustomAttribute<ServiceProviderAttribute>()?.Name ?? (value as Type)?.Name;
            else if(value is String)
            {
                var t = Type.GetType(value.ToString());
                return t?.GetCustomAttribute<ServiceProviderAttribute>()?.Name ?? t?.Name;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
