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
 * Date: 2022-9-7
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Exceptions;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// Implementation of the <see cref="ICertificateIdentityProvider"/> which can authenticate 
    /// principals from an X509 certificiate
    /// </summary>
    public class AdoCertificateIdentityProvider : ICertificateIdentityProvider
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoCertificateIdentityProvider));

        private readonly AdoPersistenceConfigurationSection m_configuration;
        private readonly SecurityConfigurationSection m_securityConfiguration;
        private readonly IPasswordHashingService m_passwordHashingService;
        private readonly IPolicyEnforcementService m_pepService;
        private readonly ITfaRelayService m_tfaRelay;
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Creates a new ADO session identity provider with injected configuration manager
        /// </summary>
        public AdoCertificateIdentityProvider(IConfigurationManager configuration,
            ILocalizationService localizationService,
            IPasswordHashingService passwordHashingService,
            IPolicyEnforcementService policyEnforcementService,
            ITfaRelayService twoFactorSecretGenerator = null)
        {
            this.m_configuration = configuration.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configuration.GetSection<SecurityConfigurationSection>();
            this.m_passwordHashingService = passwordHashingService;
            this.m_pepService = policyEnforcementService;
            this.m_tfaRelay = twoFactorSecretGenerator;
            this.m_localizationService = localizationService;
        }

        /// <inheritdoc/>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;
        /// <inheritdoc/>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">When an expected argument is not provided</exception>
        /// <exception cref="SecurityException">When a general security constraint is violated</exception>
        /// <exception cref="DataPersistenceException">When persisting the identity mapping fails</exception>
        public void AddIdentityMap(IIdentity identityToBeMapped, X509Certificate2 authenticationCertificate, IPrincipal authenticatedPrincipal)
        {
            if (identityToBeMapped == null)
            {
                throw new ArgumentNullException(nameof(identityToBeMapped), ErrorMessages.ARGUMENT_NULL);
            }
            else if (authenticationCertificate == null)
            {
                throw new ArgumentNullException(nameof(authenticationCertificate), ErrorMessages.ARGUMENT_NULL);
            }
            else if (authenticatedPrincipal == null)
            {
                throw new ArgumentNullException(nameof(authenticatedPrincipal), ErrorMessages.ARGUMENT_NULL);
            }
            else if (authenticationCertificate.NotAfter < DateTimeOffset.Now || authenticationCertificate.NotBefore > DateTimeOffset.Now)
            {
                throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_NOT_BEFORE_AFTER));
            }

            // Demand access to the alter identity permission
            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, authenticatedPrincipal);

            try
            {
               
                // Now insert into database
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();

                    // get the identity
                    var certificateRegistration = new DbCertificateMapping()
                    {
                        CreatedByKey = context.EstablishProvenance(authenticatedPrincipal, null),
                        CreationTime = DateTimeOffset.Now,
                        Expiration = authenticationCertificate.NotAfter,
                        X509Thumbprint = authenticationCertificate.Thumbprint,
                        X509PublicKeyData = authenticationCertificate.RawData
                    };

                    switch (identityToBeMapped)
                    {
                        case IDeviceIdentity did:
                            certificateRegistration.SecurityDeviceKey = context.Query<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == did.Name.ToLowerInvariant()).Select(o => o.Key).FirstOrDefault();
                            break;
                        case IApplicationIdentity aid:
                            certificateRegistration.SecurityApplicationKey = context.Query<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == aid.Name.ToLowerInvariant()).Select(o => o.Key).FirstOrDefault();
                            break;
                        default:
                            certificateRegistration.SecurityUserKey = context.Query<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == identityToBeMapped.Name.ToLowerInvariant()).Select(o => o.Key).FirstOrDefault();
                            break;
                    }

                    // attempt storage
                    context.Insert(certificateRegistration);

                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error creating identity mapping {0} with {1} - {2}", identityToBeMapped.Name, authenticationCertificate.Subject, e.Message);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_CREATE_GEN, new { identity = identityToBeMapped.Name, subject = authenticationCertificate.Subject }), e);
            }

        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException">When an expected argument is not provided</exception>
        /// <exception cref="SecurityException">When a general security constraint is violated</exception>
        /// <exception cref="AuthenticationException">When authentication fails</exception>
        public IPrincipal Authenticate(X509Certificate2 authenticationCertificate)
        {
            if (authenticationCertificate == null)
            {
                throw new ArgumentNullException(nameof(authenticationCertificate), ErrorMessages.ARGUMENT_NULL);
            }
            else if (authenticationCertificate.NotAfter < DateTimeOffset.Now || authenticationCertificate.NotBefore > DateTimeOffset.Now)
            {
                throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_NOT_BEFORE_AFTER));
            }

            var preEvtArgs = new AuthenticatingEventArgs(authenticationCertificate.Subject);
            this.Authenticating?.Invoke(this, preEvtArgs);
            if (preEvtArgs.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Authenticate trigger signals cancel");
                if (preEvtArgs.Success)
                {
                    return preEvtArgs.Principal;
                }
                else
                {
                    throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CANCELLED));
                }
            }

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();

                    var authSql = context.CreateSqlStatement<DbCertificateMapping>().SelectFrom(typeof(DbCertificateMapping), typeof(DbSecurityUser), typeof(DbSecurityApplication), typeof(DbSecurityDevice))
                        .Join<DbCertificateMapping, DbSecurityUser>("LEFT", o => o.SecurityUserKey, o => o.Key)
                        .Join<DbCertificateMapping, DbSecurityApplication>("LEFT", o => o.SecurityApplicationKey, o => o.Key)
                        .Join<DbCertificateMapping, DbSecurityDevice>("LEFT", o => o.SecurityDeviceKey, o => o.Key)
                        .Where<DbCertificateMapping>(o => o.X509Thumbprint == authenticationCertificate.Thumbprint && o.ObsoletionTime == null && o.Expiration > DateTime.Now)
                        .And<DbSecurityDevice>(o => o.ObsoletionTime == null)
                        .And<DbSecurityApplication>(o => o.ObsoletionTime == null)
                        .And<DbSecurityUser>(o => o.ObsoletionTime == null);

                    var authData = context.Query<CompositeResult<DbCertificateMapping, DbSecurityUser, DbSecurityApplication, DbSecurityDevice>>(authSql).SingleOrDefault();
                    // Was authentication successful?
                    if (authData?.Object1 == null)
                    {
                        throw new InvalidIdentityAuthenticationException();
                    }

                    AdoIdentity authenticatedIdentity = null;
                    // The type of authentication principal sets the behavior
                    if (authData.Object1.SecurityUserKey.HasValue)
                    {
                        // Ensure the user is not locked
                        if (authData.Object2.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                        {
                            throw new LockedIdentityAuthenticationException(authData.Object2.Lockout.Value);
                        }
                        else if (authData.Object2.TwoFactorEnabled)
                        {
                            throw new InvalidOperationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_TFA_INVALID));
                        }

                        // Update the last login
                        authData.Object2.UpdatedTime = authData.Object2.LastLoginTime = DateTimeOffset.Now;
                        authData.Object2.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                        context.Update(authData.Object2);
                        authenticatedIdentity = new AdoUserIdentity(authData.Object2, "CERTIFICATE");

                        // Claims to add to the principal
                        var claims = context.Query<DbUserClaim>(o => o.SourceKey == authData.Object1.SecurityUserKey && o.ClaimExpiry < DateTimeOffset.Now).ToList();
                        claims.RemoveAll(o => o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode);

                        // Establish role
                        var roleSql = context.CreateSqlStatement<DbSecurityRole>()
                            .SelectFrom()
                            .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                            .Where<DbSecurityUserRole>(o => o.UserKey == authData.Object1.SecurityUserKey);
                        (authenticatedIdentity as AdoUserIdentity).AddRoleClaims(context.Query<DbSecurityRole>(roleSql).Select(o => o.Name));

                    }
                    else if (authData.Object1.SecurityApplicationKey.HasValue)
                    {
                        if (authData.Object3.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                        {
                            throw new LockedIdentityAuthenticationException(authData.Object3.Lockout.Value);
                        }

                        // Update the last login
                        authData.Object3.UpdatedTime = authData.Object3.LastAuthentication = DateTimeOffset.Now;
                        authData.Object3.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                        context.Update(authData.Object3);
                        authenticatedIdentity = new AdoApplicationIdentity(authData.Object3, "CERTIFICATE");
                    }
                    else if (authData.Object1.SecurityDeviceKey.HasValue)
                    {
                        if (authData.Object4.Lockout.GetValueOrDefault() > DateTimeOffset.Now)
                        {
                            throw new LockedIdentityAuthenticationException(authData.Object4.Lockout.Value);
                        }

                        // Update the last login
                        authData.Object4.UpdatedTime = authData.Object4.LastAuthentication = DateTimeOffset.Now;
                        authData.Object4.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                        context.Update(authData.Object4);
                        authenticatedIdentity = new AdoDeviceIdentity(authData.Object4, "CERTIFICATE");
                    }
                    else
                    {
                        throw new InvalidOperationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_NO_CERT_MAP));
                    }


                    authenticatedIdentity.AddClaim(new SanteDBClaim(SanteDBClaimTypes.AuthenticationCertificate, authenticationCertificate.Subject));

                    // Create principal
                    var retVal = new AdoClaimsPrincipal(authenticatedIdentity);
                    this.m_pepService.Demand(PermissionPolicyIdentifiers.Login, retVal);

                    // Fire authentication
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(authenticatedIdentity.Name, retVal, true));
                    return retVal;
                }
            }
            catch (AuthenticationException)
            {
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(authenticationCertificate.Subject, null, false));
                throw;
            }
            catch (Exception e)
            {
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(authenticationCertificate.Subject, null, false));
                this.m_tracer.TraceError("Could not authenticate using certificate {0}- {1}", authenticationCertificate.Subject, e);
                throw new AuthenticationException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_GENERAL), e);
            }
        }

        /// <inheritdoc/>
        public IIdentity GetCertificateIdentity(X509Certificate2 authenticationCertificate)
        {
            if(authenticationCertificate == null)
            {
                throw new ArgumentNullException(nameof(authenticationCertificate), ErrorMessages.ARGUMENT_NULL);
            }

            try
            {
                using(var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();

                    var authSql = context.CreateSqlStatement<DbCertificateMapping>().SelectFrom(typeof(DbCertificateMapping), typeof(DbSecurityUser), typeof(DbSecurityApplication), typeof(DbSecurityDevice))
                        .Join<DbCertificateMapping, DbSecurityUser>("LEFT", o => o.SecurityUserKey, o => o.Key)
                        .Join<DbCertificateMapping, DbSecurityApplication>("LEFT", o => o.SecurityApplicationKey, o => o.Key)
                        .Join<DbCertificateMapping, DbSecurityDevice>("LEFT", o => o.SecurityDeviceKey, o => o.Key)
                        .Where<DbCertificateMapping>(o => o.X509Thumbprint == authenticationCertificate.Thumbprint && o.ObsoletionTime == null && o.Expiration > DateTime.Now)
                        .And<DbSecurityDevice>(o => o.ObsoletionTime == null)
                        .And<DbSecurityApplication>(o => o.ObsoletionTime == null)
                        .And<DbSecurityUser>(o => o.ObsoletionTime == null);

                    // Return the appropriate type of data 
                    var authData = context.Query<CompositeResult<DbCertificateMapping, DbSecurityUser, DbSecurityApplication, DbSecurityDevice>>(authSql).FirstOrDefault();
                    if(authData.Object1.SecurityUserKey.HasValue)
                    {
                        return new AdoUserIdentity(authData.Object2);
                    }
                    else if(authData.Object1.SecurityApplicationKey.HasValue)
                    {
                        return new AdoApplicationIdentity(authData.Object3);
                    }
                    else if (authData.Object1.SecurityDeviceKey.HasValue)
                    {
                        return new AdoDeviceIdentity(authData.Object4);
                    }
                    else
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_NO_CERT_MAP));
                    }

                }
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Could not find mapped identity using certificate {0}- {1}", authenticationCertificate.Subject, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_GENERAL), e);
            }
        }

        /// <inheritdoc/>
        public X509Certificate2 GetIdentityCertificate(IIdentity identityOfCertificte)
        {
            if(identityOfCertificte == null)
            {
                throw new ArgumentNullException(nameof(identityOfCertificte), ErrorMessages.ARGUMENT_NULL);
            }

            try
            {
                using(var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    DbCertificateMapping retVal = null;
                    if(identityOfCertificte is IDeviceIdentity did)
                    {
                        retVal = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                            .InnerJoin<DbCertificateMapping, DbSecurityDevice>(o => o.SecurityDeviceKey, o => o.Key)
                            .Where<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == did.Name.ToLowerInvariant() && o.ObsoletionTime == null)
                            .And<DbCertificateMapping>(o => o.ObsoletionTime == null)).FirstOrDefault();
                    }
                    else if(identityOfCertificte is IApplicationIdentity aid)
                    {
                        retVal = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                           .InnerJoin<DbCertificateMapping, DbSecurityApplication>(o => o.SecurityApplicationKey, o => o.Key)
                           .Where<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == aid.Name.ToLowerInvariant() && o.ObsoletionTime == null)
                           .And<DbCertificateMapping>(o => o.ObsoletionTime == null)).FirstOrDefault();
                    }
                    else
                    {
                        retVal = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                           .InnerJoin<DbCertificateMapping, DbSecurityUser>(o => o.SecurityUserKey, o=>o.Key)
                           .Where<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == identityOfCertificte.Name.ToLowerInvariant() && o.ObsoletionTime == null)
                           .And<DbCertificateMapping>(o => o.ObsoletionTime == null)).FirstOrDefault();
                    }

                    if (retVal != null)
                    {
                        return new X509Certificate2(retVal.X509PublicKeyData);
                    }
                    else
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_NO_CERT_MAP));
                    }
                }
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Could not find mapped identity using identity {0}- {1}", identityOfCertificte.Name, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_GENERAL), e);
            }
        }

        /// <inheritdoc/>
        public bool RemoveIdentityMap(IIdentity identityToBeUnMapped, X509Certificate2 authenticationCertificate, IPrincipal authenticatedPrincipal)
        {
            if(identityToBeUnMapped == null)
            {
                throw new ArgumentNullException(nameof(identityToBeUnMapped), ErrorMessages.ARGUMENT_NULL);
            }
            else if(authenticationCertificate == null)
            {
                throw new ArgumentNullException(nameof(authenticationCertificate), ErrorMessages.ARGUMENT_NULL);
            }
            else if(authenticatedPrincipal == null)
            {
                throw new ArgumentNullException(nameof(authenticatedPrincipal), ErrorMessages.ARGUMENT_NULL);
            }
            else if (authenticationCertificate.NotAfter < DateTimeOffset.Now || authenticationCertificate.NotBefore > DateTimeOffset.Now)
            {
                throw new SecurityException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_NOT_BEFORE_AFTER));
            }

            // Demand access to the alter identity permission
            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, authenticatedPrincipal);

            try
            {
                using(var context = this.m_configuration.Provider.GetWriteConnection())
                {

                    context.Open();

                    // Lookup the certificate
                    DbCertificateMapping dbCertMapping = null;
                    if(identityToBeUnMapped is IDeviceIdentity did) {
                        dbCertMapping = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                            .InnerJoin<DbSecurityDevice>(o => o.SecurityDeviceKey, o => o.Key)
                            .Where<DbSecurityDevice>(o => o.ObsoletionTime == null && o.PublicId.ToLowerInvariant() == did.Name.ToLowerInvariant())
                            .And<DbCertificateMapping>(o => o.ObsoletionTime == null && o.X509Thumbprint == authenticationCertificate.Thumbprint))
                            .FirstOrDefault();
                    }
                    else if(identityToBeUnMapped is IApplicationIdentity aid)
                    {
                        dbCertMapping = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                            .InnerJoin<DbSecurityApplication>(o => o.SecurityApplicationKey, o => o.Key)
                            .Where<DbSecurityApplication>(o => o.ObsoletionTime == null && o.PublicId.ToLowerInvariant() == aid.Name.ToLowerInvariant())
                            .And<DbCertificateMapping>(o => o.ObsoletionTime == null && o.X509Thumbprint == authenticationCertificate.Thumbprint))
                            .FirstOrDefault();
                    }
                    else
                    {
                        dbCertMapping = context.Query<DbCertificateMapping>(context.CreateSqlStatement<DbCertificateMapping>().SelectFrom()
                            .InnerJoin<DbSecurityUser>(o => o.SecurityUserKey, o => o.Key)
                            .Where<DbSecurityUser>(o => o.ObsoletionTime == null && o.UserName.ToLowerInvariant() == identityToBeUnMapped.Name.ToLowerInvariant())
                            .And<DbCertificateMapping>(o => o.ObsoletionTime == null && o.X509Thumbprint == authenticationCertificate.Thumbprint))
                            .FirstOrDefault();
                    }

                    if(dbCertMapping == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_NO_CERT_MAP, new { identity = identityToBeUnMapped.Name, cert = authenticationCertificate.Subject }));
                    }

                    dbCertMapping.ObsoletedByKey = context.EstablishProvenance(authenticatedPrincipal, null);
                    dbCertMapping.ObsoletionTime = DateTimeOffset.Now;
                    context.Update(dbCertMapping);
                    return true;
                }
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error removing identity mapping {0} with {1} - {2}", identityToBeUnMapped.Name, authenticationCertificate.Subject, e.Message);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CERT_CREATE_GEN, new { identity = identityToBeUnMapped.Name, subject = authenticationCertificate.Subject }), e);
            }
        }
    }
}
