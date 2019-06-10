/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

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
        public FhirOperationResult Create(ResourceBase target, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Delete
        /// </summary>
        public FhirOperationResult Delete(string id, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the resource definition
        /// </summary>
        public ResourceDefinition GetResourceDefinition()
        {
            return new ResourceDefinition()
            {
                ConditionalCreate = false,
                ConditionalDelete = ConditionalDeleteStatus.NotSupported,
                ConditionalUpdate = false,
                Interaction = new List<InteractionDefinition>()
                {
                    new InteractionDefinition()
                    {
                        Type = TypeRestfulInteraction.Read
                    },
                    new InteractionDefinition()
                    {
                        Type = TypeRestfulInteraction.VersionRead
                    },
                    new InteractionDefinition()
                    {
                        Type = TypeRestfulInteraction.Search
                    }
                },
                Type = "StructureDefinition",
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
        /// Query for the specified search structure definition
        /// </summary>
        public FhirQueryResult Query(NameValueCollection parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read the specified structure definition
        /// </summary>
        public FhirOperationResult Read(string id, string versionId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update
        /// </summary>
        public FhirOperationResult Update(string id, ResourceBase target, Core.Services.TransactionMode mode)
        {
            throw new NotSupportedException();
        }
    }
}
