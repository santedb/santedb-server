using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Tag persistence service
    /// </summary>
    public sealed class TagPersistenceService : ITagPersistenceService
    {
        // Configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// Localization service
        /// </summary>
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Creates a new tag persistence service
        /// </summary>
        public TagPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Data Tagging Service";

        /// <summary>
        /// Save the specified tag against the specified source key
        /// </summary>
        public void Save(Guid sourceKey, ITag tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (tag.TagKey.StartsWith("$")) // transient tag don't save
            {
                return;
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var provenanceId = context.EstablishProvenance(AuthenticationContext.Current.Principal, null);
                    // Persist act tags
                    if (tag is ActTag actTag)
                    {
                        var existingTag = context.FirstOrDefault<DbActTag>(o => o.SourceKey == sourceKey && o.TagKey == tag.TagKey && o.ObsoletionTime == null);
                        if (existingTag != null)
                        {
                            // Update?
                            if (String.IsNullOrEmpty(tag.Value))
                            {
                                existingTag.ObsoletedByKey = provenanceId;
                                existingTag.ObsoletionTime = DateTimeOffset.Now;
                            }
                            else
                            {
                                existingTag.Value = tag.Value;
                            }
                            context.Update(existingTag);
                        }
                        else
                        {
                            context.Insert(new DbActTag()
                            {
                                CreatedByKey = provenanceId,
                                CreationTime = DateTimeOffset.Now,
                                SourceKey = sourceKey,
                                TagKey = tag.TagKey,
                                Value = tag.Value
                            });
                        }
                    }
                    else if (tag is EntityTag entityTag)
                    {
                        var existingTag = context.FirstOrDefault<DbEntityTag>(o => o.SourceKey == sourceKey && o.TagKey == tag.TagKey && o.ObsoletionTime == null);
                        if (existingTag != null)
                        {
                            // Update?
                            if (String.IsNullOrEmpty(tag.Value))
                            {
                                existingTag.ObsoletedByKey = provenanceId;
                                existingTag.ObsoletionTime = DateTimeOffset.Now;
                            }
                            else
                            {
                                existingTag.Value = tag.Value;
                            }
                            context.Update(existingTag);
                        }
                        else
                        {
                            context.Insert(new DbEntityTag()
                            {
                                CreatedByKey = provenanceId,
                                CreationTime = DateTimeOffset.Now,
                                SourceKey = sourceKey,
                                TagKey = tag.TagKey,
                                Value = tag.Value
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException($"Error adding tag to {sourceKey}", e);
                }
            }
        }
    }
}