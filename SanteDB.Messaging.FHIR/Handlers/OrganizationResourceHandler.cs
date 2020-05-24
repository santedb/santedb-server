/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Organization resource provider
    /// </summary>
    public class OrganizationResourceHandler : RepositoryResourceHandlerBase<SanteDB.Messaging.FHIR.Resources.Organization, SanteDB.Core.Model.Entities.Organization>, IBundleResourceHandler
    {

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(BundleEntry bundleResource, RestOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource.Resource.Resource as SanteDB.Messaging.FHIR.Resources.Organization, context);
        }

        /// <summary>
        /// Get the interactions 
        /// </summary>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Map to FHIR
        /// </summary>
        protected override SanteDB.Messaging.FHIR.Resources.Organization MapToFhir(Core.Model.Entities.Organization model, RestOperationContext webOperationContext)
        {
            return DataTypeConverter.CreateResource<SanteDB.Messaging.FHIR.Resources.Organization>(model);
        }

        /// <summary>
        /// Map to Model
        /// </summary>
        protected override Core.Model.Entities.Organization MapToModel(SanteDB.Messaging.FHIR.Resources.Organization resource, RestOperationContext webOperationContext)
        {
            // Organization
            var retVal = new Core.Model.Entities.Organization()
            {
                TypeConcept = DataTypeConverter.ToConcept(resource.Type),
                Addresses = resource.Address.Select(DataTypeConverter.ToEntityAddress).ToList(),
                CreationTime = DateTimeOffset.Now,
                // TODO: Extensions
                Extensions = resource.Extension.Select(DataTypeConverter.ToEntityExtension).OfType<EntityExtension>().ToList(),
                Identifiers = resource.Identifier.Select(DataTypeConverter.ToEntityIdentifier).ToList(),
                Key = Guid.NewGuid(),
                Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, resource.Name) },
                StatusConceptKey = resource.Active?.Value == true ? StatusKeys.Active : StatusKeys.Obsolete,
                Telecoms = resource.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).ToList()
            };

            Guid key;
            if (!Guid.TryParse(resource.Id, out key))
            {
                key = Guid.NewGuid();
            }

            retVal.Key = key;

            return retVal;
        }
    }
}
