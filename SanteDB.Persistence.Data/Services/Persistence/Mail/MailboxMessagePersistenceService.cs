using SanteDB.Core.Mail;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Mail;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Mail
{
    /// <summary>
    /// Represents a persistence service which can persist the assocation between a mail message and mailbox
    /// </summary>
    public class MailboxMessagePersistenceService : IdentifiedDataPersistenceService<MailboxMailMessage, DbMailboxMessageAssociation>
    {
        /// <inheritdoc/>
        public MailboxMessagePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override MailboxMailMessage BeforePersisting(DataContext context, MailboxMailMessage data)
        {
            data.TargetKey = this.EnsureExists(context, data.Target)?.Key ?? data.TargetKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override MailboxMailMessage DoConvertToInformationModel(DataContext context, DbMailboxMessageAssociation dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            if((DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy) == LoadMode.FullLoad)
            {
                retVal.Target = retVal.Target.GetRelatedPersistenceService().Get(context, dbModel.TargetKey);
                retVal.SetLoaded(o => o.Target);
            }
            return retVal;
        }
    }
}
