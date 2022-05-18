using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Docker
{
    /// <summary>
    /// ADO.NET Database Feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AdoDataDockerFeature : IDockerFeature
    {
        /// <summary>
        /// Readwrite connection
        /// </summary>
        public const string ConnectionStringSetting = "RW_CONNECTION";

        /// <summary>
        /// Readonly connection
        /// </summary>
        public const string ReadonlyConnectionSetting = "RO_CONNECTION";

        public const string VersioningPolicySetting = "VERSIONING";

        /// <summary>
        /// The deletion mode
        /// </summary>
        public const string DeleteModeSetting = "DELETE_MODE";

        /// <summary>
        /// The loading mode
        /// </summary>
        public const string LoadModeSetting = "READ_MODE";

        /// <summary>
        /// ID of the docker feature
        /// </summary>
        public string Id => "NUADO";


        /// <summary>
        /// Gets the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { ConnectionStringSetting, ReadonlyConnectionSetting, DeleteModeSetting, LoadModeSetting, VersioningPolicySetting };

        /// <summary>
        /// Configure the service
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var configSection = configuration.GetSection<AdoPersistenceConfigurationSection>();
            if (configSection == null)
            {
                configSection = new AdoPersistenceConfigurationSection()
                {
                    AutoInsertChildren = true,
                    AutoUpdateExisting = true,
                    PrepareStatements = true,
                    ProviderType = "Npgsql",
                    ReadonlyConnectionString = "MAIN",
                    ReadWriteConnectionString = "MAIN",
                    LoadStrategy = Core.Services.LoadMode.SyncLoad,
                    CachingPolicy = new AdoPersistenceCachingPolicy()
                    {
                        DataObjectExpiry = new TimeSpan(0,0,30)
                    },
                    DeleteStrategy = Core.Services.DeleteMode.LogicalDelete,
                    StrictKeyAgreement= true,
                    MaxRequests = 1000,
                    VersioningPolicy = AdoVersioningPolicyFlags.FullVersioning,
                    Validation = new List<AdoValidationPolicy>()
                    {
                        new AdoValidationPolicy()
                        {
                            Authority = AdoValidationEnforcement.Loose,
                            Format = AdoValidationEnforcement.Loose,
                            Scope = AdoValidationEnforcement.Strict,
                            Uniqueness = AdoValidationEnforcement.Loose,
                            CheckDigit = AdoValidationEnforcement.Loose
                        }
                    }
                };
                configuration.AddSection(configSection);
            }

            if(settings.TryGetValue(LoadModeSetting, out string loadModeString) && Enum.TryParse(loadModeString, out Core.Services.LoadMode loadMode))
            {
                configSection.LoadStrategy = loadMode;
            }
            if(settings.TryGetValue(DeleteModeSetting, out string deleteModeString) && Enum.TryParse(deleteModeString, out Core.Services.DeleteMode deleteMode))
            {
                configSection.DeleteStrategy = deleteMode;
            }
            if(settings.TryGetValue(VersioningPolicySetting, out string versionModeString) && Enum.TryParse(versionModeString, out AdoVersioningPolicyFlags versionMode))
            {
                configSection.VersioningPolicy = versionMode;
            }

            if (settings.TryGetValue(ReadonlyConnectionSetting, out string roConnection))
            {
                configSection.ReadonlyConnectionString = roConnection;
            }

            if (settings.TryGetValue(ConnectionStringSetting, out string rwConnection))
            {
                configSection.ReadWriteConnectionString = rwConnection;
            }


            // Service types
            var serviceTypes = new Type[] {
                typeof(SanteDB.Persistence.Data.Services.AdoApplicationIdentityProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoDeviceIdentityProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoIdentityProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoSecurityChallengeProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoSessionProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoPolicyInformationService),
                typeof(SanteDB.Persistence.Data.Services.AdoRoleProvider),
                typeof(SanteDB.Persistence.Data.Services.AdoPersistenceService),
                //typeof(SanteDB.Persistence.Data.Services.AdoSubscriptionExecutor)
              };
            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.AddRange(serviceTypes.Where(t => !serviceConfiguration.Any(c => c.Type == t)).Select(t => new TypeReferenceConfiguration(t)));
        }
    }
}