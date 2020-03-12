/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using SanteDB.Core.Model.Query;
using System.Diagnostics.Tracing;
using System.Data.Common;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Bundle persistence service
    /// </summary>
    public class BundlePersistenceService : AdoBasePersistenceService<Bundle>, IReportProgressChanged
    {
        // Progress has changed
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Bundle modelInstance, DataContext context)
        {
            return m_mapper.MapModelInstance<Bundle, Object>(modelInstance);
        }


        /// <summary>
        /// Reorganize all the major items for insert
        /// </summary>
        private Bundle ReorganizeForInsert(Bundle bundle)
        {
            Bundle retVal = new Bundle() { Item = new List<IdentifiedData>() };
            foreach (var itm in bundle.Item.Where(o => o != null))
            {
                this.m_tracer.TraceInfo("Reorganizing {0}..", itm.Key);
                // Are there any relationships
                if (itm is Entity)
                {
                    var ent = itm as Entity;
                    foreach (var rel in ent.Relationships)
                    {
                        this.m_tracer.TraceInfo("Processing {0} / relationship / {1} ..", itm.Key, rel.TargetEntityKey);

                        var bitm = bundle.Item.FirstOrDefault(o => o.Key == rel.TargetEntityKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.TargetEntityKey))
                            continue;
                        this.m_tracer.TraceInfo("Bumping (due to relationship): {0}", bitm);

                        retVal.Item.Add(bitm); // make sure it gets inserted first
                    }

                }
                else if (itm is Act)
                {
                    var act = itm as Act;
                    foreach (var rel in act.Relationships)
                    {
                        this.m_tracer.TraceInfo("Processing {0} / relationship / {1} ..", itm.Key, rel.TargetActKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.TargetActKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.TargetActKey))
                            continue;
                        this.m_tracer.TraceInfo("Bumping (due to relationship): {0}", bitm);
                        retVal.Item.Add(bitm); // make sure it gets inserted first
                    }

                    foreach (var rel in act.Participations)
                    {
                        this.m_tracer.TraceInfo("Processing {0} / participation / {1} ..", itm.Key, rel.PlayerEntityKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.PlayerEntityKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.PlayerEntityKey))
                            continue;

                        this.m_tracer.TraceInfo("Bumping (due to participation): {0}", bitm);
                        retVal.Item.Add(bitm); // make sure it gets inserted first
                    }


                    // Old versions of the mobile had an issue with missing record targets
                    if (this.m_persistenceService.GetConfiguration().DataCorrectionKeys.Contains("correct-missing-rct"))
                    {
                        var patientEncounter = bundle.Item.OfType<PatientEncounter>().FirstOrDefault();

                        if (patientEncounter != null)
                        {
                            var rct = act.Participations.FirstOrDefault(o => o.ParticipationRoleKey == SanteDB.Core.Model.Constants.ActParticipationKey.RecordTarget);
                            if (!(rct == null || rct.PlayerEntityKey.HasValue))
                            {
                                var perct = patientEncounter.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);

                                act.Participations.Remove(rct);
                                act.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, perct.PlayerEntityKey));
                            }
                        }
                    }
                }

                this.m_tracer.TraceInfo("Re-adding: {0}", itm);
                retVal.Item.Add(itm);
            }

            return retVal;
        }

        /// <summary>
        /// Insert or update contents of the bundle
        /// </summary>
        /// <returns></returns>
        public override Bundle InsertInternal(DataContext context, Bundle data)
        {

            this.m_tracer.TraceInfo("Bundle has {0} objects...", data.Item.Count);
            data = this.ReorganizeForInsert(data);
            this.m_tracer.TraceInfo("After reorganization has {0} objects...", data.Item.Count);

            if (this.m_persistenceService.GetConfiguration().PrepareStatements)
                context.PrepareStatements = true;

            // Ensure that provenance objects match
            var operationalItems = data.Item.Where(o => !data.ExpansionKeys.Any(k => o.Key == k)).ToArray();
            var provenance = operationalItems.OfType<NonVersionedEntityData>().Select(o => o.UpdatedByKey.GetValueOrDefault()).Union(operationalItems.OfType<BaseEntityData>().Select(o => o.CreatedByKey.GetValueOrDefault())).Where(o => o != Guid.Empty);
            if(provenance.Distinct().Count() > 1)
                this.m_tracer.TraceError("PROVENANCE OF OBJECTS DO NOT MATCH. WHEN A BUNDLE IS PERSISTED PROVENANCE DATA MUST BE NULL OR MUST MATCH. {0}", String.Join(",", provenance.Distinct().Select(o=>o.ToString())));

            for (int i = 0; i < data.Item.Count; i++)
            {
                var itm = data.Item[i];
                var svc = this.m_persistenceService.GetPersister(itm.GetType());

                if (data.ExpansionKeys.Any(k => itm.Key == k)) continue; // skip refs

                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)(i + 1) / data.Item.Count, itm));
                try
                {
                    if (svc == null)
                        throw new InvalidOperationException($"Cannot find persister for {itm.GetType()}");
                    
                    if (itm.TryGetExisting(context, true) != null)
                    {
                        this.m_tracer.TraceInfo("Will update {0} object from bundle...", itm);
                        data.Item[i] = svc.Update(context, itm) as IdentifiedData;
                    }
                    else
                    {
                        this.m_tracer.TraceInfo("Will insert {0} object from bundle...", itm);
                        data.Item[i] = svc.Insert(context, itm) as IdentifiedData;
                    }
                }
                catch (TargetInvocationException e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error,  "Error inserting bundle: {0}", e);
                    throw e.InnerException;
                }
                catch(DetectedIssueException e)
                {
                    this.m_tracer.TraceError("### Error Inserting Bundle[{0}]:", i);
                    foreach (var iss in e.Issues)
                        this.m_tracer.TraceError("\t{0}: {1}", iss.Priority, iss.Text);
                    throw new DetectedIssueException(e.Issues, $"Could not insert bundle due to sub-object persistence (at item {i})", e);
                }
                catch(DbException e)
                {
                    try
                    {
                        this.TranslateDbException(e);
                    }
                    catch(Exception e2)
                    {
                        throw new Exception($"Could not insert bundle due to sub-object persistence (bundle item {i})", e2);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not insert bundle due to sub-object persistence (bundle item {i})", e);
                }

            }

            // Cache items
            foreach (var itm in data.Item)
            {
                itm.LoadState = LoadState.FullLoad;
                context.AddCacheCommit(itm);
            }
            return data;
        }

        /// <summary>
        /// Obsolete each object in the bundle
        /// </summary>
        public override Bundle ObsoleteInternal(DataContext context, Bundle data)
        {

            foreach (var itm in data.Item)
            {
                var idp = typeof(IDataPersistenceService<>).MakeGenericType(new Type[] { itm.GetType() });
                var svc = ApplicationServiceContext.Current.GetService(idp);
                var mi = svc.GetType().GetRuntimeMethod("Obsolete", new Type[] { typeof(DataContext), itm.GetType(), typeof(IPrincipal) });

                itm.CopyObjectData(mi.Invoke(ApplicationServiceContext.Current.GetService(idp), new object[] { context, itm }));
            }
            return data;
        }

        /// <summary>
        /// Query the specified object
        /// </summary>
        public override IEnumerable<Bundle> QueryInternal(DataContext context, Expression<Func<Bundle, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<Bundle>[] orderBy, bool countResults = true)
        {
            totalResults = 0;
            return new List<Bundle>().AsQueryable();
        }

        /// <summary>
        /// Model instance 
        /// </summary>
        public override Bundle ToModelInstance(object dataInstance, DataContext context)
        {
            return m_mapper.MapModelInstance<Object, Bundle>(dataInstance);

        }

        /// <summary>
        /// Update all items in the bundle
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <param name="principal"></param>
        /// <returns></returns>
        public override Bundle UpdateInternal(DataContext context, Bundle data)
        {
            return this.InsertInternal(context, data);
        }


    }
}
