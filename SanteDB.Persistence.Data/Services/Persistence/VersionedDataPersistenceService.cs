using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.MappedResultSets;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Persistence service which handles versioned objects
    /// </summary>
    public abstract class VersionedDataPersistenceService<TModel, TDbModel, TDbKeyModel> : BaseEntityDataPersistenceService<TModel, TDbModel>
        where TModel : BaseEntityData, IVersionedData, IHasState, new()
        where TDbModel : DbVersionedData, IDbHasStatus, new()
        where TDbKeyModel : DbIdentified, new()
    {


        /// <summary>
        /// Perform a copy of the existing version inforamtion to a new version
        /// </summary>
        /// <param name="context">The context on which the records should be inserted</param>
        /// <param name="newVersion">The new version key to be copied to</param>
        protected abstract void DoCopyVersionSubTableInternal(DataContext context, TDbModel newVersion);

        /// <inheritdoc/>
        protected override bool ValidateCacheItem(TModel cacheEntry, TDbModel dataModel) => cacheEntry.VersionSequence >= dataModel.VersionSequenceId;

        /// <summary>
        /// This method creates a new version from the old
        /// </summary>
        protected override TModel DoTouchModel(DataContext context, Guid key)
        {

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif

                // We don't create new versions, instead we update the current data
                var existing = context.Query<TDbModel>(o => o.Key == key).OrderByDescending(o => o.VersionSequenceId).Skip(0).Take(2).ToArray();

                if (!existing.Any())
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = key }));
                }

                // Are we creating a new verison? or no?
                if (!this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    if (existing.Count() > 1) // We only keep recent and last
                    {
                        context.Delete<TDbModel>(o => o.Key == key && o.VersionSequenceId <= existing.Last().VersionSequenceId);
                    }
                }

                // We want to obsolete the non current version(s)
                foreach (var itm in context.Query<TDbModel>(o => o.Key == key && !o.ObsoletionTime.HasValue))
                {
                    itm.ObsoletionTime = DateTimeOffset.Now;
                    itm.ObsoletedByKey = context.ContextId;
                    itm.ObsoletedByKeySpecified = itm.ObsoletionTimeSpecified = true;
                    context.Update(itm);
                }

                // next - we create a new version of dbmodel
                var newVersion = existing.First();
                newVersion.ReplacesVersionKey = newVersion.VersionKey;
                newVersion.CreationTime = DateTimeOffset.Now;
                newVersion.CreatedByKey = context.ContextId;
                newVersion.ObsoletedByKey = null;
                newVersion.ObsoletionTime = null;
                newVersion.VersionSequenceId = null;
                newVersion.ObsoletedByKeySpecified = true;
                newVersion.VersionKey = Guid.NewGuid();

                context.Insert(newVersion);
                this.DoCopyVersionSubTableInternal(context, newVersion);

                return this.DoConvertToInformationModel(context, newVersion);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoUpdateModel - {sw.ElapsedMilliseconds}ms", key, new StackTrace());
            }
#endif

        }
        /// <summary>
        /// Generate the specified constructor
        /// </summary>
        public VersionedDataPersistenceService(IConfigurationManager configurationManager,
            ILocalizationService localizationService,
            IAdhocCacheService adhocCacheService = null,
            IDataCachingService dataCachingService = null,
            IQueryPersistenceService queryPersistence = null
            ) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Verify identities on the specified entity
        /// </summary>
        protected virtual IEnumerable<DetectedIssue> VerifyEntity<TToVerify>(DataContext context, TToVerify objectToVerify)
            where TToVerify : TModel, IHasIdentifiers
        {
            // Validate unique values for IDs
            var validation = this.m_configuration.Validation.Find(o => o.Target?.Type == objectToVerify.GetType()) ??
                this.m_configuration.Validation.Find(o => o.Target == null);
            if (validation == null) // no special validation
            {
                yield break;
            }

            foreach (var id in objectToVerify.Identifiers)
            {
                // Get ID
                DbAssigningAuthority dbAuth = null;

                if (id.AuthorityKey.HasValue)
                {
                    dbAuth = this.m_adhocCache?.Get<DbAssigningAuthority>($"{DataConstants.AdhocAuthorityKey}{id.AuthorityKey}");
                    if (dbAuth == null)
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.Key == id.AuthorityKey);
                }
                else if (id.Authority.Key.HasValue) // Attempt lookup in adhoc cache then by db
                {
                    dbAuth = this.m_adhocCache?.Get<DbAssigningAuthority>($"{DataConstants.AdhocAuthorityKey}{id.Authority.Key}");
                    if (dbAuth == null)
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.Key == id.Authority.Key);
                }
                else
                {
                    dbAuth = this.m_adhocCache?.Get<DbAssigningAuthority>($"{DataConstants.AdhocAuthorityKey}{id.Authority.DomainName}");
                    if (dbAuth == null)
                    {
                        dbAuth = context.FirstOrDefault<DbAssigningAuthority>(o => o.DomainName == id.Authority.DomainName);
                        if (dbAuth != null)
                            id.Authority.Key = dbAuth.Key;
                    }
                }

                if (dbAuth == null)
                {
                    if (!this.m_configuration.AutoInsertChildren || id.Authority == null) // we're not inserting it and it doesn't exist - raise the alarm!
                    {
                        yield return new DetectedIssue(DetectedIssuePriorityType.Error, DataConstants.IdentifierDomainNotFound, $"Missing assigning authority with ID {String.Join(",", objectToVerify.Identifiers.Select(o => o.Authority.Key))}", DetectedIssueKeys.SafetyConcernIssue);
                    }
                    continue;
                }
                else
                {
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityKey}{id.Authority.Key}", dbAuth, new TimeSpan(0, 5, 0));
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityKey}{dbAuth.DomainName}", dbAuth, new TimeSpan(0, 5, 0));
                }

                // Get this identifier records which is not owned by my record
                var ownedByOthers = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.Authority.Key && o.ObsoleteVersionSequenceId == null && o.SourceKey != objectToVerify.Key)
                    .And("NOT EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ? AND trg_ent_id = ent_id_tbl.ent_id OR trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL)", objectToVerify.Key, objectToVerify.Key)
                ).Any();
                var ownedByMe = context.Query<DbEntityIdentifier>(
                    context.CreateSqlStatement()
                    .SelectFrom(typeof(DbEntityIdentifier))
                    .Where<DbEntityIdentifier>(o => o.Value == id.Value && o.AuthorityKey == id.Authority.Key && o.ObsoleteVersionSequenceId == null)
                    .And("(ent_id = ? OR EXISTS (SELECT 1 FROM ent_rel_tbl WHERE (src_ent_id = ?  AND trg_ent_id = ent_id_tbl.ent_id) OR (trg_ent_id = ? AND src_ent_id = ent_id_tbl.ent_id) AND obslt_vrsn_seq_id IS NULL))", objectToVerify.Key, objectToVerify.Key, objectToVerify.Key)
                ).Any();

                // Verify scope
                IEnumerable<DbAuthorityScope> scopes = this.m_adhocCache?.Get<DbAuthorityScope[]>($"ado.aa.scp.{dbAuth.Key}");
                if (scopes == null)
                {
                    scopes = context.Query<DbAuthorityScope>(o => o.SourceKey == dbAuth.Key);
                    this.m_adhocCache?.Add($"{DataConstants.AdhocAuthorityScopeKey}{dbAuth.Key}", scopes.ToArray());
                }

                if (objectToVerify is IHasClassConcept classObject &&
                    scopes.Any() && !scopes.Any(s => s.ScopeConceptKey == classObject.ClassConceptKey) // This type of identifier is not allowed to be assigned to this type of object
                    && !ownedByOthers
                    && !ownedByMe) // Unless it was already associated to another type of object related to me
                    yield return new DetectedIssue(validation.Scope.ToPriority(), DataConstants.IdentifierInvalidTargetScope, $"Identifier of type {dbAuth.DomainName} cannot be assigned to object of type {classObject.ClassConceptKey}", DetectedIssueKeys.BusinessRuleViolationIssue);

                // If the identity domain is unique, and we've been asked to raid identifier uq issues
                if (dbAuth.IsUnique &&
                    ownedByOthers)
                {
                    yield return new DetectedIssue(validation.Uniqueness.ToPriority(), DataConstants.IdentifierNotUnique, $"Identifier {id.Value} in domain {dbAuth.DomainName} violates unique constraint", DetectedIssueKeys.FormalConstraintIssue);
                }

                if (dbAuth.AssigningApplicationKey.HasValue) // Must have permission
                {
                    if (context.GetProvenance().ApplicationKey == dbAuth.AssigningApplicationKey)
                    {
                        id.Reliability = IdentifierReliability.Authoritative;
                    }
                    else if (objectToVerify.CreatedByKey != dbAuth.AssigningApplicationKey  // original prov key
                        && !ownedByMe
                        && !ownedByOthers) // and has not already been assigned to me or anyone else (it is a new , unknown identifier)
                    {
                        id.Reliability = IdentifierReliability.Informative;
                        // Is the validation set to deny unauthorized assignment?
                        yield return new DetectedIssue(validation.Authority.ToPriority(), DataConstants.IdentifierNoAuthorityToAssign, $"Application does not have permission to assign {dbAuth.DomainName}", DetectedIssueKeys.SecurityIssue);
                    }
                }

                if (!String.IsNullOrEmpty(dbAuth.ValidationRegex) && validation.Format != Configuration.AdoValidationEnforcement.Off) // must be valid
                {
                    var nonMatch = !new Regex(dbAuth.ValidationRegex).IsMatch(id.Value);
                    if (nonMatch)
                        yield return new DetectedIssue(validation.Format.ToPriority(), DataConstants.IdentifierPatternFormatFail, $"Identifier {id.Value} in domain {dbAuth.DomainName} failed format validation", DetectedIssueKeys.FormalConstraintIssue);
                }

                if (validation.CheckDigit != Configuration.AdoValidationEnforcement.Off && !String.IsNullOrEmpty(dbAuth.CustomValidator))
                {
                    var type = Type.GetType(dbAuth.CustomValidator);
                    if (type == null)
                    {
                        yield return new DetectedIssue(validation.CheckDigit.ToPriority(), DataConstants.IdentifierCheckProviderNotFound, $"Custom validator {dbAuth.CustomValidator} not found", DetectedIssueKeys.OtherIssue);
                    }
                    var validator = Activator.CreateInstance(type) as IIdentifierValidator;
                    if (validator?.IsValid(id) != true)
                    {
                        yield return new DetectedIssue(validation.CheckDigit.ToPriority(), DataConstants.IdentifierCheckDigitFailed, $"Custom validator for {id.Value} in {dbAuth.DomainName} failed", DetectedIssueKeys.FormalConstraintIssue);
                    }
                }
            }
        }

        /// <summary>
        /// Perform a GET internal
        /// </summary>
        protected override TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            TDbModel retVal = default(TDbModel);

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                if (versionKey.GetValueOrDefault() == Guid.Empty) // fetching the current version
                {
                    var cacheKey = this.GetAdHocCacheKey(key);
                    if (allowCache && (this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                    {
                        retVal = this.m_adhocCache?.Get<TDbModel>(cacheKey);
                    }

                    // Cache miss
                    if (retVal == null)
                    {
                        retVal = context.Query<TDbModel>(o => o.Key == key).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault();

                        if ((this.m_configuration?.CachingPolicy?.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                        {
                            this.m_adhocCache.Add<TDbModel>(cacheKey, retVal, this.m_configuration.CachingPolicy?.DataObjectExpiry);
                        }
                    }
                }
                else
                {
                    // Fetch the object
                    retVal = context.FirstOrDefault<TDbModel>(o => o.Key == key && o.VersionKey == versionKey);
                }

#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Get {0}/v.{1} took {2} ms", key, versionKey, sw.ElapsedMilliseconds);
            }
#endif
            return retVal;
        }

        /// <summary>
        /// Perform an insert on the specified object
        /// </summary>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if (dbModel == null)
            {
                throw new ArgumentNullException(nameof(dbModel), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                // First we want to insert the keyed object via TDbKeyTable
                if (dbModel.Key == Guid.Empty)
                {
                    dbModel.Key = Guid.NewGuid();
                }
                var dbKeyTable = context.Insert(new TDbKeyModel() { Key = dbModel.Key });

                // Next we want to insert the version key
                if (dbModel.VersionKey == Guid.Empty)
                {
                    dbModel.VersionKey = Guid.NewGuid();
                }
                dbModel.CreationTime = DateTimeOffset.Now;
                dbModel.CreatedByKey = context.ContextId;

                // Insert the version link data
                return context.Insert(dbModel);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Insert {0} took {1}ms", dbModel, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform a versioned update
        /// </summary>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (model == null)
            {
                throw new ArgumentNullException(nameof(model), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // We don't create new versions, instead we update the current data
                var existing = context.Query<TDbModel>(o => o.Key == model.Key).OrderByDescending(o => o.VersionSequenceId).Skip(0).Take(2).ToArray();

                if (!existing.Any())
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = model.Key }));
                }

                // Are we creating a new verison? or no?
                if (!this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    if (existing.Count() > 1) // We only keep recent and last
                    {
                        context.Delete<TDbModel>(o => o.Key == model.Key && o.VersionSequenceId <= existing.Last().VersionSequenceId);
                    }
                }

                // We want to obsolete the non current version(s)
                foreach (var itm in context.Query<TDbModel>(o => o.Key == model.Key && !o.ObsoletionTime.HasValue))
                {
                    itm.ObsoletionTime = DateTimeOffset.Now;
                    itm.ObsoletedByKey = context.ContextId;
                    itm.ObsoletedByKeySpecified = itm.ObsoletionTimeSpecified = true;
                    context.Update(itm);
                }

                // next - we create a new version of dbmodel
                var oldVersion = existing.First();
                var newVersion = new TDbModel();
                newVersion.CopyObjectData(model, true);
                newVersion.ReplacesVersionKey = oldVersion.VersionKey;
                newVersion.CreationTime = DateTimeOffset.Now;
                newVersion.CreatedByKey = context.ContextId;
                newVersion.ObsoletedByKey = null;
                newVersion.ObsoletionTime = null;
                newVersion.VersionSequenceId = null;

                newVersion.ObsoletedByKeySpecified = model.ObsoletionTimeSpecified = true;
                newVersion.VersionKey = Guid.NewGuid();

                return context.Insert(newVersion); // Insert the core version
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Update of {0} took {1} ms", model, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Obsolete all objects
        /// </summary>
        protected override void DoDeleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression, DeleteMode deletionMode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (expression == null)
            {
                throw new ArgumentException(nameof(expression), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_RANGE));
            }

#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                // First - we determine if the query has an explicit status concept set
                if (typeof(IHasState).IsAssignableFrom(typeof(TModel)) && !expression.ToString().Contains(nameof(IHasState.StatusConceptKey)))
                {
                    var statusKeyProperty = Expression.MakeMemberAccess(expression.Parameters[0], typeof(TModel).GetProperty(nameof(IHasState.StatusConceptKey)));
                    statusKeyProperty = Expression.MakeMemberAccess(statusKeyProperty, statusKeyProperty.Type.GetProperty("Value"));
                    expression = Expression.Lambda<Func<TModel, bool>>(Expression.And(expression.Body, Expression.MakeBinary(ExpressionType.NotEqual, statusKeyProperty, Expression.Constant(StatusKeys.Obsolete))), expression.Parameters);
                }

                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    // Convert the query to a domain query so that the object persistence layer can turn the
                    // structured LINQ query into a SQL statement
                    var domainExpression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(expression, false);
                    if (domainExpression == null)
                    {
                        this.m_tracer.TraceWarning("WARNING: Using very slow DeleteAll() method - consider using only primary properties for delete all");
                        var columnKey = TableMapping.Get(typeof(TDbModel)).GetColumn(nameof(DbVersionedData.Key));
                        var keyQuery = context.GetQueryBuilder(this.m_modelMapper).CreateQuery(expression, columnKey);
                        var keys = context.Query<TDbModel>(keyQuery).Select(o => o.Key);
                        domainExpression = o => keys.Contains(o.Key);
                    }

                    // Add obsolete filter - only apply this to current versions
                    var obsoletionReference = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(domainExpression.Parameters[0], typeof(TDbModel).GetProperty(nameof(DbVersionedData.ObsoletionTime))), Expression.Constant(null));
                    domainExpression = Expression.Lambda<Func<TDbModel, bool>>(Expression.MakeBinary(ExpressionType.AndAlso, obsoletionReference, domainExpression.Body), domainExpression.Parameters);

                    // determine our deletion mode
                    switch (deletionMode)
                    {
                        case DeleteMode.NullifyDelete:
                        case DeleteMode.ObsoleteDelete:
                        case DeleteMode.LogicalDelete:

                            foreach (var newVersion in context.InsertAll(
                                context.UpdateAll<TDbModel>(context.Query<TDbModel>(domainExpression), o => // Update the current version
                                {
                                    o.ObsoletionTime = DateTimeOffset.Now;
                                    o.ObsoletedByKey = context.ContextId;
                                    return o;
                                }).Select(o => // Insert a new version
                                {
                                    o.VersionSequenceId = null;
                                    o.ReplacesVersionKey = o.VersionKey;
                                    o.ObsoletedByKey = null;
                                    o.ObsoletionTime = null;
                                    o.VersionKey = Guid.NewGuid();
                                    o.StatusConceptKey = deletionMode == DeleteMode.NullifyDelete ? StatusKeys.Nullified :
                                        deletionMode == DeleteMode.ObsoleteDelete ? StatusKeys.Obsolete : StatusKeys.Inactive;
                                    this.m_dataCacheService?.Remove(o.Key);
                                    return o;
                                })
                            ))
                            {
                                this.DoCopyVersionSubTableInternal(context, newVersion);
                            }

                            break;

                        case DeleteMode.PermanentDelete:
                            foreach (var existing in context.Query<TDbModel>(domainExpression))
                            {
                                existing.StatusConceptKey = StatusKeys.Purged;
                                this.DoDeleteReferencesInternal(context, existing.Key);
                                this.DoDeleteReferencesInternal(context, existing.VersionKey);
                                context.Delete<TDbModel>(o => o.VersionKey == existing.VersionKey);

                                // Reverse the history
                                foreach (var ver in context.Query<TDbModel>(o => o.Key == existing.Key).OrderByDescending(o => o.VersionSequenceId).Select(o => o.VersionKey))
                                {
                                    this.DoDeleteReferencesInternal(context, ver);
                                    context.Delete<TDbModel>(o => o.VersionKey == ver);

                                }

                                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.KeepPurged))
                                {
                                    existing.VersionKey = Guid.Empty;
                                    context.Insert(existing);
                                }
                                else
                                {
                                    context.Delete<TDbKeyModel>(o => o.Key == existing.Key);
                                }

                                this.m_dataCacheService.Remove(existing.Key);

                            }
                            break;
                    }
                }
                else
                {
                    base.DoDeleteAllInternal(context, expression, DeleteMode.PermanentDelete);
                }
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsolete all {0} took {1}ms", expression, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform an obsoletion of the object in the datamodel
        /// </summary>
        /// <param name="context">The context in which the obsoletion is occurring</param>
        /// <param name="key">The key of the object which is to be obsoleted</param>
        /// <param name="deletionMode">The mode of deletion</param>
        protected override TDbModel DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            else if (key == Guid.Empty)
            {
                throw new ArgumentException(this.m_localizationService.GetString(ErrorMessageStrings.MISSING_ARGUMENT, nameof(key)));
            }

            // Perform the obsoletion
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif

                // How are we obsoleting this? - is it full versioning?
                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
                {
                    // Get the current version
                    var existing = context.Query<TDbModel>(o => o.Key == key).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault();
                    if (existing == null)
                    {
                        throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = typeof(TModel).Name, id = key }));
                    }

                    TDbModel retVal = null;
                    switch (deletionMode)
                    {
                        case DeleteMode.ObsoleteDelete:
                            existing.StatusConceptKey = StatusKeys.Obsolete;
                            retVal = this.DoUpdateInternal(context, existing);
                            break;
                        case DeleteMode.NullifyDelete:
                            existing.StatusConceptKey = StatusKeys.Nullified;
                            retVal = this.DoUpdateInternal(context, existing);
                            break;
                        case DeleteMode.LogicalDelete:
                            existing.StatusConceptKey = StatusKeys.Inactive;
                            retVal = this.DoUpdateInternal(context, existing);
                            break;
                        case DeleteMode.VersionedDelete:
                            existing.StatusConceptKey = StatusKeys.Purged;
                            retVal = this.DoUpdateInternal(context, existing);
                            break;
                        case DeleteMode.PermanentDelete:
                            existing.StatusConceptKey = StatusKeys.Purged;
                            this.DoDeleteReferencesInternal(context, existing.Key);
                            this.DoDeleteReferencesInternal(context, existing.VersionKey);
                            context.Delete<TDbModel>(o => o.VersionKey == existing.VersionKey);
                            // Reverse the history
                            foreach (var ver in context.Query<TDbModel>(o => o.Key == existing.Key).OrderByDescending(o => o.VersionSequenceId).Select(o => o.VersionKey))
                            {
                                this.DoDeleteReferencesInternal(context, ver);
                                context.Delete<TDbModel>(o => o.VersionKey == ver);
                            }

                            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.KeepPurged))
                            {
                                existing.VersionKey = Guid.Empty;
                                context.Insert(existing);
                            }
                            else
                            {
                                context.Delete<TDbKeyModel>(o => o.Key == existing.Key);
                            }
                            return existing;

                        default:
                            throw new InvalidOperationException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_DELETE_MODE_SUPPORT, new { mode = deletionMode }));
                    }


                    // Copy a new version of dependent tables
                    this.DoCopyVersionSubTableInternal(context, retVal);

                    return retVal;

                }
                else
                {
                    return base.DoDeleteInternal(context, key, deletionMode);
                }
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsoletion of {0} took {1} ms", key, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform a query internal as another
        /// </summary>
        protected override OrmResultSet<TReturn> DoQueryInternalAs<TReturn>(DataContext context, Expression<Func<TModel, bool>> query, Func<SqlStatement, SqlStatement> queryModifier = null)
        {
            // First - we determine if the query has an explicit status concept set
            if (typeof(IHasState).IsAssignableFrom(typeof(TModel)) && !query.ToString().Contains(nameof(IHasState.StatusConceptKey)))
            {
                var statusKeyProperty = Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IHasState.StatusConceptKey)));
                statusKeyProperty = Expression.MakeMemberAccess(statusKeyProperty, statusKeyProperty.Type.GetProperty("Value"));

                Expression obsoleteFilter = null;
                foreach (var itm in StatusKeys.InactiveStates)
                {
                    var condition = Expression.MakeBinary(ExpressionType.NotEqual, statusKeyProperty, Expression.Constant(itm));
                    if (obsoleteFilter == null)
                    {
                        obsoleteFilter = condition;
                    }
                    else
                    {
                        obsoleteFilter = Expression.AndAlso(condition, obsoleteFilter);
                    }
                }
                query = Expression.Lambda<Func<TModel, bool>>(Expression.And(query.Body, obsoleteFilter), query.Parameters);
            }

            return base.DoQueryInternalAs<TReturn>(context, query, queryModifier);
        }

        /// <inheritdoc/>
        protected override IQueryResultSet<TModel> DoQueryModel(Expression<Func<TModel, bool>> query) => new MappedQueryResultSet<TModel>(this, nameof(IDbVersionedData.Key)).Where(query);

        /// <summary>
        /// Update associated entities
        /// </summary>
        /// <remarks>
        /// Updates the associated items of <typeparamref name="TModelAssociation"/> such that
        /// <paramref name="data"/>'s associations are updated to match the list
        /// provided in <paramref name="associations"/>
        /// </remarks>
        protected virtual IEnumerable<TModelAssociation> UpdateModelVersionedAssociations<TModelAssociation>(DataContext context, TModel data, IEnumerable<TModelAssociation> associations)
            where TModelAssociation : IdentifiedData, IVersionedAssociation, new()
        {
            if (data == null || data.Key.GetValueOrDefault() == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(IdentifiedData.Key), ErrorMessages.ARGUMENT_NULL);
            }
            // Ensure either the relationship points to (key) (either source or target)
            associations = associations.Select(a =>
            {
                if (a is ITargetedAssociation target && target.Key != data.Key && a.SourceEntityKey != data.Key ||
                    a.SourceEntityKey.GetValueOrDefault() == Guid.Empty) // The target is a target association
                {
                    a.SourceEntityKey = data.Key;
                }

                return a;
            }).ToArray();

            // We now want to fetch the perssitence serivce of this
            var persistenceService = typeof(TModelAssociation).GetRelatedPersistenceService() as IAdoPersistenceProvider<TModelAssociation>;
            if (persistenceService == null)
            {
                throw new DataPersistenceException(String.Format(ErrorMessages.RELATED_OBJECT_NOT_AVAILABLE, typeof(TModelAssociation), typeof(TModel)));
            }

            // Next we want to perform a relationship query to establish what is being loaded and what is being persisted
            // TODO: Determine if this line performs better than selecting the entire object (I suspect it would - but need to check)

            var existing = persistenceService.Query(context, o => o.SourceEntityKey == data.Key && !o.ObsoleteVersionSequenceId.HasValue).Select(o => o.Key).ToArray();

            // Which are new and which are not?
            var removedRelationships = existing.Where(o => associations.Any(a=>a.Key == o && a.BatchOperation == BatchOperationType.Delete) || !associations.Any(a => a.Key == o)).Select(a => persistenceService.Delete(context, a.Value, DeleteMode.LogicalDelete));
            var addedRelationships = associations.Where(o => o.BatchOperation != BatchOperationType.Delete && ( !o.Key.HasValue || !existing.Any(a => a == o.Key))).Select(a =>
            {
                a.EffectiveVersionSequenceId = data.VersionSequence;
                a = persistenceService.Insert(context, a);
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Insert;
                return a;
            });
            var updatedRelationships = associations.Where(o => o.BatchOperation != BatchOperationType.Delete && o.Key.HasValue && existing.Any(a => a == o.Key)).Select(a =>
            {
                a = persistenceService.Update(context, a);
                a.BatchOperation = Core.Model.DataTypes.BatchOperationType.Update;
                return a;
            });

            return addedRelationships.Union(updatedRelationships).Except(removedRelationships).ToArray();
        }

        /// <summary>
        /// Update the internal
        /// </summary>
        protected virtual IEnumerable<TAssociativeTable> UpdateInternalVersoinedAssociations<TAssociativeTable>(DataContext context, Guid sourceKey, long versionSequence, IEnumerable<TAssociativeTable> associations)
            where TAssociativeTable : IDbVersionedAssociation, new()
        {
            if (sourceKey == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(sourceKey), ErrorMessages.ARGUMENT_NULL);
            }

            // Ensure the source by locking the IEnumerable
            associations = associations.Select(a =>
            {
                if (a.SourceKey == Guid.Empty)
                {
                    a.SourceKey = sourceKey;
                }
                return a;
            }).ToArray();

            // Existing associations in the database
            var existing = context.Query<TAssociativeTable>(o => o.SourceKey == sourceKey && !o.ObsoleteVersionSequenceId.HasValue).ToArray();

            // Which ones are new?
            var removeRelationships = existing.Where(e => !associations.Any(a => a.Equals(e)));
            var addRelationships = associations.Where(a => !existing.Any(e => e.Equals(a)));

            // First, remove the old
            foreach (var itm in removeRelationships)
            {
                this.m_tracer.TraceVerbose("Will remove {0} of {1}", typeof(TAssociativeTable).Name, itm);

                if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
                {
                    itm.ObsoleteVersionSequenceId = versionSequence;
                    context.Update(itm);
                }
                else
                {
                    context.Delete(itm);
                }
            }

            // Next, add the new
            foreach (var itm in addRelationships)
            {
                this.m_tracer.TraceVerbose("Will add {0} of {1}", typeof(TAssociativeTable).Name, itm);
                itm.EffectiveVersionSequenceId = versionSequence;
                context.Insert(itm);
            }

            return existing.Where(o => !removeRelationships.Any(r => r.Equals(o))).Union(addRelationships);
        }
    }
}