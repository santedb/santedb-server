﻿using SanteDB.Core;
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
    /// Application identity provider that uses the database to authenticate applications
    /// </summary>
    public class AdoApplicationIdentityProvider : IApplicationIdentityProviderService
    {
        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "Databased Application Identity Provider";

        /// <summary>
        /// Fired when the service has authenticated an application
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Fired when the service is authenticating the application
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoApplicationIdentityProvider));

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Security configuration 
        private SecurityConfigurationSection m_securityConfiguration;

        // Pep service
        private IPolicyEnforcementService m_pepService;

        // Symm provider
        private ISymmetricCryptographicProvider m_symmetricCryptographicProvider;

        // Hasher
        private IPasswordHashingService m_hasher;

        /// <summary>
        /// Creates a new application identity provider
        /// </summary>
        public AdoApplicationIdentityProvider(IConfigurationManager configurationManager,
            IPasswordHashingService hashingService,
            IPolicyEnforcementService pepService,
            ISymmetricCryptographicProvider symmetricCryptographicProvider)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configurationManager.GetSection<SecurityConfigurationSection>();
            this.m_hasher = hashingService;
            this.m_pepService = pepService;
            this.m_symmetricCryptographicProvider = symmetricCryptographicProvider;
        }

        /// <summary>
        /// Authenticates the specified application using the application id an secret 
        /// </summary>
        /// <param name="applicationId">The application public id</param>
        /// <param name="applicationSecret">The secret passphrase for the application</param>
        /// <returns>The principal if authentication is successful</returns>
        /// <exception cref="AuthenticationException">When authentication fails</exception>
        public IPrincipal Authenticate(string applicationId, string applicationSecret)
        {
            if (String.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (String.IsNullOrEmpty(applicationSecret))
            {
                throw new ArgumentNullException(nameof(applicationSecret), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Notify login
            var preAuthenticateArgs = new AuthenticatingEventArgs(applicationId);
            this.Authenticating?.Invoke(this, preAuthenticateArgs);
            if (preAuthenticateArgs.Cancel)
            {
                // Did the callee override?
                if (preAuthenticateArgs.Success)
                {
                    return preAuthenticateArgs.Principal;
                }
                else
                {
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_CANCELLED);
                }
            }

            // Connect and authenticate
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    // Query for application matching application ID
                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == applicationId.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (app == null)
                    {
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_APP_INVALID);
                    }

                    // Locked?
                    if (app.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                    {
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_APP_LOCKED);
                    }

                    var pepperSecret = this.m_configuration.GetPepperCombos(applicationSecret).Select(o => this.m_hasher.ComputeHash(o));
                    // Pepper authentication
                    if (!context.Any<DbSecurityApplication>(a=>a.PublicId.ToLowerInvariant() == applicationId.ToLower() && pepperSecret.Contains(a.Secret)))
                    {
                        app.InvalidAuthAttempts = app.InvalidAuthAttempts.GetValueOrDefault() + 1;

                        if (app.InvalidAuthAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins))
                        {
                            var lockoutSlide = 30 * app.InvalidAuthAttempts.Value;
                            if (DateTimeOffset.Now < DateTimeOffset.MaxValue.AddSeconds(-lockoutSlide))
                            {
                                app.Lockout = DateTimeOffset.Now.AddSeconds(lockoutSlide);
                            }
                        }
                        context.Update(app);
                        throw new AuthenticationException(ErrorMessages.ERR_AUTH_APP_INVALID);
                    }

                    // Re-pepper the password 
                    app.LastAuthentication = DateTimeOffset.Now;
                    app.Secret = this.m_hasher.ComputeHash( this.m_configuration.AddPepper(applicationSecret));


                    // Construct an identity and login
                    var retVal = new AdoClaimsPrincipal(new AdoApplicationIdentity(app, "LOCAL"));

                    // Demand login as a service 
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.LoginAsService, retVal);

                    context.Update(app);

                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(applicationId, retVal, true));

                    return retVal;
                }
                catch (AuthenticationException)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(applicationId, null, false));
                    throw;
                }
                catch (Exception e)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(applicationId, null, false));
                    this.m_tracer.TraceError("Could not authenticate application {0} - {1}", applicationId, e);
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_APP_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Change the specified application identity's secret
        /// </summary>
        /// <param name="name">The name of the application to change the secret for</param>
        /// <param name="secret">The new secret</param>
        /// <param name="principal">The principal which is seeking the change</param>
        public void ChangeSecret(string name, string secret, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (String.IsNullOrEmpty(secret))
            {
                throw new ArgumentNullException(nameof(secret), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Rule - Application can change its own password or ALTER_IDENTITY
            if (!principal.Identity.IsAuthenticated || !principal.Identity.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                this.m_pepService.Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ?
                    PermissionPolicyIdentifiers.AlterIdentity : PermissionPolicyIdentifiers.AlterLocalIdentity, principal);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (app == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND);
                    }

                    app.Secret = this.m_hasher.ComputeHash(this.m_configuration.AddPepper(secret));
                    app.UpdatedByKey = context.EstablishProvenance(principal, null);
                    app.UpdatedTime = DateTimeOffset.Now;

                    context.Update(app);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating secret for {0} - {1}", name, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_UPDATE_SECRET, e);
                }
            }
        }

        /// <summary>
        /// Get the specified identity 
        /// </summary>
        /// <param name="name">The name of the identity to retrieve</param>
        /// <returns>The constructed, unauthenticated identity</returns>
        public IApplicationIdentity GetIdentity(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (app == null)
                    {
                        return null;
                    }
                    return new AdoApplicationIdentity(app);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching identity for {0}", name);
                    throw new DataPersistenceException(ErrorMessages.ERR_FETCH_APPLICATION, e);
                }
            }
        }

        /// <summary>
        /// Get the secure key for the application
        /// </summary>
        /// <param name="name">The name of the applicationto fetch</param>
        /// <returns>The secure key</returns>
        /// <remarks>This method fetches the security stamp from the database for things like HMAC shared secrets</remarks>
        public byte[] GetPublicKey(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Get key
            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant());
                    if (app == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND);
                    }

                    // Key is null
                    if (app.SigningKey == null)
                    {
                        return null;
                    }

                    // First 16 bytes are IV
                    var ivLength = app.SigningKey[0];
                    var iv = app.SigningKey.Skip(1).Take(ivLength).ToArray();
                    var data = app.SigningKey.Skip(1 + ivLength).ToArray();
                    return this.m_symmetricCryptographicProvider.Decrypt(data, this.m_symmetricCryptographicProvider.GetContextKey(), iv);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching security information for {0}", name);
                    throw new DataPersistenceException(ErrorMessages.ERR_FETCH_APPLICATION_KEY, e);
                }
            }
        }

        /// <summary>
        /// Set the lockout period
        /// </summary>
        /// <param name="name">The name of the application to set</param>
        /// <param name="lockoutState">The lockout state to set</param>
        /// <param name="principal">The principal attempting lockout</param>
        public void SetLockout(string name, bool lockoutState, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            if (!principal.Identity.IsAuthenticated || principal != AuthenticationContext.SystemPrincipal)
            {
                this.m_pepService.Demand(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ?
                    PermissionPolicyIdentifiers.AlterIdentity : PermissionPolicyIdentifiers.AlterLocalIdentity, principal);
            }

            // Get the write connection
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if (app == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND);
                    }

                    if (lockoutState)
                    {
                        app.Lockout = DateTimeOffset.MaxValue.ToLocalTime();
                    }
                    else
                    {
                        app.Lockout = null;
                        app.LockoutSpecified = true;
                    }

                    app = context.Update(app);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating lockout state for {0}", name);
                    throw new DataPersistenceException(ErrorMessages.ERR_SET_LOCKOUT, e);
                }
            }

        }

        /// <summary>
        /// Create the specified identity
        /// </summary>
        
        public IApplicationIdentity CreateIdentity(string applicationName, string password, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateApplication, principal);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Is password null? If so generate new 
                        if (String.IsNullOrEmpty(password))
                        {
                            password = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "");
                        }

                        // Create the principal
                        DbSecurityApplication dbsa = new DbSecurityApplication()
                        {
                            PublicId = applicationName,
                            CreatedByKey = context.EstablishProvenance(principal, null),
                            CreationTime = DateTimeOffset.Now,
                            Secret = this.m_hasher.ComputeHash(this.m_configuration.AddPepper(password))
                        };
                        dbsa = context.Insert(dbsa);

                        // Assign default group policies
                        var skelSql = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom()
                            .InnerJoin<DbSecurityRole>(o => o.SourceKey, o => o.Key)
                            .Where<DbSecurityRole>(o => o.Name == "APPLICATIONS");

                        context.Query<DbSecurityRolePolicy>(skelSql)
                            .ToList()
                            .ForEach(o => context.Insert(new DbSecurityApplicationPolicy()
                            {
                                GrantType = o.GrantType,
                                PolicyKey = o.PolicyKey,
                                SourceKey = dbsa.Key,
                                Key = Guid.NewGuid()
                            }));

                        tx.Commit();

                        return new AdoApplicationIdentity(dbsa);
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating identity {0}", applicationName);
                    throw new DataPersistenceException(ErrorMessages.ERR_AUTH_APP_CREATE, e);
                }
            }
        }

        /// <summary>
        /// Gets the device security identity
        /// </summary>
        public Guid GetSid(string name)
        {
            if(String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }    
            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    var app = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null)?.Key;
                    if(!app.HasValue)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_NOT_FOUND);
                    }
                    return app.Value;
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error fetching SID for {0}", name);
                    throw new DataPersistenceException(ErrorMessages.ERR_DATA_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Set the public key used for symmetric encryption, etc.
        /// </summary>
        public void SetPublicKey(string name, byte[] key, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if(key == null || key.Length == 0)
            {
                throw new ArgumentException(nameof(key), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Must be self or have unrestricted 
            if (!principal.Identity.IsAuthenticated || !principal.Identity.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration, principal);
            }

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbApp = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if(dbApp == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_FETCH_APPLICATION_KEY);
                    }

                    var iv = this.m_symmetricCryptographicProvider.GenerateIV();
                    var encData = this.m_symmetricCryptographicProvider.Encrypt(key, this.m_symmetricCryptographicProvider.GetContextKey(), iv);
                    byte[] storeData = new byte[iv.Length + encData.Length + 1];
                    storeData[0] = (byte)iv.Length;
                    Array.Copy(iv, 0, storeData, 1, iv.Length);
                    Array.Copy(encData, 0, storeData, 1 + iv.Length, encData.Length);
                    dbApp.SigningKey = storeData;
                    dbApp.UpdatedByKey = context.EstablishProvenance(principal, null);
                    dbApp.UpdatedTime = DateTimeOffset.Now;

                    context.Update(dbApp);
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error updating application key: {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_APP_UPDATE_ERROR, e);
                }
            }

        }

        /// <summary>
        /// Delete identity
        /// </summary>
        public void DeleteIdentity(string name, IPrincipal principal)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateDevice, principal);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbApp = context.FirstOrDefault<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == name.ToLowerInvariant() && o.ObsoletionTime == null);
                    if(dbApp == null)
                    {
                        throw new KeyNotFoundException(ErrorMessages.ERR_FETCH_APPLICATION_KEY);
                    }

                    dbApp.ObsoletedByKey = context.EstablishProvenance(principal, null);
                    dbApp.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(dbApp);
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException(ErrorMessages.ERR_APP_DELETE_ERROR, e);
                }
            }
        }
    }
}