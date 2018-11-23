/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-23
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive code value
    /// </summary>
    [XmlType(Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirCode<T> : Primitive<T>
    {

        /// <summary>
        /// Constructs a new instance of code
        /// </summary>
        public FhirCode()
        {

        }

        /// <summary>
        /// Creates a new instance of the code
        /// </summary>
        public FhirCode(T code) : base(code)
        {
        }


        /// <summary>
        /// Create fhir code from prinitive
        /// </summary>
        /// <param name="v"></param>
        public static implicit operator FhirCode<T>(T v)
        {
            return new FhirCode<T>(v);
        }
    }
}
