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
using MARC.HI.EHRS.SVC.Auditing.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Messaging.FHIR.Attributes
{
    /// <summary>
    /// Identifies how a resource maps to a participant object audit
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ParticipantObjectMapAttribute : System.Attribute
    {

        /// <summary>
        /// Gets or sets the Id type
        /// </summary>
        public AuditableObjectIdType IdType { get; set; }

        /// <summary>
        /// Gets or sets the role
        /// </summary>
        public AuditableObjectRole Role { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public AuditableObjectType Type { get; set; }

        /// <summary>
        /// Gets the OID name
        /// </summary>
        public string OidName { get; set; }
    }
}
