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
 * Date: 2018-9-25
 */
using NHapi.Base.Model;
using NHapi.Model.V25.Datatype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Represents message extensions
    /// </summary>
    public static class MessageExtensions
    {

        /// <summary>
        /// Determine if the CS is empty
        /// </summary>
        public static bool IsEmpty(this CX me)
        {
            return me.IDNumber.IsEmpty() && me.AssigningAuthority.IsEmpty();
        }

        /// <summary>
        /// Determine if the HD is empty
        /// </summary>
        public static bool IsEmpty(this HD me)
        {
            return me.NamespaceID.IsEmpty() && me.UniversalID.IsEmpty();
        }

        /// <summary>
        /// Determie if TS is empty
        /// </summary>
        public static bool IsEmpty(this TS me)
        {
            return me.Time.IsEmpty();
        }

        /// <summary>
        /// Determines if an abstract primitive is empty
        /// </summary>
        public static bool IsEmpty(this AbstractPrimitive me)
        {
            return String.IsNullOrEmpty(me.Value);
        }

        /// <summary>
        /// Determines if the code is empty
        /// </summary>
        public static bool IsEmpty(this CE me)
        {
            return me.Identifier.IsEmpty() && me.AlternateIdentifier.IsEmpty();
        }
    }
}
