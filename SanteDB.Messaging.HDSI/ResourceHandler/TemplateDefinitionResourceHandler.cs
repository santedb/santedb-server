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
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.DataTypes;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
    /// <summary>
    /// Template definition resource handler
    /// </summary>
    public class TemplateDefinitionResourceHandler : IResourceHandler
    {
        /// <summary>
        /// Get capabilities 
        /// </summary>
        public ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Search;
            }
        }

        /// <summary>
        /// Gets the scope of this handler
        /// </summary>
        public Type Scope => typeof(Wcf.IHdsiServiceContract);

        /// <summary>
        /// Get the resource name
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "TemplateDefinition";
            }
        }

        /// <summary>
        /// Get the type
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(TemplateDefinition);
            }
        }

        /// <summary>
        /// Create not supported
        /// </summary>
        public Object Create(Object data, bool updateIfExists)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get not supported
        /// </summary>
        public Object Get(object id, object versionId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obsolete not supported
        /// </summary>
        public Object Obsolete(object  key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query the template definitions
        /// </summary>
        public IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query the properties
        /// </summary>
        public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var metaService = ApplicationContext.Current.GetService<IMetadataRepositoryService>();
            if (metaService == null)
                throw new InvalidOperationException("Cannot locate metadata repository");
            else
                return metaService.FindTemplateDefinitions(QueryExpressionParser.BuildLinqExpression<TemplateDefinition>(queryParameters), offset, count, out totalCount);
        }

        public Object Update(Object  data)
        {
            throw new NotImplementedException();
        }
    }
}
