using SanteDB.Core.Model;
using SanteDB.Core.Data;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.OrmLite.Providers;

namespace SanteDB.Persistence.Data.ADO.Index
{
    /// <summary>
    /// A <see cref="IPreparedFilterManager"/> which manages the pre-fetch filtering with ADO data tables
    /// </summary>
    public class AdoPreparedFilterManager : IPreparedFilterManager
    {
        // Configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;

        // Caching service
        private readonly IAdhocCacheService m_adhocCache;

        // PEP
        private readonly IPolicyEnforcementService m_policyService;

        // Mapper
        private readonly ModelMapper m_mapper;

        /// <inheritdoc/>
        public AdoPreparedFilterManager(IConfigurationManager configurationManager, IPolicyEnforcementService pepService, IAdhocCacheService adhocCacheService = null)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_adhocCache = adhocCacheService;
            this.m_policyService = pepService;
            this.m_mapper = new ModelMapper(typeof(AdoPreparedFilter).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName));

        }

        /// <inheritdoc/>
        public string ServiceName => "ADO.NET Pre-Filtering Management";

        /// <inheritdoc/>
        public IPreparedFilter Create<TTarget>(string name, Expression<Func<TTarget, dynamic>> indexExpression, String indexProvider = null)
        {
            if(String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            else if(indexExpression == null)
            {
                throw new ArgumentNullException(nameof(indexExpression));
            }

            this.m_policyService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);

            using(var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    using(var tx = context.BeginTransaction())
                    {

                        if(context.Any<DbPreparedFilterDefinition>(o=>o.Name.ToLowerInvariant() == name.ToLower() && o.ObsoletionTime == null))
                        {
                            throw new InvalidOperationException($"Index {name} already exists");
                        }

                        var indexSelector = QueryExpressionBuilder.BuildPropertySelector(indexExpression);
                        var targetType = $"{typeof(TTarget).FullName}, {typeof(TTarget).Assembly.FullName}";
                        if(context.Any<DbPreparedFilterDefinition>(o=>o.FilterExpression == indexSelector && o.TargetType == targetType && o.ObsoletionTime == null))
                        {
                            throw new InvalidOperationException($"Index {indexSelector} has already been applied to {targetType}");
                        }

                        if(!String.IsNullOrEmpty(indexProvider) && this.m_configuration.Provider.GetIndexFunction(indexProvider) == null)
                        {
                            throw new InvalidOperationException($"Provider {this.m_configuration.Provider.Invariant} does not support {indexProvider}");
                        }

                        var ormType = this.m_mapper.MapModelType(typeof(TTarget));
                        if(ormType == typeof(TTarget))
                        {
                            throw new InvalidOperationException($"{typeof(TTarget)} is not under control of ADO persistence");
                        }

                        var storeTableName = $"pf_{ormType.Name}_";
                        // Insert the data
                        var dbFilterDefinition = context.Insert(new DbPreparedFilterDefinition()
                        {
                            CreatedByKey = context.EstablishProvenance(AuthenticationContext.Current.Principal, null),
                            CreationTime = DateTimeOffset.Now,
                            FilterExpression = indexSelector,
                            Indexer = indexProvider,
                            Name = name,
                            Status = PreparedFilterIndexState.Inactive,
                            TargetType = targetType,
                            Key = Guid.NewGuid(),
                            StoreName = storeTableName
                        });
                        storeTableName = dbFilterDefinition.StoreName += dbFilterDefinition.SequenceId.ToString();
                        context.ExecuteNonQuery(context.CreateSqlStatement($"CREATE TABLE {storeTableName} (")
                            .Append("obj_id UUID NOT NULL,")
                            .Append($"idx_v {this.m_configuration.Provider.MapSchemaDataType(indexExpression.Type)},")
                            .Append($"CONSTRAINT pk_{storeTableName} PRIMARY KEY (obj_id, idx_v)")
                            .Append(")"));

                        // Create default index

                        context.ExecuteNonQuery(this.m_configuration.Provider.CreateIndex($"{storeTableName}_v_idx",storeTableName, "idx_v", false));
                        context.ExecuteNonQuery(this.m_configuration.Provider.CreateIndex($"{storeTableName}_obj_id_idx",storeTableName, "obj_id", false));
                        if(!String.IsNullOrEmpty(indexProvider))
                        {
                            var provider = this.m_configuration.Provider.GetIndexFunction(indexProvider);
                            context.ExecuteNonQuery(provider.CreateIndex($"{storeTableName}_{indexProvider}_idx", storeTableName, "idx_v"));
                        }
                        context.Update(dbFilterDefinition);

                        tx.Commit();

                        return new AdoPreparedFilter(dbFilterDefinition);
                    }
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException($"Error creating prepared filter {name}", e);
                }
            }
        }

        /// <inheritdoc/>
        public IPreparedFilter Delete(Guid indexId)
        {

            this.m_policyService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);

            using (var context= this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();

                    using(var tx = context.BeginTransaction())
                    {
                        var idxRegistration = context.FirstOrDefault<DbPreparedFilterDefinition>(o => o.Key == indexId && o.ObsoletionTime == null);
                        if(idxRegistration == null)
                        {
                            throw new KeyNotFoundException($"Index {indexId} not found");
                        }

                        // Drop the table
                        context.ExecuteNonQuery(context.CreateSqlStatement($"DROP TABLE {idxRegistration.StoreName}"));

                        idxRegistration.ObsoletedByKey = context.EstablishProvenance(AuthenticationContext.Current.Principal, null);
                        idxRegistration.ObsoletionTime = DateTimeOffset.Now;
                        context.Update(idxRegistration);

                        tx.Commit();

                        return new AdoPreparedFilter(idxRegistration);
                    }
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException($"Error dropping prepared filter {indexId}", e);
                }
            }
        }

        /// <inheritdoc/>

        public IPreparedFilter Get(Guid indexId)
        {
            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    var idxRegistration = context.FirstOrDefault<DbPreparedFilterDefinition>(o => o.Key == indexId && o.ObsoletionTime == null);
                    if(idxRegistration == null)
                    {
                        return null;
                    }
                    else
                    {
                        return new AdoPreparedFilter(idxRegistration);
                    }
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException($"Error retrieving prepared filter {indexId}", e);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IPreparedFilter> GetPreparedFilters()
        {
            using(var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    return context.Query<DbPreparedFilterDefinition>(o => o.ObsoletionTime == null).ToList().Select(o => new AdoPreparedFilter(o));
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException("Error fetching prepared filters", e);
                }
            }
        }

        // TODO: Needs to be optimized
        public SqlStatement CreatePopulateSql(IDbProvider provider, DbPreparedFilterDefinition definition, params Guid[] objectIds)
        {

            var modelType = Type.GetType(definition.TargetType);
            var ormType = this.m_mapper.MapModelType(modelType);
            var tableMap = TableMapping.Get(ormType);
            var fromStatement = new SqlStatement(provider, $"FROM {tableMap.TableName}");
            var scopedTables = new List<TableMapping>() { tableMap };

            // Join tables
            while(ormType != modelType)
            {
                modelType = modelType.BaseType;
                ormType = this.m_mapper.MapModelType(modelType);
                if (ormType != modelType && !scopedTables.Any(s=>s.OrmType == ormType))
                {
                    var joinCol = tableMap.Columns.FirstOrDefault(o => o.ForeignKey?.Table == ormType);
                    if(joinCol == null)
                    {
                        throw new InvalidOperationException($"Table {ormType.Name} is not related to {tableMap.OrmType}");
                    }
                    tableMap = TableMapping.Get(ormType);
                    fromStatement.Append($" INNER JOIN {tableMap.TableName} USING ({joinCol.Name})");
                    scopedTables.Add(tableMap);
                }
            }

            // Last table is still versioned - join
            var whereStatement = new SqlStatement(provider);
            if (typeof(IDbVersionedData).IsAssignableFrom(scopedTables.Last().OrmType))
            {
                whereStatement.Append($" {scopedTables.Last().TableName}.obslt_utc IS NULL ");
                foreach (var itm in scopedTables.Last().Columns.Where(o=>o.IsAlwaysJoin))
                {
                    tableMap = TableMapping.Get(itm.ForeignKey.Table);
                    fromStatement.Append($" INNER JOIN {tableMap.TableName} USING ({itm.Name})");
                    scopedTables.Add(tableMap);
                }
            }

            // Process the definition queries to left join the values out
            var predicate = QueryPredicate.Parse(definition.FilterExpression);
            modelType = Type.GetType(definition.TargetType);
            var selectStatement = new SqlStatement(provider, $"SELECT DISTINCT {scopedTables.Last().TableName}.{scopedTables.Last().PrimaryKey.First().Name} ");
            var tableNameStack = new Stack<String>();

            while (!String.IsNullOrEmpty(predicate.Path))
            {
                // Get the model property the predicate matches
                var modelProperty = modelType.GetQueryProperty(predicate.Path, true);
                if(modelProperty == null)
                {
                    throw new MissingMemberException($"Cannot find {predicate.Path}");
                }

                var parentModelType = modelType;
                modelType = modelProperty.PropertyType.StripGeneric();
                ormType = this.m_mapper.MapModelType(modelType);
                

                // Type of JOIN -
                if (typeof(IList).IsAssignableFrom(modelProperty.PropertyType))
                {
                    var leftTableMap = scopedTables.Last();
                    tableMap = TableMapping.Get(ormType);

                    // Is there a direct link?
                    var linkColumn = tableMap.Columns.FirstOrDefault(c => c.ForeignKey?.Table == leftTableMap.OrmType);
                    if(linkColumn == null)
                    {
                        throw new NotSupportedException($"Column expression is too complex to be indexed");
                    }

                    scopedTables.Add(tableMap);

                    var tableAlias = $"T{tableNameStack.Count}";
                    var parentTableAlias = leftTableMap.TableName; // Table alias
                    scopedTables.Add(tableMap);
                    if (tableNameStack.Count > 0)
                    {
                        parentTableAlias = tableNameStack.Peek();
                    }
                    tableNameStack.Push(tableAlias);

                    fromStatement.Append($" LEFT JOIN {tableMap.TableName} AS {tableAlias} ON ({parentTableAlias}.{linkColumn.Name} = {tableAlias}.{linkColumn.Name} ");
                    if(typeof(IDbVersionedAssociation).IsAssignableFrom(ormType))
                    {
                        fromStatement.And($" {tableAlias}.obslt_vrsn_seq_id IS NULL ");
                    }

                    // If guard is applied
                    if(!String.IsNullOrEmpty(predicate.Guard))
                    {
                        var classifierAttribute = modelType.GetCustomAttribute<ClassifierAttribute>();
                        if(classifierAttribute == null)
                        {
                            throw new InvalidOperationException($"Cannot place guard on type {modelType}");
                        }
                        var domainClassifier = this.m_mapper.MapModelProperty(modelType, modelType.GetProperty(classifierAttribute.ClassifierKeyProperty));
                        var domainClassifierColumn = tableMap.GetColumn(domainClassifier);
                        var guardClause = new SqlStatement(provider);
                        foreach(var itm in predicate.Guard.Split('|'))
                        {
                            if(Guid.TryParse(itm, out var uuidFilter))
                            {
                                guardClause.Or($"{tableAlias}.{domainClassifierColumn.Name} = ?", uuidFilter);
                            }
                            else
                            {
                                var classifierLookupType = modelType.GetProperty(classifierAttribute.ClassifierProperty).PropertyType;
                                var classifierLookupAttribute = classifierLookupType.GetCustomAttribute<ClassifierAttribute>();
                                if(classifierLookupAttribute == null)
                                {
                                    throw new InvalidOperationException("Classifier lacks lookup value for this property type");
                                }
                                var classifierLookupTable = TableMapping.Get(this.m_mapper.MapModelType(classifierLookupType));
                                var classifierLookupColumn = classifierLookupTable.GetColumn(this.m_mapper.MapModelProperty(classifierLookupType, classifierLookupType.GetProperty(classifierLookupAttribute.ClassifierProperty)));

                                var subQuery = new SqlStatement(provider).SelectFrom(classifierLookupTable.OrmType, classifierLookupTable.PrimaryKey.First())
                                    .Where($"{classifierLookupColumn.Name} = ?", itm);
                                guardClause.Or($"{tableAlias}.{domainClassifierColumn.Name} IN (")
                                    .Append(subQuery)
                                    .Append(")");
                            }
                        }
                        fromStatement.Append("AND (").Append(guardClause).Append(")");
                    }
                    fromStatement.Append(")");
                }
                else if(typeof(IdentifiedData).IsAssignableFrom(modelType))
                {
                    // Get the column in scoped tables that points to the column
                    var domainProperty = this.m_mapper.MapModelProperty(modelProperty.DeclaringType, modelProperty.GetSerializationRedirectProperty());
                    var leftTableMap = TableMapping.Get(domainProperty.DeclaringType);
                    var leftTableColumn = leftTableMap.GetColumn(domainProperty);
                    tableMap = TableMapping.Get(ormType);
                    var tableAlias = $"T{tableNameStack.Count}";
                    var parentTableAlias = leftTableMap.TableName; // Table alias
                    scopedTables.Add(tableMap);
                    if(tableNameStack.Count > 0)
                    {
                        parentTableAlias = tableNameStack.Peek();
                    }
                    tableNameStack.Push(tableAlias);
                    
                    fromStatement.Append($" INNER JOIN {tableMap.TableName} AS {tableAlias} ON ({parentTableAlias}.{leftTableColumn.Name} = {tableAlias}.{tableMap.GetColumn(leftTableColumn.ForeignKey.Column).Name}");
                    if(typeof(IDbVersionedData).IsAssignableFrom(ormType)) // Add an obslt filter
                    {
                        fromStatement.Append($" AND {tableAlias}.obslt_utc IS NULL ");
                    }
                    fromStatement.Append(")");
                }
                else
                {
                    var localProperty = this.m_mapper.MapModelProperty(parentModelType, modelProperty);

                    // Is there a local property?
                    if (localProperty == null)
                    {
                        tableMap = TableMapping.Get(this.m_mapper.MapModelType(parentModelType));
                        // Is there an auto-join property which contains our desired property?
                        var alwaysJoin = tableMap.Columns.Single(o => o.IsAlwaysJoin);
                        var alwaysJoinTable = TableMapping.Get(alwaysJoin.ForeignKey.Table);

                        var tableAlias = $"T{tableNameStack.Count}";
                        fromStatement.Append($"INNER JOIN {alwaysJoinTable.TableName} as {tableAlias} ON ({tableNameStack.Peek()}.{alwaysJoin.Name} = {tableAlias}.{alwaysJoinTable.PrimaryKey.First().Name})");
                        selectStatement.Append($", {tableAlias}.{alwaysJoinTable.GetColumn(modelProperty).Name} ");

                    }
                    else
                    {
                        var sourceTableMap = TableMapping.Get(localProperty.DeclaringType);
                        selectStatement.Append($", {tableNameStack.Pop()}.{sourceTableMap.GetColumn(localProperty).Name} ");
                    }
                    break;
                }

                predicate = QueryPredicate.Parse(predicate.SubPath);
            }
            return selectStatement.Append(fromStatement).Where(whereStatement);
        }

        /// <inheritdoc/>
        public IPreparedFilter ReCompute(Guid indexId)
        {
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    var definition = context.FirstOrDefault<DbPreparedFilterDefinition>(o => o.Key == indexId && o.ObsoletionTime == null);
                    if(definition == null)
                    {
                        throw new KeyNotFoundException($"Definition {indexId} not found");
                    }

                    return new AdoPreparedFilter(definition);
                    
                }
                catch (Exception e)
                {
                    throw new DataPersistenceException($"Error re-computing entire filter {indexId}", e);
                }
            }
        }

        /// <inheritdoc/>
        public IPreparedFilter SetStatus(Guid indexId, PreparedFilterIndexState newState)
        {

            this.m_policyService.Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration);

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    var idxDefinition = context.FirstOrDefault<DbPreparedFilterDefinition>(o => o.Key == indexId && o.ObsoletionTime == null);
                    if(idxDefinition == null)
                    {
                        throw new KeyNotFoundException($"Filter {indexId} not found");
                    }
                    idxDefinition.Status = newState;
                    idxDefinition.UpdatedTime = DateTimeOffset.Now;
                    idxDefinition.UpdatedByKey = context.EstablishProvenance(AuthenticationContext.Current.Principal, null);
                    idxDefinition = context.Update(idxDefinition);
                    return new AdoPreparedFilter(idxDefinition);
                }
                catch(Exception e)
                {
                    throw new DataPersistenceException($"Error setting state of {indexId}", e);
                }
            }
        }
    }
}
