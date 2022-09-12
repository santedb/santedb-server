using SanteDB.Core.Mail;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Mail
{
    /// <summary>
    /// Persistence service that can persist and handle mailboxes
    /// </summary>
    public class MailboxPersistenceService : BaseEntityDataPersistenceService<Mailbox, DbMailbox>
    {
        /// <inheritdoc/>
        public MailboxPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Mailbox BeforePersisting(DataContext context, Mailbox data)
        {
            data.OwnerKey = this.EnsureExists(context, data.Owner)?.Key ?? data.OwnerKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Mailbox DoConvertToInformationModel(DataContext context, DbMailbox dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Owner = retVal.Owner.GetRelatedPersistenceService().Get(context, dbModel.OwnerKey);
                    retVal.SetLoaded(o => o.Owner);
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    retVal.Messages = retVal.Messages.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Messages);
                    break;
            }
            return retVal;
        }
    }
}
