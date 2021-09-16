/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Server.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a device identity provider.
    /// </summary>
    [ServiceProvider("ADO.NET Device Identity Provider")]
    public class AdoDeviceIdentityProvider : IDeviceIdentityProviderService
	{
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "ADO.NET Device Identity Provider";

        /// <summary>
        /// The trace source.
        /// </summary>
        private readonly Tracer m_tracer = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        // Policy service
        private IPolicyEnforcementService m_policyService;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoDeviceIdentityProvider(IConfigurationManager configurationManager, IPolicyEnforcementService pepService)
        {
            this.m_policyService = pepService;
            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>(); 
        }

		/// <summary>
		/// Fired after an authentication request has been made.
		/// </summary>
		public event EventHandler<AuthenticatedEventArgs> Authenticated;
		/// <summary>
		/// Fired prior to an authentication request being made.
		/// </summary>
		public event EventHandler<AuthenticatingEventArgs> Authenticating;

		/// <summary>
		/// Authenticates the specified device identifier.
		/// </summary>
		/// <param name="deviceId">The device identifier.</param>
		/// <param name="deviceSecret">The device secret.</param>
		/// <returns>Returns the authenticated device principal.</returns>
		public IPrincipal Authenticate(string deviceId, string deviceSecret, AuthenticationMethod authMethod = AuthenticationMethod.Any)
		{
            if (!authMethod.HasFlag(AuthenticationMethod.Local))
                throw new InvalidOperationException("ADO.NET provider only supports local authentication");

			using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
			{
				try
				{
					dataContext.Open();

					var hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                    // TODO - Allow configuation of max login attempts
					var client = dataContext.ExecuteProcedure<DbSecurityDevice>("auth_dev", deviceId, hashService.ComputeHash(deviceSecret), 5);

                    if (client == null)
                        throw new SecurityException("Invalid device credentials");
                    else if (client.Key == Guid.Empty)
                        throw new AuthenticationException(client.PublicId);

					IPrincipal devicePrincipal = new DevicePrincipal(new DeviceIdentity(client.Key, client.PublicId, true));

					this.m_policyService.Demand(PermissionPolicyIdentifiers.LoginAsService, devicePrincipal);

					return devicePrincipal;
				}
				catch (Exception e)
				{
					this.m_tracer.TraceEvent(EventLevel.Error,  "Error authenticating {0} : {1}", deviceId, e);
					throw new AuthenticationException("Error authenticating application", e);
				}
			}
		}

		/// <summary>
		/// Authenticate the device based on certificate provided
		/// </summary>
		/// <param name="deviceCertificate">The certificate of the device used to authenticate the device.</param>
		/// <returns>Returns the authenticated device principal.</returns>
		public IPrincipal Authenticate(X509Certificate2 deviceCertificate)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the identity of the device using a given device name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>Returns the identity of the device.</returns>
		public IIdentity GetIdentity(string name)
		{
			using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
			{
				try
				{
					dataContext.Open();

					var client = dataContext.FirstOrDefault<DbSecurityDevice>(o => o.PublicId == name);

                    if (client == null)
                        return null;
                    else
					    return new DeviceIdentity(client.Key, client.PublicId, false);

				}
				catch (Exception e)
				{
					this.m_tracer.TraceEvent(EventLevel.Error,  "Error getting identity data for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error getting identity {name}", e);
				}
			}
		}

        /// <summary>
        /// Set the lockout for the specified device
        /// </summary>
        public void SetLockout(string name, bool lockoutState, IPrincipal principal)
        {
            using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();
                    var provId = dataContext.EstablishProvenance(principal, null);

                    var dev = dataContext.FirstOrDefault<DbSecurityDevice>(o => o.PublicId == name);
                    if (dev == null)
                        throw new KeyNotFoundException($"Device {name} not found");
                    dev.Lockout = lockoutState ? (DateTimeOffset?)DateTimeOffset.MaxValue.AddDays(-10) : null;
                    dev.LockoutSpecified = true;
                    dev.UpdatedByKey = provId;
                    dev.UpdatedTime = DateTimeOffset.Now;
                    dataContext.Update(dev);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error getting identity data for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error setting lockout for {name}", e);
                }
        }

        /// <summary>
        /// Change the device secret
        /// </summary>
        public void ChangeSecret(string name, string deviceSecret, IPrincipal principal)
        {
            using (var dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();
                    var provId = dataContext.EstablishProvenance(principal, null);

                    var dev = dataContext.FirstOrDefault<DbSecurityDevice>(o => o.PublicId == name);
                    if (dev == null)
                        throw new KeyNotFoundException($"Device {name} not found");

                    var phash = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
                    if (phash == null)
                        throw new InvalidOperationException("Cannot find password hashing service");

                    dev.UpdatedByKey = provId;
                    dev.UpdatedTime = DateTimeOffset.Now;
                    dev.DeviceSecret = phash.ComputeHash(deviceSecret);
                    dataContext.Update(dev);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error setting secret identity data for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error canging secret for {name}", e);
                }
        }

        public IDeviceIdentity CreateIdentity(string deviceId, string secret, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public Guid GetSid(string name)
        {
            throw new NotImplementedException();
        }

        IDeviceIdentity IDeviceIdentityProviderService.GetIdentity(string name)
        {
            throw new NotImplementedException();
        }

        public void DeleteIdentity(string name, IPrincipal principal)
        {
            throw new NotImplementedException();
        }
    }
}
