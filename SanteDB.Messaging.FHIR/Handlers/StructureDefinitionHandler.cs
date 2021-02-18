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
 * Date: 2019-11-27
 */
using Hl7.Fhir.Model;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents the default StructureDefinition handler
    /// </summary>
    public class StructureDefinitionHandler : IFhirResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "StructureDefinition";
            }
        }

        /// <summary>
        /// Create the specified definition
        /// </summary>
        public Resource Create(Resource target, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete
        /// </summary>
        public Resource Delete(string id, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the resource definition
        /// </summary>
        public ResourceComponent GetResourceDefinition()
        {
            return new ResourceComponent()
            {
                ConditionalCreate = false,
                ConditionalDelete = ConditionalDeleteStatus.NotSupported,
                ConditionalUpdate = false,
                Interaction = new List<ResourceInteractionComponent>()
                {
                    new ResourceInteractionComponent()
                    {
                        Code = TypeRestfulInteraction.Read
                    },
                    new ResourceInteractionComponent()
                    {
                        Code = TypeRestfulInteraction.Vread
                    },
                    new ResourceInteractionComponent()
                    {
                        Code = TypeRestfulInteraction.SearchType
                    }
                },
                Type = ResourceType.StructureDefinition,
                ReadHistory = true,
                UpdateCreate = false,
                Versioning = ResourceVersionPolicy.Versioned
            };
        }

        /// <summary>
        /// Get structure definition
        /// </summary>
        public StructureDefinition GetStructureDefinition()
        {
            return typeof(StructureDefinition).GetStructureDefinition(false);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        public FhirQueryResult History(string id)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for the specified search structure definition
        /// </summary>
        public FhirQueryResult Query(NameValueCollection parameters)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Read the specified structure definition
        /// </summary>
        public Resource Read(string id, string versionId)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Update
        /// </summary>
        public Resource Update(string id, Resource target, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }
    }
}
