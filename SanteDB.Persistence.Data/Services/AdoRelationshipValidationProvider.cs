using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Concepts;
using SanteDB.Persistence.Data.Model.System;
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
        public Guid SourceClassKey { get; }

        /// <summary>
        /// Gets the target classification key
        /// </summary>
        public Guid TargetClassKey { get; }

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

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoRelationshipValidationProvider(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Relationship Validation Service";

        /// <inheritdoc/>
        public IRelationshipValidationRule AddValidRelationship(Guid sourceClassKey, Guid targetClassKey, Guid relationshipTypeKey, string description)
        {
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {

                try
                {

                    var conceptSet = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptKey == relationshipTypeKey).Select(o => o.SourceKey).FirstOrDefault();

                    DbRelationshipValidationRule dbInstance = null;

                    if (conceptSet == ConceptSetKeys.EntityRelationshipType)
                    {
                        dbInstance = new DbEntityRelationshipValidationRule();
                    }
                    else if (conceptSet == ConceptSetKeys.ActRelationshipType)
                    {
                        dbInstance = new DbActRelationshipValidationRule();
                    }
                    else if (conceptSet == ConceptSetKeys.ActParticipationType)
                    {
                        dbInstance = new DbActParticipationValidationRule();
                    }
                    else
                    {
                        throw new InvalidOperationException(ErrorMessages.INVALID_CLASS_CODE);
                    }

                    dbInstance.Description = description;
                    dbInstance.RelationshipTypeKey = relationshipTypeKey;
                    dbInstance.SourceClassKey = sourceClassKey;
                    dbInstance.TargetClassKey = targetClassKey;
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
        public IEnumerable<IRelationshipValidationRule> GetValidRelationships()
        {
            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                foreach (var itm in
                    context.Query<DbEntityRelationshipValidationRule>(o => true)
                    .Union(context.Query<DbActRelationshipValidationRule>(o => true))
                    .Union(context.Query<DbActParticipationValidationRule>(o => true)))
                {
                    yield return new AdoRelationshipValidationRule(itm as DbActRelationshipValidationRule);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IRelationshipValidationRule> GetValidRelationships(Guid sourceClassKey)
        {
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                foreach (var itm in
                    context.Query<DbEntityRelationshipValidationRule>(o => o.SourceClassKey == sourceClassKey)
                    .Union(context.Query<DbActRelationshipValidationRule>(o => o.SourceClassKey == sourceClassKey))
                    .Union(context.Query<DbActParticipationValidationRule>(o => o.SourceClassKey == sourceClassKey)))
                {
                    yield return new AdoRelationshipValidationRule(itm as DbActRelationshipValidationRule);
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveValidRelationship(Guid sourceClassKey, Guid targetClassKey, Guid relationshipTypeKey)
        {
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.DeleteAll<DbEntityRelationshipValidationRule>(o => o.SourceClassKey == sourceClassKey && o.TargetClassKey == targetClassKey && o.RelationshipTypeKey == relationshipTypeKey);
                    context.DeleteAll<DbActRelationshipValidationRule>(o => o.SourceClassKey == sourceClassKey && o.TargetClassKey == targetClassKey && o.RelationshipTypeKey == relationshipTypeKey);
                    context.DeleteAll<DbActParticipationValidationRule>(o => o.SourceClassKey == sourceClassKey && o.TargetClassKey == targetClassKey && o.RelationshipTypeKey == relationshipTypeKey);
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
