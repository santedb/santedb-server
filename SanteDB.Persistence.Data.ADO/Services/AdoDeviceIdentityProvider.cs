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
 * Date: 2018-6-22
 */
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Diagnostics;
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
        private readonly TraceSource traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

		/// <summary>
		/// The configuration.
		/// </summary>
		private readonly AdoPersistenceConfigurationSection configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

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

			using (var dataContext = this.configuration.Provider.GetWriteConnection())
			{
				try
				{
					dataContext.Open();

					var hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                    // TODO - Allow configuation of max login attempts
					var client = dataContext.FirstOrDefault<DbSecurityDevice>("auth_dev", deviceId, hashService.ComputeHash(deviceSecret), 5);

                    if (client == null)
                        throw new SecurityException("Invalid device credentials");
                    else if (client.Key == Guid.Empty)
                        throw new AuthenticationException(client.PublicId);

					IPrincipal devicePrincipal = new DevicePrincipal(new DeviceIdentity(client.Key, client.PublicId, true));

					new PolicyPermission(System.Security.Permissions.PermissionState.None, PermissionPolicyIdentifiers.LoginAsService, devicePrincipal).Demand();

					return devicePrincipal;
				}
				catch (Exception e)
				{
					this.traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error authenticating {0} : {1}", deviceId, e);
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
			using (var dataContext = this.configuration.Provider.GetReadonlyConnection())
			{
				try
				{
					dataContext.Open();

					var client = dataContext.FirstOrDefault<DbSecurityDevice>(o => o.PublicId == name);

					return new DeviceIdentity(client.Key, client.PublicId, false);

				}
				catch (Exception e)
				{
					this.traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error getting identity data for {0} : {1}", name, e);
					throw;
				}
			}
		}
	}
}
