/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System;
using System.Collections;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Concept persistence service
    /// </summary>
    public class ConceptPersistenceService : VersionedDataPersistenceService<Core.Model.DataTypes.Concept, DbConceptVersion, DbConcept>
    {
        /// <summary>
        /// To morel instance
        /// </summary>
        public override Core.Model.DataTypes.Concept ToModelInstance(object dataInstance, DataContext context)
        {
            var dbConceptVersion = (dataInstance as CompositeResult)?.Values.OfType<DbConceptVersion>().FirstOrDefault() ?? dataInstance as DbConceptVersion;
            var retVal = m_mapper.MapDomainInstance<DbConceptVersion, Concept>(dbConceptVersion);

            if (retVal == null) return null;

            if (dbConceptVersion != null && context.LoadState == Core.Model.LoadState.FullLoad)
            {
                var dbConcept = (dataInstance as CompositeResult)?.Values.OfType<DbConcept>().FirstOrDefault() ?? context.FirstOrDefault<DbConcept>(o => o.Key == dbConceptVersion.Key);
                retVal.IsReadonly = dbConcept.IsReadonly;
            }
            //retVal.ConceptSetsXml = de.Concept.ConceptSetMembers.Select(o => o.ConceptSetId).ToList();

            if (context.LoadState == Core.Model.LoadState.FullLoad)
            {
                retVal.LoadAssociations(context);
                retVal.LoadState = Core.Model.LoadState.FullLoad;
            }
            else
            {
                retVal.LoadState = Core.Model.LoadState.PartialLoad;
                retVal.ConceptNames = context.Query<DbConceptName>(o => o.SourceKey == retVal.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => new ConceptName(o.Language, o.Name)).ToList();
                retVal.ReferenceTerms = context.Query<DbConceptReferenceTerm>(o => o.SourceKey == retVal.Key).Select(o => new ConceptReferenceTerm(o.TargetKey, o.RelationshipTypeKey)).ToList();
                
            }
            return retVal;
        }

        /// <summary>
        /// Insert concept 
        /// </summary>
        public override Core.Model.DataTypes.Concept InsertInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            data.StatusConceptKey = data.StatusConceptKey ?? StatusKeys.Active;
            data.ClassKey = data.ClassKey ?? ConceptClassKeys.Other;

            // Ensure exists
            if(data.Class != null) data.Class = data.Class?.EnsureExists(context) as ConceptClass;
            if(data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            data.ClassKey = data.Class?.Key ?? data.ClassKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;

            // Persist
            var retVal = base.InsertInternal(context, data);

            // Concept sets xml
            if (data.ConceptSetsXml != null)
                foreach (var i in data.ConceptSetsXml)
                {
                    context.Insert(new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        ConceptSetKey = i
                    }
                    );
                }

            // Concept names
            if (data.ConceptNames != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptName, DbConceptName>(
                   data.ConceptNames,
                    data,
                    context
                );

            if (data.ReferenceTerms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptReferenceTerm, DbConceptReferenceTerm>(
                    data.ReferenceTerms,
                     data,
                     context
                 );

            return retVal;
        }

        /// <summary>
        /// Override update to handle associated items
        /// </summary>
        public override Core.Model.DataTypes.Concept UpdateInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            if (data.Class != null) data.Class = data.Class?.EnsureExists(context) as ConceptClass;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            data.ClassKey = data.Class?.Key ?? data.ClassKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;

            var retVal = base.UpdateInternal(context, data);

            var sourceKey = data.Key.Value.ToByteArray();
            if (data.ConceptNames != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptName, DbConceptName>(
                    data.ConceptNames,
                     data,
                     context
                 );

            if (retVal.ReferenceTerms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptReferenceTerm, DbConceptReferenceTerm>(
                    data.ReferenceTerms,
                     data,
                     context
                 );

            // Concept sets 
            if (retVal.ConceptSetsXml != null)
            {
                // Special case m2m
                var existingConceptSets = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptKey == retVal.Key).Select(o=>o.ConceptSetKey);
                
                // Any new?
                var newConcepts = data.ConceptSetsXml.Where(o => !existingConceptSets.Contains(o));
                foreach (var i in newConcepts)
                {
                    //i.EnsureExists(context);
                    context.Insert(new DbConceptSetConceptAssociation() { ConceptKey = retVal.Key.Value, ConceptSetKey = i });
                }

                var delConcepts = existingConceptSets.ToList().Where(o => !data.ConceptSetsXml.Contains(o));
                foreach (var i in delConcepts)
                    context.Delete<DbConceptSetConceptAssociation>(p => p.ConceptKey == retVal.Key.Value && p.ConceptSetKey == i);
            }

            return retVal;
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override Core.Model.DataTypes.Concept ObsoleteInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            data.StatusConceptKey = StatusKeys.Obsolete;
            return base.UpdateInternal(context, data);
        }

    }

    /// <summary>
    /// Persistence service for concept names
    /// </summary>
    public class ConceptNamePersistenceService : IdentifiedPersistenceService<Core.Model.DataTypes.ConceptName, DbConceptName>, IAdoAssociativePersistenceService
    {

        /// <summary>
        /// Get names from source
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <param name="versionSequenceId"></param>
        /// <param name="principal"></param>
        /// <returns></returns>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            return this.QueryInternal(context, this.BuildSourceQuery<ConceptName>(id, versionSequenceId), Guid.Empty, 0, null, out tr, null, false).ToList();
        }

        /// <summary>
        /// Insert the specified data
        /// </summary>
        public override ConceptName InsertInternal(DataContext context, ConceptName data)
        {
            // Does this inbound not have an ID, and if so, does the concept already have the same name language pair?
            if (!data.Key.HasValue)
            {
                var existing = context.FirstOrDefault<DbConceptName>(o => o.Language == data.Language && o.SourceKey == data.SourceEntityKey && !o.ObsoleteVersionSequenceId.HasValue);
                if (existing != null) // Obsolete the existing 
                {
                    existing.ObsoleteVersionSequenceId = data.SourceEntity?.VersionSequence ?? context.FirstOrDefault<DbConceptVersion>(o => o.Key == data.SourceEntityKey && o.ObsoletionTime == null)?.VersionSequenceId;
                    context.Update(existing);
                }

            }

            // Remove concept from cache and create a new version
            ApplicationContext.Current.GetService<IDataCachingService>().Remove(data.SourceEntityKey.Value);
            var conceptVersion = context.FirstOrDefault<DbConceptVersion>(o => o.Key == data.SourceEntityKey.Value && o.ObsoletionTime == null);
            var newVersion = new DbConceptVersion();
            conceptVersion.ObsoletedByKey = context.ContextId;
            conceptVersion.ObsoletionTime = DateTimeOffset.Now;
            context.Update(conceptVersion);
            
            // Insert new version
            conceptVersion.VersionSequenceId = null;
            conceptVersion.ReplacesVersionKey = conceptVersion.VersionKey;

            conceptVersion.VersionKey = Guid.Empty;
            conceptVersion.ObsoletionTime = null;
            conceptVersion.ObsoletedByKey = null;
            conceptVersion.CreatedByKey = context.ContextId;
            conceptVersion.CreationTime = DateTime.MinValue;
            conceptVersion= context.Insert(conceptVersion);

            data.EffectiveVersionSequenceId = conceptVersion.VersionSequenceId;
            return base.InsertInternal(context, data);

        }
    }
}
