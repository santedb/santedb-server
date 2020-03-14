using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a callenge service which uses the ADO.NET tables
    /// </summary>
    public class AdoSecurityChallengeProvider : ISecurityChallengeService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSecurityChallengeProvider));

        // Configuration section
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Security Configuration section
        private SecurityConfigurationSection m_securityConfiguration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();


        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Challenge Service";

        /// <summary>
        /// Service is authenticating
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Authentication has succeeded
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Authenticate a user using their challenge response
        /// </summary>
        public IPrincipal Authenticate(string userName, Guid challengeKey, string response)
        {
            try
            {
                var authArgs = new AuthenticatingEventArgs(userName);
                this.Authenticating?.Invoke(this, authArgs);
                if(authArgs.Cancel)
                    throw new SecurityException("Authentication cancelled");

                var hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
                var responseHash = hashService.ComputeHash(response);
                
                // Connection to perform auth
                using(var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    var query = context.CreateSqlStatement<DbSecurityUser>().SelectFrom(typeof(DbSecurityUser), typeof(DbSecurityUserChallengeAssoc))
                        .InnerJoin<DbSecurityUserChallengeAssoc>(o => o.Key, o => o.UserKey)
                        .Where(o => o.UserName.ToLower() == userName.ToLower() && o.ObsoletionTime == null);
                    var dbUser = context.FirstOrDefault<CompositeResult<DbSecurityUser, DbSecurityUserChallengeAssoc>>(query);

                    // User found?
                    if (dbUser == null)
                        throw new SecurityException("AUTH_INV");
                    else if (dbUser.Object1.Lockout > DateTime.Now)
                        throw new SecurityException("AUTH_LCK");
                    else if (dbUser.Object2.ChallengeResponse != responseHash || dbUser.Object1.Lockout.GetValueOrDefault() > DateTime.Now) // Increment invalid
                    {
                        dbUser.Object1.InvalidLoginAttempts++;
                        if (dbUser.Object1.InvalidLoginAttempts > this.m_securityConfiguration.MaxInvalidLogins) 
                            dbUser.Object1.Lockout = DateTime.Now.Add(new TimeSpan(0, 0, dbUser.Object1.InvalidLoginAttempts.Value * 30));
                        dbUser.Object1.UpdatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                        dbUser.Object1.UpdatedTime = DateTimeOffset.Now;

                        context.Update(dbUser.Object1);
                        if (dbUser.Object1.Lockout > DateTime.Now)
                            throw new AuthenticationException("AUTH_LCK");
                        else
                            throw new AuthenticationException("AUTH_INV");
                    }
                    else
                    {
                        var principal = AdoClaimsIdentity.Create(dbUser.Object1, true, "Secret=" + challengeKey.ToString()).CreateClaimsPrincipal();

                        new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.Login, principal).Demand(); // must still be allowed to login

                        (principal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.ReadMetadata));
                        (principal.Identity as IClaimsIdentity).AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBScopeClaim, PermissionPolicyIdentifiers.LoginPasswordOnly));

                        this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, principal, true));
                        return principal;
                    }
                }
            }
            catch (Exception e)
            {
                this.Authenticated?.Invoke(this, new AuthenticatedEventArgs(userName, null, false));
                throw new AuthenticationException($"Challenge authentication failed");
            }
        }

        public IEnumerable<SecurityChallenge> Get(Guid userKey)
        {
            throw new NotImplementedException();
        }

        public void Remove(Guid userKey, Guid challengeKey)
        {
            throw new NotImplementedException();
        }

        public void Set(Guid userKey, Guid challengeKey, string response)
        {
            throw new NotImplementedException();
        }
    }
}
