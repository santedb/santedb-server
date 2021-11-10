/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * User: fyfej
 * Date: 2021-8-27
 */

using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Test contexts don't really have applet configurations
    /// </summary>
    public class TestLocalizationService : ILocalizationService
    {
        /// <summary>
        /// Get the service name
        /// </summary>
        public string ServiceName => "Testing Context Localization";

        /// <summary>
        /// Format string
        /// </summary>
        public string GetString(string stringKey, dynamic parameters)
        {
            return this.GetString(null, stringKey, parameters);
        }

        /// <summary>
        /// Format string
        /// </summary>
        public string GetString(string locale, string stringKey, dynamic parameters)
        {
            var values = (TypeDescriptor.GetProperties(parameters) as PropertyDescriptorCollection).OfType<PropertyDescriptor>().Select(o => $"{o.Name} = {o.GetValue(parameters)}");
            return $"{stringKey} ({String.Join(";", values)})";
        }

        /// <summary>
        /// Get string
        /// </summary>
        public string GetString(string stringKey)
        {
            return stringKey;
        }

        /// <summary>
        /// Get string in locale
        /// </summary>
        public string GetString(string locale, string stringKey)
        {
            return stringKey;
        }

        /// <summary>
        /// Get all strings
        /// </summary>
        public KeyValuePair<string, string>[] GetStrings(string locale) => new KeyValuePair<string, string>[0];

        /// <summary>
        /// Reload strings
        /// </summary>
        public void Reload()
        {
            return;
        }
    }
}