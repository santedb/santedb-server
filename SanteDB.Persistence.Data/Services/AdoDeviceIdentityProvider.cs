using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An implementation of the device identity provider 
    /// </summary>
    public class AdoDeviceIdentityProvider : IDeviceIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Databased Device Identity Provider";

        /// <summary>
        /// Fired after authenticating a device principal is complete
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;
        /// <summary>
        /// Fired prior to authenticating a device principal
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoDeviceIdentityProvider));

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Security configuration 
        private SecurityConfigurationSection m_securityConfiguration;

        // Pep service
        private IPolicyEnforcementService m_pepService;

        // Hasher
        private IPasswordHashingService m_hasher;

        /// <summary>
        /// Creates a new application identity provider
        /// </summary>
        public AdoDeviceIdentityProvider(IConfigurationManager configurationManager,
            IPasswordHashingService hashingService,
            IPolicyEnforcementService pepService)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configurationManager.GetSection<SecurityConfigurationSection>();
            this.m_hasher = hashingService;
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Authenticates the specified device identity
        /// </summary>
        /// <param name="deviceId">The public identifier of the device</param>
        /// <param name="deviceSecret">The secret of the device , either a plantext secret or a certificate thumbprint</param>
        /// <param name="authMethod">The authentication method used</param>
        /// <returns></returns>
        public IPrincipal Authenticate(string deviceId, string deviceSecret, AuthenticationMethod authMethod = AuthenticationMethod.Any)
        {
            if (String.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (String.IsNullOrEmpty(deviceSecret))
            {
                throw new ArgumentNullException(nameof(deviceSecret), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if(!authMethod.HasFlag(AuthenticationMethod.Local))
            {
                throw new NotSupportedException(ErrorMessages.ERR_AUTH_DEV_LOCAL_ONLY_SUPPORTED);
            }

            var preAuthArgs = new AuthenticatingEventArgs(deviceId);
            this.Authenticating?.Invoke(this, preAuthArgs);
            if (preAuthArgs.Cancel)
            {
                if (preAuthArgs.Success)
                {
                    return preAuthArgs.Principal;
                }
                else
                {
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_CANCELLED);
                }
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    var dev = context.FirstOrDefault<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == deviceId.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (dev == null)
                    {
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_DEV_INVALID);
                    }

                    if (dev.Lockout.HasValue && dev.Lockout.Value > DateTimeOffset.Now)
                    {
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_DEV_LOCKED);
                    }

                    // Peppered authentication
                    if (!this.m_configuration.GetPepperCombos(deviceSecret).Any(p=>this.m_hasher.ComputeHash(p) == deviceSecret))
                    {
                        dev.InvalidAuthAttempts++;
                        if (dev.InvalidAuthAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins))
                        {
                            var lockoutSlide = 30 * dev.InvalidAuthAttempts.Value;
                            if (dev.Lockout < DateTimeOffset.MaxValue.AddSeconds(-lockoutSlide))
                            {
                                dev.Lockout = DateTimeOffset.Now.AddSeconds(lockoutSlide);
                            }
                        }
                        context.Update(dev);
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_APP_INVALID);
                    }

                    dev.LastAuthentication = DateTimeOffset.Now;

                    dev.DeviceSecret = this.m_configuration.AddPepper(deviceSecret);

                    var retVal = new AdoClaimsPrincipal(new AdoDeviceIdentity(dev, "LOCAL"));

                    // demand login
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.LoginAsService, retVal);

                    context.Update(dev);

                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(deviceId, retVal, true));
                    return retVal;
                }
                catch (AuthenticationException)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(deviceId, null, false));
                    throw;
                }
                catch (Exception e)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(deviceId, null, false));
                    this.m_tracer.TraceError("Could not authenticated device {0}- {1]", deviceId, e);
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_DEV_GENERAL, e);
                }
            }

        }

        /// <summary>
        /// Chenge the device principal secret
        /// </summary>
        public void ChangeSecret(string name, string deviceSecret, IPrincipal systemPrincipal)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (String.IsNullOrEmpty(deviceSecret))
            {
                throw new ArgumentNullException(nameof(deviceSecret), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (systemPrincipal == null)
            {
                throw new ArgumentNullException(nameof(systemPrincipal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            if (systemPrincipal.Identity.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                this.m_pepService.Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ?
                    PermissionPolicyIdentifiers.AlterIdentity: PermissionPolicyIdentifiers.AlterLocalIdentity, systemPrincipal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dev = context.FirstOrDefault<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (dev == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND);
                    }

                   
                    dev.DeviceSecret = this.m_configuration.AddPepper(deviceSecret);
                    dev.UpdatedByKey = context.EstablishProvenance(systemPrincipal, null);
                    dev.UpdatedTime = DateTimeOffset.Now;
                    context.Update(dev);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating secret for device {0} - {1}", name, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_UPDATE_SECRET, e);
                }
            }
        }

        /// <summary>
        /// Gets an unauthenticated device identity for the specified name
        /// </summary>
        public IDeviceIdentity GetIdentity(string name)
        {
            if(!String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    var dev = context.FirstOrDefault<DbSecurityDevice>(d => d.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && d.ObsoletionTime == null);
                    if(dev == null)
                    {
                        return null;
                    }
                    return new AdoDeviceIdentity(dev);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching identity for {0}", name);
                    throw new DataPersistenceException(ErrorMessages.ERR_FETCH_APPLICATION, e);
                }
            }
        }

        /// <summary>
        /// Sets the lockout state of the device
        /// </summary>
        public void SetLockout(string name, bool lockoutState, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_pepService.Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ?
                    PermissionPolicyIdentifiers.AlterIdentity : PermissionPolicyIdentifiers.AlterLocalIdentity, principal);
        }

        /// <summary>
        /// Create a new device identity
        /// </summary>
        public IDeviceIdentity CreateIdentity(string deviceId, string secret, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the security identifier
        /// </summary>
        public Guid GetSid(string name)
        {
            throw new NotImplementedException();
        }
    }
}
