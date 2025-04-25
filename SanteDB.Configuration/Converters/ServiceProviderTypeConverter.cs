/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Services;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace SanteDB.Configuration.Converters
{
    /// <summary>
    /// Service provider type converter
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceProviderTypeConverter : TypeConverter
    {
        /// <summary>
        /// Can convert from
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(Type);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Convert to 
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is Type)
            {
                return (value as Type)?.GetCustomAttribute<ServiceProviderAttribute>()?.Name ?? (value as Type)?.Name;
            }

            if (value is string)
            {
                var t = Type.GetType(value.ToString());
                return t?.GetCustomAttribute<ServiceProviderAttribute>()?.Name ?? t?.Name;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}