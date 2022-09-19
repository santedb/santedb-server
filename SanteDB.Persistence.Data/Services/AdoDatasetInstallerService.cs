using SanteDB.Core.Data.Initialization;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Sys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// A <see cref="IDatasetInstallerService"/> which can install datasets in ADO.NET persistence layer
    /// </summary>
    public class AdoDatasetInstallerService : IDatasetInstallerService, IReportProgressChanged
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoDatasetInstallerService));

        // Configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoDatasetInstallerService(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Dataset Installation Service";

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Get the installation date
        /// </summary>
        public DateTimeOffset? GetInstallDate(string dataSetId)
        {
            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    var patchId = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(dataSetId)).HexEncode();

                    context.Open();
                    return context.Query<DbPatch>(o => o.PatchId == patchId).Select(o => o.ApplyDate).FirstOrDefault();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(String.Empty, e);
                }
            }
        }

        /// <summary>
        /// Get all installed datasets
        /// </summary>
        public IEnumerable<string> GetInstalled()
        {
            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    return context.Query<DbPatch>(o => true).Select(o => o.PatchId);
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(String.Empty, e);
                }
            }
        }

        /// <summary>
        /// Install the dataset into the primary datastore
        /// </summary>
        /// <param name="dataset">The dataset to be installed</param>
        /// <returns>True if installation succeeded</returns>
        public bool Install(Dataset dataset)
        {
            if (dataset == null)
            {
                throw new ArgumentNullException(nameof(dataset), ErrorMessages.ARGUMENT_NULL);
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    var patchId = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(dataset.Id)).HexEncode();

                    // Check if dataset is installed
                    if (context.Any<DbPatch>(o => o.PatchId == patchId))
                    {
                        this.m_tracer.TraceWarning("Skipping {0} because it is already installed", dataset.Id);
                        return false;
                    }

                    using (var tx = context.BeginTransaction())
                    {
                        context.ContextId = context.EstablishProvenance(AuthenticationContext.Current.Principal, null);

                        for (var i = 0; i < dataset.Action.Count; i++)
                        {
                            var itm = dataset.Action[i];
                            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)i / (float)dataset.Action.Count, String.Format(UserMessages.PROCESSING, dataset.Id)));
                            var persistenceService = itm.Element.GetType().GetRelatedPersistenceService();

                            itm.Element.Key = itm.Element.Key ?? Guid.NewGuid();

                            try
                            {
                                switch (itm)
                                {
                                    case DataInsert di:
                                        if (di.SkipIfExists && persistenceService.Exists(context, itm.Element.Key.Value))
                                            continue;
                                        persistenceService.Insert(context, di.Element);
                                        break;
                                    case DataUpdate du:
                                        if (du.InsertIfNotExists && !persistenceService.Exists(context, itm.Element.Key.Value))
                                            persistenceService.Insert(context, itm.Element);
                                        else
                                            persistenceService.Update(context, itm.Element);
                                        break;
                                    case DataDelete dd:
                                        persistenceService.Delete(context, dd.Element.Key.Value, DataPersistenceControlContext.Current?.DeleteMode ?? this.m_configuration.DeleteStrategy);
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                if (itm.IgnoreErrors)
                                {
                                    this.m_tracer.TraceWarning("Error applying dataset - ignore errors is enabled - {0}", e.Message);
                                    return false;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }

                        // Insert the post install trigger
                        if (dataset.SqlExec?.Any() == true)
                        {
                            this.m_tracer.TraceInfo("Executing post-install triggers for {0}...", dataset.Id);
                            foreach (var itm in dataset.SqlExec.Where(o => o.InvariantName == this.m_configuration.Provider.Invariant))
                            {
                                context.ExecuteNonQuery(context.CreateSqlStatement(itm.QueryText));
                            }
                        }
                        context.Insert(new DbPatch() { ApplyDate = DateTimeOffset.Now, PatchId = patchId, Description = dataset.Id });
                        tx.Commit();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException(String.Format(ErrorMessages.BUNDLE_PERSISTENCE_ERROR, dataset.Id, "X"), e);
                }
            }
        }
    }
}
