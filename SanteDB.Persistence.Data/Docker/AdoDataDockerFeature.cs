using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Docker
{
    /// <summary>
    /// ADO.NET Database Feature
    /// </summary>
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
        /// <summary>
        /// Fuzzy counts
        /// </summary>
        public const string FuzzyTotalSetting = "FUZZY_TOTAL";

        /// <summary>
        /// ID of the docker feature
        /// </summary>
        public string Id => "NUADO";

        /// <summary>
        /// Gets the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { FuzzyTotalSetting, ConnectionStringSetting, ReadonlyConnectionSetting };

        /// <summary>
        /// Configure the service
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {

            var configSection = configuration.GetSection<AdoPersistenceConfigurationSection>();
            if(configSection == null)
            {
                configSection = new AdoPersistenceConfigurationSection()
                {
                    AutoInsertChildren = true,
                    AutoUpdateExisting = true,
                    PrepareStatements = true,
                    ProviderType = "Npgsql",
                    ReadonlyConnectionString = "MAIN",
                    ReadWriteConnectionString = "MAIN",
                    UseFuzzyTotals = false,
                    Validation = new EntityValidationFlags()
                    {
                        IdentifierFormat = true,
                        IdentifierUniqueness = true,
                        ValidationLevel = Core.BusinessRules.DetectedIssuePriorityType.Warning
                    }
                };
                configuration.AddSection(configSection);
            }

            if(settings.TryGetValue(ReadonlyConnectionSetting, out string roConnection))
            {
                configSection.ReadonlyConnectionString = roConnection;
            }

            if(settings.TryGetValue(ConnectionStringSetting, out string rwConnection))
            {
                configSection.ReadWriteConnectionString = rwConnection;
            }

            if(settings.TryGetValue(FuzzyTotalSetting, out string fuzzyTotal))
            {
                if(!Boolean.TryParse(fuzzyTotal, out bool fuzzyTotalBool))
                {
                    throw new ArgumentException($"{fuzzyTotal} is not a valid Boolean");
                }
                configSection.UseFuzzyTotals = fuzzyTotalBool;
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
                typeof(SanteDB.Persistence.Data.Services.AdoSubscriptionExecutor)
              };
            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.AddRange(serviceTypes.Where(t => !serviceConfiguration.Any(c => c.Type == t)).Select(t => new TypeReferenceConfiguration(t)));
        }
    }
}
