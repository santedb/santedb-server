/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-9-7
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Persistence service for concepts
    /// </summary>
    public class ConceptPersistenceService : VersionedDataPersistenceService<Concept, DbConceptVersion, DbConcept>
    {
        /// <summary>
        /// Creates a DI instance of hte conept persistence service
        /// </summary>
        public ConceptPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// The object <paramref name="key"/> is being purged - delete all references for the object
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            // Delete other objects
            context.DeleteAll<DbConceptName>(o => o.SourceKey == key);
            context.DeleteAll<DbConceptRelationship>(o => o.SourceKey == key);
            context.DeleteAll<DbConceptReferenceTerm>(o => o.SourceKey == key);
            base.DoDeleteReferencesInternal(context, key);
        }

        /// <summary>
        /// Prepare references
        /// </summary>
        protected override Concept BeforePersisting(DataContext context, Concept data)
        {
            if (!data.StatusConceptKey.HasValue)
            {
                // Set NEW as the default status
                data.StatusConceptKey = StatusKeys.New;
            }
            data.ClassKey = this.EnsureExists(context, data.Class)?.Key ?? data.ClassKey;
            data.StatusConceptKey = this.EnsureExists(context, data.StatusConcept)?.Key ?? data.StatusConceptKey;
            return data;
        }

        /// <summary>
        /// Perform an insert of the model properties
        /// </summary>
        protected override Concept DoInsertModel(DataContext context, Concept data)
        {
            // Do Insertion of themodel
            var retVal = base.DoInsertModel(context, data);

            // Insert names
            if (data.ConceptNames != null)
            {
                retVal.ConceptNames = base.UpdateModelVersionedAssociations<ConceptName>(context, retVal, data.ConceptNames).ToList();
                retVal.SetLoaded(o => o.ConceptNames);

            }

            // Concept sets
            if (data.ConceptSetsXml != null)
            {
                retVal.ConceptSetsXml = base.UpdateInternalAssociations<DbConceptSetConceptAssociation>(context, retVal.Key.Value,
                    data.ConceptSetsXml.Select(o => new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        SourceKey = o
                    }), o => o.ConceptKey == retVal.Key).Select(o => o.SourceKey).ToList();
            }

            // Reference terms
            if (data.ReferenceTerms != null)
            {
                retVal.ReferenceTerms = base.UpdateModelVersionedAssociations<ConceptReferenceTerm>(context, retVal, data.ReferenceTerms).ToList();
                retVal.SetLoaded(o => o.ReferenceTerms);

            }

            // Relationships
            if (data.Relationships != null)
            {
                retVal.Relationships = base.UpdateModelVersionedAssociations<ConceptRelationship>(context, retVal, data.Relationships).ToList(); 
                retVal.SetLoaded(o => o.Relationships);

            }
            return retVal;
        }

        /// <summary>
        /// Perform an update of the model and properties
        /// </summary>
        protected override Concept DoUpdateModel(DataContext context, Concept data)
        {
            var retVal = base.DoUpdateModel(context, data);

            // Update names
            if (data.ConceptNames != null)
            {
                retVal.ConceptNames = base.UpdateModelVersionedAssociations<ConceptName>(context, retVal, data.ConceptNames).ToList();
                retVal.SetLoaded(o => o.ConceptNames);

            }

            // Update concept sets
            if (data.ConceptSetsXml != null)
            {
                retVal.ConceptSetsXml = base.UpdateInternalAssociations<DbConceptSetConceptAssociation>(context, retVal.Key.Value,
                    data.ConceptSetsXml.Select(o => new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        SourceKey = o
                    }), o => o.ConceptKey == retVal.Key).Select(o => o.SourceKey).ToList();
            }

            // Update reference terms
            if (data.ReferenceTerms != null)
            {
                retVal.ReferenceTerms = base.UpdateModelVersionedAssociations<ConceptReferenceTerm>(context, retVal, data.ReferenceTerms).ToList();
                retVal.SetLoaded(o => o.ReferenceTerms);

            }

            // Relationships
            if (data.Relationships != null)
            {
                retVal.Relationships = base.UpdateModelVersionedAssociations<ConceptRelationship>(context, retVal, data.Relationships).ToList();
                retVal.SetLoaded(o => o.Relationships);

            }

            return retVal;
        }

        /// <summary>
        /// Convert the <paramref name="dbModel"/> to <typeparamref name="TModel"/>
        /// </summary>
        protected override Concept DoConvertToInformationModel(DataContext context, DbConceptVersion dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.Class = retVal.Class.GetRelatedPersistenceService().Get(context, dbModel.ClassKey);
                    retVal.SetLoaded(nameof(Concept.Class));
                    goto case LoadMode.SyncLoad; // special case - FullLoad implies SyncLoad so we want a fallthrough - the only way to do this in C# is with this messy GOTO stuff
                case LoadMode.SyncLoad:
                    retVal.ConceptNames = retVal.ConceptNames.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Concept.ConceptNames));
                    retVal.Relationships = retVal.Relationships.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Concept.Relationships));
                    retVal.ReferenceTerms = retVal.ReferenceTerms.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key && o.ObsoleteVersionSequenceId == null).ToList();
                    retVal.SetLoaded(nameof(Concept.ReferenceTerms));
                    goto case LoadMode.QuickLoad;
                case LoadMode.QuickLoad:
                    retVal.ConceptSetsXml = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptKey == dbModel.Key).Select(o => o.SourceKey).ToList();
                    retVal.SetLoaded(nameof(Concept.ConceptSets));
                    break;
            }

            return retVal;
        }

        /// <inheritdoc/>
        /// <remarks>This is not required since there are no sub-tables on the concept version</remarks>
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbConceptVersion newVersion)
        {
        }
    }
}