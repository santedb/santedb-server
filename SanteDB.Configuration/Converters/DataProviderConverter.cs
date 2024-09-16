/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using SanteDB.Configuration.Editors;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace SanteDB.Configuration.Converters
{
    /// <summary>
    /// Represents a type converter for the provider type
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataProviderConverter : TypeConverter
    {
        /// <summary>
        /// Can convert
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <summary>
        /// Convert to display 
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is string)
            {
                return new DataProviderWrapper(ConfigurationContext.Current.DataProviders.FirstOrDefault(o => o.DbProviderType == Type.GetType(value.ToString()) || o.Invariant.Equals(value.ToString(), StringComparison.OrdinalIgnoreCase)))?.Provider.Name;
            }

            return value;
        }
    }
}