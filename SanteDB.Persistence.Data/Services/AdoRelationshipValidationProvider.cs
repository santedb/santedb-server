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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Concepts;
using SanteDB.Persistence.Data.Model.Sys;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{

    /// <summary>
    /// ADO Relationship validation rule
    /// </summary>
    public class AdoRelationshipValidationRule : IRelationshipValidationRule
    {

        /// <summary>
        /// Create a new relationship validation rule based on a database relationship rule
        /// </summary>
        internal AdoRelationshipValidationRule(DbRelationshipValidationRule rule)
        {

            this.SourceClassKey = rule.SourceClassKey;
            this.TargetClassKey = rule.TargetClassKey;
            this.RelationshipTypeKey = rule.RelationshipTypeKey;
            this.Description = rule.Description;
        }

        /// <summary>
        /// Create a new validation rule 
        /// </summary>
        internal AdoRelationshipValidationRule(Guid sourceClassKey, Guid targetClassKey, Guid relationshipType, String description)
        {
            this.SourceClassKey = sourceClassKey;
            this.TargetClassKey = targetClassKey;
            this.RelationshipTypeKey = relationshipType;
            this.Description = description;
        }

        /// <summary>
        /// Gets the source classification key
        /// </summary>
        public Guid? SourceClassKey { get; }

        /// <summary>
        /// Gets the target classification key
        /// </summary>
        public Guid? TargetClassKey { get; }

        /// <summary>
        /// Gets the type of relationship
        /// </summary>
        public Guid RelationshipTypeKey { get; }

        /// <summary>
        /// Gets the description of the relationship
        /// </summary>
        public string Description { get; }

    }

    /// <summary>
    /// ADO.NET Based Relationship Provider
    /// </summary>
    /// <remarks>This class allows for the management of validation rules between entities, 
    /// acts, or entities and acts</remarks>
    public class AdoRelationshipValidationProvider : IRelationshipValidationProvider
    {
        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoRelationshipValidationProvider));

        // Configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoRelationshipValidationProvider(IConfigurationManager configurationManager, IPolicyEnforcementService pepService)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Relationship Validation Service";

        /// <inheritdoc/>
        public IRelationshipValidationRule AddValidRelationship<TRelationship>(Guid? sourceClassKey, Guid? targetClassKey, Guid relationshipTypeKey, string description)
            where TRelationship : ITargetedAssociation
        {

            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {

                try
                {

                    context.Open();
                    var dbInstance = new DbRelationshipValidationRule()
                    {
                        Description = description,
                        RelationshipTypeKey = relationshipTypeKey,
                        SourceClassKey = sourceClassKey,
                        TargetClassKey = targetClassKey,
                        RelationshipClassType = typeof(TRelationship) == typeof(EntityRelationship) ? RelationshipTargetType.EntityRelationship :
                            typeof(TRelationship) == typeof(ActRelationship) ? RelationshipTargetType.ActRelationship : RelationshipTargetType.ActParticipation
                    };

                    return new AdoRelationshipValidationRule(context.Insert(dbInstance));
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException($"Error creating validation rule {sourceClassKey}-[{relationshipTypeKey}]->{targetClassKey}", e);
                }

            }
        }

        /// <inheritdoc/>
        public IEnumerable<IRelationshipValidationRule> GetValidRelationships<TRelationship>()
            where TRelationship : ITargetedAssociation
        {
            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                var tclass = typeof(TRelationship) == typeof(EntityRelationship) ? RelationshipTargetType.EntityRelationship :
                            typeof(TRelationship) == typeof(ActRelationship) ? RelationshipTargetType.ActRelationship : RelationshipTargetType.ActParticipation;
                context.Open();

                foreach (var itm in context.Query<DbRelationshipValidationRule>(o => o.RelationshipClassType == tclass))
                {
                    yield return new AdoRelationshipValidationRule(itm);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IRelationshipValidationRule> GetValidRelationships<TRelationship>(Guid sourceClassKey)
            where TRelationship : ITargetedAssociation
        {
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                var tclass = typeof(TRelationship) == typeof(EntityRelationship) ? RelationshipTargetType.EntityRelationship :
                           typeof(TRelationship) == typeof(ActRelationship) ? RelationshipTargetType.ActRelationship : RelationshipTargetType.ActParticipation;
                context.Open();

                foreach (var itm in context.Query<DbRelationshipValidationRule>(o => (o.SourceClassKey == sourceClassKey || o.SourceClassKey == null) && o.RelationshipClassType == tclass))
                {
                    yield return new AdoRelationshipValidationRule(itm);
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveValidRelationship<TRelationship>(Guid? sourceClassKey, Guid? targetClassKey, Guid relationshipTypeKey)
            where TRelationship : ITargetedAssociation
        {
            this.m_pepService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    var tclass = typeof(TRelationship) == typeof(EntityRelationship) ? RelationshipTargetType.EntityRelationship :
                           typeof(TRelationship) == typeof(ActRelationship) ? RelationshipTargetType.ActRelationship : RelationshipTargetType.ActParticipation;
                    context.DeleteAll<DbRelationshipValidationRule>(o => o.SourceClassKey == sourceClassKey && o.TargetClassKey == targetClassKey && o.RelationshipTypeKey == relationshipTypeKey && o.RelationshipClassType == tclass);
                }
                catch (DbException e)
                {
                    throw e.TranslateDbException();
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException($"Error creating validation rule {sourceClassKey}-[{relationshipTypeKey}]->{targetClassKey}", e);
                }
            }
        }
    }
}
