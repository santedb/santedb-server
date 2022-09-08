/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI;
using System;
using System.Diagnostics;
using System.Security.Permissions;
using SanteDB.Core.Services;
using SanteDB.Core.Security.Services;

namespace SanteDB.Messaging.HDSI.Wcf
{
    /// <summary>
    /// Health Data Service Interface (HDSI)
    /// </summary>
    /// <remarks>Represents SanteDB Server implementation of the the Health Data Service Interface (HDSI) contract</remarks>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class HdsiServiceBehavior : HdsiServiceBehaviorBase
    {
        /// <summary>
        /// RestSrvr doesn't support DI
        /// </summary>
        public HdsiServiceBehavior() : base(ApplicationServiceContext.Current.GetService<IDataCachingService>(), ApplicationServiceContext.Current.GetService<ILocalizationService>(), ApplicationServiceContext.Current.GetService<IPatchService>(), ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(), ApplicationServiceContext.Current.GetService<IBarcodeProviderService>(), ApplicationServiceContext.Current.GetService<IResourcePointerService>())
        {
        }

        /// <summary>
        /// Get resource handler
        /// </summary>
        protected override ResourceHandlerTool GetResourceHandler() => HdsiMessageHandler.ResourceHandler;

        /// <summary>
        /// Create the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData Create(string resourceType, IdentifiedData body)
        {
            return base.Create(resourceType, body);
        }

        /// <summary>
        /// Create or update the specified object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData CreateUpdate(string resourceType, string id, IdentifiedData body)
        {
            return base.CreateUpdate(resourceType, id, body);
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData Get(string resourceType, string id)
        {
            return base.Get(resourceType, id);
        }

        /// <summary>
        /// Gets a specific version of a resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData GetVersion(string resourceType, string id, string versionId)
        {
            return base.GetVersion(resourceType, id, versionId);
        }

        /// <summary>
        /// Gets the recent history an object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData History(string resourceType, string id)
        {
            return base.History(resourceType, id);
        }

        /// <summary>
        /// Perform a search on the specified resource type
        /// </summary>
        [PolicyPermissionAttribute(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData Search(string resourceType)
        {
            return base.Search(resourceType);
        }

        /// <summary>
        /// Update the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData Update(string resourceType, string id, IdentifiedData body)
        {
            return base.Update(resourceType, id, body);
        }

        /// <summary>
        /// Obsolete the specified data
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override IdentifiedData Delete(string resourceType, string id)
        {
            return base.Delete(resourceType, id);
        }

        /// <summary>
        /// Perform the search but only return the headers
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override void HeadSearch(string resourceType)
        {
            base.HeadSearch(resourceType);
        }

        /// <summary>
        /// Get just the headers
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override void Head(string resourceType, string id)
        {
            base.Head(resourceType, id);
        }

        /// <summary>
        /// Perform a patch on the serviceo
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="id"></param>
        /// <param name="body"></param>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override void Patch(string resourceType, string id, Patch body)
        {
            base.Patch(resourceType, id, body);
        }

        /// <summary>
        /// Get options
        /// </summary>
        public override ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            return base.Options();
        }

        /// <summary>
        /// Throw if not ready
        /// </summary>
        public override void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
                throw new DomainStateException();
        }

        /// <summary>
        /// Options resource
        /// </summary>
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            return base.ResourceOptions(resourceType);
        }

        /// <summary>
        /// Copy operation (not supported)
        /// </summary>
        public override IdentifiedData Copy(string reosurceType, string id)
        {
            throw new NotSupportedException("Copy from upstream not supported on iCDR");
        }
    }
}