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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Util;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Organization resource provider
    /// </summary>
    public class OrganizationResourceHandler : RepositoryResourceHandlerBase<Hl7.Fhir.Model.Organization, SanteDB.Core.Model.Entities.Organization>, IBundleResourceHandler
    {

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(Resource bundleResource, RestOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource as Hl7.Fhir.Model.Organization, context);
        }

        /// <summary>
        /// Get the interactions 
        /// </summary>
        protected override IEnumerable<ResourceInteractionComponent> GetInteractions() =>
            new TypeRestfulInteraction[]
            {
                TypeRestfulInteraction.Vread,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.SearchType,
                TypeRestfulInteraction.HistoryInstance
            }.Select(o => new ResourceInteractionComponent() { Code = o });

        /// <summary>
        /// Map to FHIR
        /// </summary>
        protected override Hl7.Fhir.Model.Organization MapToFhir(Core.Model.Entities.Organization model, RestOperationContext webOperationContext)
        {
            return DataTypeConverter.CreateResource<Hl7.Fhir.Model.Organization>(model, webOperationContext);
        }

        /// <summary>
        /// Map to Model
        /// </summary>
        protected override Core.Model.Entities.Organization MapToModel(Hl7.Fhir.Model.Organization resource, RestOperationContext webOperationContext)
        {
            // Organization
            var retVal = new Core.Model.Entities.Organization()
            {
                TypeConcept = resource.Type.Select(o => DataTypeConverter.ToConcept(o)).OfType<Concept>().FirstOrDefault(),
                Addresses = resource.Address.Select(DataTypeConverter.ToEntityAddress).ToList(),
                CreationTime = DateTimeOffset.Now,
                // TODO: Extensions
                Identifiers = resource.Identifier.Select(DataTypeConverter.ToEntityIdentifier).ToList(),
                Key = Guid.NewGuid(),
                Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, resource.Name) },
                StatusConceptKey = !resource.Active.HasValue || resource.Active == true ? StatusKeys.Active : StatusKeys.Obsolete,
                Telecoms = resource.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).OfType<EntityTelecomAddress>().ToList()
            };
            retVal.Extensions = resource.Extension.Select(o => DataTypeConverter.ToEntityExtension(o, retVal)).OfType<EntityExtension>().ToList();

            if (!Guid.TryParse(resource.Id, out Guid key))
            {
                key = Guid.NewGuid();
            }
            retVal.Key = key;

            return retVal;
        }
    }
}
