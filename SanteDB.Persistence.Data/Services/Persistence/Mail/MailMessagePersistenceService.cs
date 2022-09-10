using SanteDB.Core.i18n;
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
    /// Mail message persistence service which can handles the persistence of <see cref="MailMessage"/> with 
    /// <see cref="DbMailMessage"/>
    /// </summary>
    public class MailMessagePersistenceService : BaseEntityDataPersistenceService<MailMessage, DbMailMessage>
    {
        /// <inheritdoc/>
        public MailMessagePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override MailMessage DoConvertToInformationModel(DataContext context, DbMailMessage dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Mailboxes = retVal.Mailboxes.GetRelatedPersistenceService().Query(context, o => o.TargetKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.Mailboxes);
                    break;
            }
            return retVal;
        }
    }
}
