/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
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
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Bundle persistence service
    /// </summary>
    public class BundlePersistenceService : AdoBasePersistenceService<Bundle>, IReportProgressChanged
    {
        // Progress has changed
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public BundlePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Bundles don't really exist
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return false;
        }

        /// <summary>
        /// From model instance
        /// </summary>
        public override object FromModelInstance(Bundle modelInstance, DataContext context)
        {
            return this.m_settingsProvider.GetMapper().MapModelInstance<Bundle, Object>(modelInstance);
        }


        /// <summary>
        /// Reorganize all the major items for insert
        /// </summary>
        /// TODO: Refactor this (clean it up)
        private Bundle ReorganizeForInsert(Bundle bundle)
        {
            Bundle retVal = new Bundle() { Item = new List<IdentifiedData>() };
            foreach (var itm in bundle.Item.Where(o => o != null).Distinct())
            {
                this.m_tracer.TraceVerbose("Reorganizing {0}..", itm.Key);
                var idx = retVal.Item.FindIndex(o => o.Key == itm.Key);

                // Are there any relationships
                if (itm is Entity ent)
                {
                    foreach (var rel in ent.Relationships)
                    {
                        this.m_tracer.TraceVerbose("Processing {0} / relationship / {1} ..", itm.Key, rel.TargetEntityKey);

                        var bitm = bundle.Item.FirstOrDefault(o => o.Key == rel.TargetEntityKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.TargetEntityKey))
                            continue;
                        this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);

                        if (idx > -1)
                            retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                        else
                            retVal.Item.Add(bitm);
                    }

                }
                else if (itm is Act act)
                {
                    foreach (var rel in act.Relationships)
                    {
                        this.m_tracer.TraceVerbose("Processing {0} / relationship / {1} ..", itm.Key, rel.TargetActKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.TargetActKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.TargetActKey))
                            continue;
                        this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                        if (idx > -1)
                            retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                        else
                            retVal.Item.Add(bitm);

                    }

                    foreach (var rel in act.Participations)
                    {
                        this.m_tracer.TraceVerbose("Processing {0} / participation / {1} ..", itm.Key, rel.PlayerEntityKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.PlayerEntityKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.PlayerEntityKey))
                            continue;

                        this.m_tracer.TraceVerbose("Bumping (due to participation): {0}", bitm);
                        if (idx > -1)
                            retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                        else
                            retVal.Item.Add(bitm);

                    }
                }
                else if (itm is Concept concept) {
                    foreach (var rel in concept.ReferenceTerms)
                    {
                        this.m_tracer.TraceVerbose("Processing {0} / referenceTerm / {1} ..", itm.Key, rel.ReferenceTermKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.ReferenceTermKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.ReferenceTermKey))
                            continue;
                        this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                        if (idx > -1)
                            retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                        else
                            retVal.Item.Add(bitm);
                    }

                    foreach (var rel in concept.Relationship)
                    {
                        this.m_tracer.TraceVerbose("Processing {0} / relationship / {1} ..", itm.Key, rel.TargetConceptKey);
                        var bitm = bundle.Item?.FirstOrDefault(o => o.Key == rel?.TargetConceptKey);
                        if (bitm == null) continue;

                        if (retVal.Item.Any(o => o.Key == rel.TargetConceptKey))
                            continue;
                        this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                        if (idx > -1)
                            retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                        else
                            retVal.Item.Add(bitm);
                    }
                }
                else if (itm is EntityRelationship entRel)
                {
                    var bitm = bundle.Item.FirstOrDefault(o => o.Key == entRel.TargetEntityKey);

                    if (bitm != null)
                    {
                        if (!retVal.Item.Any(o => o.Key == entRel.TargetEntityKey))
                        {
                            this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                            if (idx > -1)
                                retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                            else
                                retVal.Item.Add(bitm);
                        }
                    }

                    bitm = bundle.Item.FirstOrDefault(o => o.Key == entRel.SourceEntityKey);
                    if (bitm != null)
                    {
                        if (!retVal.Item.Any(o => o.Key == entRel.SourceEntityKey))
                        {
                            this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                            if (idx > -1)
                                retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                            else
                                retVal.Item.Add(bitm);
                        }
                    }
                }
                else if (itm is ActRelationship actRel)
                {
                    var bitm = bundle.Item.FirstOrDefault(o => o.Key == actRel.TargetActKey);
                    if (bitm != null)
                    {
                        if (!retVal.Item.Any(o => o.Key == actRel.TargetActKey))
                        {
                            this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                            if (idx > -1)
                                retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                            else
                                retVal.Item.Add(bitm);
                        }
                    }
                    bitm = bundle.Item.FirstOrDefault(o => o.Key == actRel.SourceEntityKey);
                    if (bitm != null)
                    {
                        if (!retVal.Item.Any(o => o.Key == actRel.SourceEntityKey))
                        {
                            this.m_tracer.TraceVerbose("Bumping (due to relationship): {0}", bitm);
                            if (idx > -1)
                                retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                            else
                                retVal.Item.Add(bitm);
                        }
                    }
                }
                else if (itm is ActParticipation actPtcpt)
                {
                    var bitm = bundle.Item.FirstOrDefault(o => o.Key == actPtcpt.PlayerEntityKey);

                    if (bitm != null)
                    {
                        if (!retVal.Item.Any(o => o.Key == actPtcpt.PlayerEntityKey))
                        {
                            this.m_tracer.TraceVerbose("Bumping (due to participation): {0}", bitm);
                            if (idx > -1)
                                retVal.Item.Insert(idx, bitm); // make sure it gets inserted first
                            else
                                retVal.Item.Add(bitm);
                        }
                    }
                }
                this.m_tracer.TraceVerbose("Re-adding: {0}", itm);
                if (!itm.Key.HasValue || !retVal.Item.Any(o => o.Key == itm.Key))
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

            this.m_tracer.TraceVerbose("Bundle has {0} objects...", data.Item.Count);
            var reorganized = this.ReorganizeForInsert(data);
            this.m_tracer.TraceVerbose("After reorganization has {0} objects...", reorganized.Item.Count);

            context.PrepareStatements = this.m_settingsProvider.GetConfiguration().PrepareStatements;

            // Ensure that provenance objects match
            var operationalItems = reorganized.Item.Where(o => !reorganized.ExpansionKeys.Any(k => o.Key == k)).ToArray();
            var provenance = operationalItems.OfType<NonVersionedEntityData>().Select(o => o.UpdatedByKey.GetValueOrDefault()).Union(operationalItems.OfType<BaseEntityData>().Select(o => o.CreatedByKey.GetValueOrDefault())).Where(o => o != Guid.Empty);
            if (provenance.Distinct().Count() > 1)
                this.m_tracer.TraceError("PROVENANCE OF OBJECTS DO NOT MATCH. WHEN A BUNDLE IS PERSISTED PROVENANCE DATA MUST BE NULL OR MUST MATCH. {0}", String.Join(",", provenance.Distinct().Select(o => o.ToString())));

            for (int i = 0; i < reorganized.Item.Count; i++)
            {
                var itm = reorganized.Item[i];
                var svc = this.m_settingsProvider.GetPersister(itm.GetType());

                if (reorganized.ExpansionKeys.Any(k => itm.Key == k)) continue; // skip refs

                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)(i + 1) / reorganized.Item.Count, itm));
                try
                {
                    if (svc == null)
                        throw new InvalidOperationException($"Cannot find persister for {itm.GetType()}");

                    if (itm.CheckExists(context))
                    {
                        this.m_tracer.TraceVerbose("Will update {0} object from bundle...", itm);
                        reorganized.Item[i] = svc.Update(context, itm) as IdentifiedData;
                    }
                    else
                    {
                        this.m_tracer.TraceVerbose("Will insert {0} object from bundle...", itm);
                        reorganized.Item[i] = svc.Insert(context, itm) as IdentifiedData;
                    }
                }
                catch (TargetInvocationException e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error inserting bundle: {0}", e);
                    throw new Exception($"Error inserting bundle at item #{i}", e.InnerException);
                }
                catch (DetectedIssueException e)
                {
                    this.m_tracer.TraceError("### Error Inserting Bundle[{0} / {1}]:", i, data.Item.FindIndex(o=>o.Key == itm.Key));
                    foreach (var iss in e.Issues)
                        this.m_tracer.TraceError("\t{0}: {1}", iss.Priority, iss.Text);
                    throw new DetectedIssueException(e.Issues, $"Could not insert bundle due to sub-object persistence (at item {i})", e);
                }
                catch (DbException e)
                {
                    throw new DataPersistenceException($"Cannot insert bundle object {itm} @ {i} - {e.Message}", this.TranslateDbException(e));
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
            return this.m_settingsProvider.GetMapper().MapModelInstance<Object, Bundle>(dataInstance);

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
