using SanteDB.Core.Model.Acts;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which manages acts
    /// </summary>
    public class ActPersistenceService : ActDerivedPersistenceService<Act>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ActPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


        /// <inheritdoc/>
        /// <remarks>This is not implemented on the act persistence service since there are no
        /// dependent tables</remarks>
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbActVersion newVersion)
        {
            if(this.TryGetSubclassPersister(newVersion.ClassConceptKey, out var provider) && provider is IActDerivedPersistenceService adps)
            {
                adps.DoCopyVersionSubTable(context, newVersion);
            }
        }

        /// <summary>
        /// Insert the model using theproper type of persistence based on class code or class
        /// </summary>
        /// <param name="context">The context where the data is to be inserted</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted data</returns>
        protected override Act DoInsertModel(DataContext context, Act data)
        {
            switch (data)
            {
                case Account acct:
                    return this.GetRelatedPersistenceService<Account>().Insert(context, acct);
                case CarePlan cp:
                    return this.GetRelatedPersistenceService<CarePlan>().Insert(context, cp);
                case ControlAct ca:
                    return this.GetRelatedPersistenceService<ControlAct>().Insert(context, ca);
                case FinancialContract fc:
                    return this.GetRelatedPersistenceService<FinancialContract>().Insert(context, fc);
                case FinancialTransaction ft:
                    return this.GetRelatedPersistenceService<FinancialTransaction>().Insert(context, ft);
                case InvoiceElement ie:
                    return this.GetRelatedPersistenceService<InvoiceElement>().Insert(context, ie);
                case Narrative nv:
                    return this.GetRelatedPersistenceService<Narrative>().Insert(context, nv);
                case QuantityObservation qo:
                    return this.GetRelatedPersistenceService<QuantityObservation>().Insert(context, qo);
                case CodedObservation co:
                    return this.GetRelatedPersistenceService<CodedObservation>().Insert(context, co);
                case TextObservation to:
                    return this.GetRelatedPersistenceService<TextObservation>().Insert(context, to);
                case PatientEncounter pe:
                    return this.GetRelatedPersistenceService<PatientEncounter>().Insert(context, pe);
                case Procedure pr:
                    return this.GetRelatedPersistenceService<Procedure>().Insert(context, pr);
                case SubstanceAdministration sa:
                    return this.GetRelatedPersistenceService<SubstanceAdministration>().Insert(context, sa);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Act)service.Insert(context, data);
                    }
                    else
                    {
                        return base.DoInsertModel(context, data);
                    }
            }
        }

        /// <summary>
        /// Update the model using theproper type of persistence based on class code or class
        /// </summary>
        /// <param name="context">The context where the data is to be update</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted data</returns>
        protected override Act DoUpdateModel(DataContext context, Act data)
        {
            switch (data)
            {
                case Account acct:
                    return this.GetRelatedPersistenceService<Account>().Update(context, acct);
                case CarePlan cp:
                    return this.GetRelatedPersistenceService<CarePlan>().Update(context, cp);
                case ControlAct ca:
                    return this.GetRelatedPersistenceService<ControlAct>().Update(context, ca);
                case FinancialContract fc:
                    return this.GetRelatedPersistenceService<FinancialContract>().Update(context, fc);
                case FinancialTransaction ft:
                    return this.GetRelatedPersistenceService<FinancialTransaction>().Update(context, ft);
                case InvoiceElement ie:
                    return this.GetRelatedPersistenceService<InvoiceElement>().Update(context, ie);
                case Narrative nv:
                    return this.GetRelatedPersistenceService<Narrative>().Update(context, nv);
                case QuantityObservation qo:
                    return this.GetRelatedPersistenceService<QuantityObservation>().Update(context, qo);
                case CodedObservation co:
                    return this.GetRelatedPersistenceService<CodedObservation>().Update(context, co);
                case TextObservation to:
                    return this.GetRelatedPersistenceService<TextObservation>().Update(context, to);
                case PatientEncounter pe:
                    return this.GetRelatedPersistenceService<PatientEncounter>().Update(context, pe);
                case Procedure pr:
                    return this.GetRelatedPersistenceService<Procedure>().Update(context, pr);
                case SubstanceAdministration sa:
                    return this.GetRelatedPersistenceService<SubstanceAdministration>().Update(context, sa);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Act)service.Update(context, data);
                    }
                    else
                    {
                        return base.DoUpdateModel(context, data);
                    }
            }
        }
    }
}
