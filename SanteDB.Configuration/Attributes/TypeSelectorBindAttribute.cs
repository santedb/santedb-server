/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2020-1-12
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configuration.Attributes
{
    /// <summary>
    /// Type selection bind attribute
    /// </summary>
    public class TypeSelectorBindAttribute : Attribute
    {

        /// <summary>
        /// Creates a new type selector bind
        /// </summary>
        public TypeSelectorBindAttribute(Type bindType)
        {
            this.BindType = bindType;
        }

        /// <summary>
        /// Gets or sets the binding type for the dropdown
        /// </summary>
        public Type BindType { get; set; }

    }
}
