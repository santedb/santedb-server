using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An identity provider implemented for .NET
    /// </summary>
    public class AdoIdentityProvider : IIdentityProviderService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSessionProvider));

        // Session configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Data signing service
        private IDataSigningService m_dataSigningService;

        // Hashing service
        private IPasswordHashingService m_passwordHashingService;

        // Security configuration
        private SecurityConfigurationSection m_securityConfiguration;

        // PEP
        private IPolicyEnforcementService m_pepService;

        // TFA generator
        private ITwoFactorSecretGenerator m_tfaGenerator;

        // The password validator
        private IPasswordValidatorService m_passwordValidator;

        /// <summary>
        /// Creates a new ADO session identity provider with injected configuration manager
        /// </summary>
        public AdoIdentityProvider(IConfigurationManager configuration,
            IDataSigningService dataSigning,
            IPasswordHashingService passwordHashingService,
            IPolicyEnforcementService policyEnforcementService,
            ITwoFactorSecretGenerator twoFactorSecretGenerator,
            IPasswordValidatorService passwordValidator)
        {
            this.m_configuration = configuration.GetSection<AdoPersistenceConfigurationSection>();
            this.m_securityConfiguration = configuration.GetSection<SecurityConfigurationSection>();
            this.m_dataSigningService = dataSigning;
            this.m_passwordHashingService = passwordHashingService;
            this.m_pepService = policyEnforcementService;
            this.m_tfaGenerator = twoFactorSecretGenerator;
            this.m_passwordValidator = passwordValidator;
        }


        /// <summary>
        /// Gets the service name of the identity provider
        /// </summary>
        public string ServiceName => "Data-Based Identity Provider";

        /// <summary>
        /// Fired when the identity provider is authenticating a principal
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;
        /// <summary>
        /// Fired when an identity provider has authenticated the principal
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Adds a claim to the specified user account
        /// </summary>
        /// <param name="userName">The user for which the claim is to be persisted</param>
        /// <param name="claim">The claim which is to be persisted</param>
        /// <param name="principal">The principal which is adding the claim (the authority under which the claim is being added)</param>
        /// <param name="expiry">The expiration time for the claim</param>
        /// <param name="protect">True if the claim should be protected in the database via a hash</param>
        public void AddClaim(string userName, IClaim claim, IPrincipal principal, TimeSpan? expiry = null)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(claim == null)
            {
                throw new ArgumentNullException(nameof(claim), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterIdentity, principal);

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                    if(dbUser == null)
                    {
                        throw new KeyNotFoundException(String.Format(ErrorMessages.ERR_USR_INVALID, userName));
                    }

                    var dbClaim = context.FirstOrDefault<DbUserClaim>(o => o.SourceKey == dbUser.Key);
                    
                    // Current claim in DB? Update
                    if(dbClaim == null)
                    {
                        dbClaim = new DbUserClaim()
                        {
                            SourceKey = dbUser.Key
                        };
                    }
                    dbClaim.ClaimType = claim.Type;
                    dbClaim.ClaimValue = claim.Value;

                    if (expiry.HasValue)
                    {
                        dbClaim.ClaimExpiry = DateTimeOffset.Now.Add(expiry.Value).DateTime;
                    }

                    dbClaim = context.InsertOrUpdate(dbClaim);
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error adding claim to {0} - {1}", userName, e);
                    throw new DataPersistenceException(ErrorMessages.ERR_USER_CLAIM_GEN_ERR,e);
                }
            }

        }

        /// <summary>
        /// Authenticate the specified user with the specified password
        /// </summary>
        /// <param name="userName">The name of the user which is to eb authenticated</param>
        /// <param name="password">The password for the user</param>
        /// <returns>The authenticated principal</returns>
        public IPrincipal Authenticate(string userName, string password)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.AuthenticateInternal(userName, password, null);
        }

        /// <summary>
        /// Authenticate the user with the user name password and TFA secret
        /// </summary>
        /// <param name="userName">The user name of the user to be authenticated</param>
        /// <param name="password">The password of the user</param>
        /// <param name="tfaSecret">The TFA secret provided by the user</param>
        /// <returns>The authentcated principal</returns>
        public IPrincipal Authenticate(string userName, string password, string tfaSecret)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(tfaSecret))
            {
                throw new ArgumentNullException(nameof(tfaSecret), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.AuthenticateInternal(userName, password, tfaSecret);
        }

        /// <summary>
        /// Perform internal authentication routine
        /// </summary>
        /// <param name="userName">The user to authentcate</param>
        /// <param name="password">If provided, the password to authenticated</param>
        /// <param name="tfaSecret">If provided the TFA challenge response</param>
        /// <returns>The authenticated principal</returns>
        private IPrincipal AuthenticateInternal(String userName, String password, String tfaSecret)
        {
            // Allow cancellation
            var preEvtArgs = new AuthenticatingEventArgs(userName);
            this.Authenticating?.Invoke(this, preEvtArgs);
            if(preEvtArgs.Cancel)
            {
                this.m_tracer.TraceWarning("Pre-Authenticate trigger signals cancel");
                if(preEvtArgs.Success)
                {
                    return preEvtArgs.Principal;
                }
                else
                {
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_CANCELLED);
                }
            }

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using(var tx = context.BeginTransaction())
                    {
                        var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);

                        // Perform authentication
                        try
                        {

                            // Claims to add to the principal
                            var claims = context.Query<DbUserClaim>(o => o.SourceKey == dbUser?.Key && o.ClaimExpiry < DateTimeOffset.Now).ToList();

                            if (dbUser == null)
                            {
                                throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_INVALID);
                            }
                            else if (dbUser.Lockout > DateTimeOffset.Now)
                            {
                                throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_LOCKED);
                            }
                            else if (!String.IsNullOrEmpty(password))
                            {
                                if (dbUser.PasswordExpiration.HasValue && dbUser.PasswordExpiration.Value < DateTimeOffset.Now)
                                {
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.PurposeOfUse, ClaimValue = PurposeOfUseKeys.SecurityAdmin.ToString() });
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.SanteDBScopeClaim, ClaimValue = PermissionPolicyIdentifiers.LoginPasswordOnly });
                                    claims.Add(new DbUserClaim() { ClaimType = SanteDBClaimTypes.SanteDBScopeClaim, ClaimValue = PermissionPolicyIdentifiers.ReadMetadata });
                                }
                                else if (!this.m_configuration.GetPepperCombos(password).Any(p => this.m_passwordHashingService.ComputeHash(p) == dbUser.Password))
                                {
                                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_INVALID);
                                }
                            }
                            else if(String.IsNullOrEmpty(password) && !claims.Any(c=>c.ClaimType == SanteDBClaimTypes.SanteDBCodeAuth && "true".Equals(o.ClaimValue, StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_INVALID);
                            }

                            // User requires TFA but the secret is empty
                            if (dbUser.TwoFactorEnabled && String.IsNullOrEmpty(tfaSecret))
                            {
                                var tfaSecretGen = this.m_passwordHashingService.ComputeHash(this.m_tfaGenerator.GenerateTfaSecret());
                                this.AddClaim(userName, new SanteDBClaim(SanteDBClaimTypes.SanteDBOTAuthCode, tfaSecretGen), AuthenticationContext.SystemPrincipal, new TimeSpan(0, 5, 0));
                                throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_TFA_REQ);
                            }

                            // TFA supplied?
                            if (!String.IsNullOrEmpty(tfaSecret) && this.m_passwordHashingService.ComputeHash(tfaSecret) != claims.FirstOrDefault(o => o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode)?.ClaimValue)
                            {
                                throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_INVALID);
                            }
                            else
                            {
                                context.Delete<DbUserClaim>(o => o.SourceKey == dbUser.Key && o.ClaimType == SanteDBClaimTypes.SanteDBOTAuthCode);
                            }

                            // Reset invalid logins
                            dbUser.InvalidLoginAttempts = 0;
                            dbUser.LastLoginTime = DateTimeOffset.Now;

                            dbUser = context.Update(dbUser);

                            // Establish ID
                            var identity = new AdoUserIdentity(dbUser, "LOCAL");

                            // Establish role
                            var roleSql = context.CreateSqlStatement<DbSecurityRole>()
                                .SelectFrom()
                                .InnerJoin<DbSecurityUserRole>(o => o.Key, o => o.RoleKey)
                                .Where<DbSecurityUserRole>(o => o.UserKey == dbUser.Key);
                            identity.AddRoleClaims(context.Query<DbSecurityRole>(roleSql).Select(o => o.Name));

                            // Establish additional claims
                            identity.AddClaims(claims.Select(o => new SanteDBClaim(o.ClaimType, o.ClaimValue)));

                            // Create principal
                            var retVal = new AdoClaimsPrincipal(identity);
                            this.m_pepService.Demand(PermissionPolicyIdentifiers.Login, retVal);

                            // Fire authentication
                            this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, retVal, true));
                            return retVal;
                        }
                        catch(AuthenticationException e) when (e.Message == ErrorMessages.ERR_AUTH_USR_INVALID && dbUser != null)
                        {
                            dbUser.InvalidLoginAttempts++;
                            if (dbUser.InvalidLoginAttempts > this.m_securityConfiguration.GetSecurityPolicy<Int32>(SecurityPolicyIdentification.MaxInvalidLogins, 5))
                            {
                                var lockoutSlide = 30 * dbUser.InvalidLoginAttempts.Value;
                                if (dbUser.Lockout < DateTimeOffset.MaxValue.AddSeconds(-lockoutSlide))
                                {
                                    dbUser.Lockout = DateTimeOffset.Now.AddSeconds(lockoutSlide);
                                }
                            }
                            context.Update(dbUser);
                            throw;
                        }
                        finally
                        {
                            tx.Commit();
                        }
                    }
                }
                catch (AuthenticationException)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                    throw;
                }
                catch (Exception e)
                {
                    this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                    this.m_tracer.TraceError("Could not authenticate user {0}- {1]", userName, e);
                    throw new AuthenticationException(ErrorMessages.ERR_AUTH_USR_GENERAL, e);
                }
            }
        }

        /// <summary>
        /// Change the specified user password
        /// </summary>
        /// <param name="userName">The user who's password is being changed</param>
        /// <param name="newPassword">The new password to set</param>
        /// <param name="principal">The principal which is setting the password</param>
        public void ChangePassword(string userName, string newPassword, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(newPassword))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(!this.m_passwordValidator.Validate(newPassword))
            {
                throw new SecurityException(ErrorMessages.ERR_USR_PWD_COMPLEXITY);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // The user changing the password must be the user or an administrator
            if(!principal.Identity.Name.Equals(userName, StringComparison.OrdinalIgnoreCase))
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.ChangePassword, principal);
            }

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction()) {
                        // Get the user 
                        var dbUser = context.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == userName.ToLowerInvariant() && o.ObsoletionTime == null);
                        if (dbUser == null)
                        {
                            throw new KeyNotFoundException(ErrorMessages.ERR_USR_INVALID);
                        }

                        // Password reuse policy?
                        if (this.m_securityConfiguration.GetSecurityPolicy<bool>(SecurityPolicyIdentification.PasswordHistory) && this.m_configuration.GetPepperCombos(newPassword).Any(o => this.m_passwordHashingService.ComputeHash(o) == dbUser.Password))
                        {
                            throw new SecurityException(ErrorMessages.ERR_USR_PWD_HISTORY);
                        }
                        dbUser.Password = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(newPassword));
                        dbUser.UpdatedByKey = context.EstablishProvenance(principal, null);
                        dbUser.UpdatedTime = DateTimeOffset.Now;

                        // Password expire policy
                        var pwdExpire = this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxPasswordAge);
                        if (pwdExpire != default(TimeSpan))
                        {
                            dbUser.PasswordExpiration = DateTimeOffset.Now.Add(pwdExpire);
                        }

                        // Abandon all sessions for this user
                        foreach(var ses in context.Query<DbSession>(o=>o.UserKey == dbUser.Key))
                        {
                            ses.NotAfter = DateTimeOffset.Now;
                            context.Update(ses);
                        }

                        // Save user
                        dbUser = context.Update(dbUser);

                        tx.Commit();
                    }

                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error updating user password - {0}", e);
                    throw new DataPersistenceException(ErrorMessages.ERR_USR_PWD_GEN_ERR, e);
                }
            }
        }

        /// <summary>
        /// Create a security identity for the specified 
        /// </summary>
        /// <param name="userName">The name of the user identity which is to be created</param>
        /// <param name="password">The initial password to set for the principal</param>
        /// <param name="principal">The principal which is creating the identity</param>
        /// <returns>The created identity</returns>
        public IIdentity CreateIdentity(string userName, string password, IPrincipal principal)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(userName), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (!this.m_passwordValidator.Validate(password))
            {
                throw new SecurityException(ErrorMessages.ERR_USR_PWD_COMPLEXITY);
            }
            else if(principal == null)
            {
                throw new ArgumentNullException(nameof(principal), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Validate create permission
            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateIdentity, principal);
            }
            else
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.CreateLocalIdentity, principal);
            }

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using (var tx = context.BeginTransaction())
                    {
                        // Construct the request
                        var newIdentity = new DbSecurityUser()
                        {
                            UserName = userName,
                            Password = this.m_passwordHashingService.ComputeHash(this.m_configuration.AddPepper(password)),
                            SecurityHash = this.m_passwordHashingService.ComputeHash(userName + password),
                            UserClass = UserClassKeys.HumanUser,
                            InvalidLoginAttempts = 0,
                            CreatedByKey = context.EstablishProvenance(principal, null),
                            CreationTime = DateTimeOffset.Now
                        };

                        var expirePwd = this.m_securityConfiguration.GetSecurityPolicy<TimeSpan>(SecurityPolicyIdentification.MaxPasswordAge);
                        if (expirePwd != default(TimeSpan))
                        {
                            newIdentity.PasswordExpiration = DateTimeOffset.Now.Add(expirePwd);
                        }

                        newIdentity = context.Insert(newIdentity);
                        tx.Commit();
                        return new AdoUserIdentity(newIdentity);
                    }
                }
                catch(Exception e)
                {

                }
            }

        }

        public void DeleteIdentity(string userName, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(string userName)
        {
            throw new NotImplementedException();
        }

        public IIdentity GetIdentity(Guid sid)
        {
            throw new NotImplementedException();
        }

        public Guid GetSid(string name)
        {
            throw new NotImplementedException();
        }

        public IPrincipal ReAuthenticate(IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public void RemoveClaim(string userName, string claimType, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        public void SetLockout(string userName, bool lockout, IPrincipal principal)
        {
            throw new NotImplementedException();
        }
    }
}
