/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a service which is responsible for the maintenance of concepts using local persistence.
    /// </summary>
    public class LocalConceptRepository : GenericLocalRepositoryEx<Concept>, IConceptRepositoryService
    {
        // OID regex
        private Regex m_oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
        private readonly IAdhocCacheService m_adhocCacheService;
        private readonly IDataPersistenceService<ConceptName> m_conceptNameService;
        private readonly IDataPersistenceService<ConceptReferenceTerm> m_referenceTermService;
        private readonly IDataPersistenceService<ConceptSet> m_conceptSetService;
        /// <summary>
        /// Privacy enforcement service
        /// </summary>
        public LocalConceptRepository(IPolicyEnforcementService policyService, 
            ILocalizationService localizationService, 
            IDataPersistenceService<Concept> persistenceService,
            IDataPersistenceService<ConceptReferenceTerm> referenceTermService,
            IDataPersistenceService<ConceptName> conceptNameService,
            IDataPersistenceService<ConceptSet> conceptSetPersistence,
            IPrivacyEnforcementService privacyService = null, 
            IAdhocCacheService adhocCacheService = null) : base(policyService, localizationService, persistenceService, privacyService)
        {
            this.m_adhocCacheService = adhocCacheService;
            this.m_conceptNameService = conceptNameService;
            this.m_referenceTermService = referenceTermService;
            this.m_conceptSetService = conceptSetPersistence;
        }

        /// <summary>
        /// Query policy for concepts
        /// </summary>
        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;

        /// <summary>
        /// Read policies for concepts
        /// </summary>
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;

        /// <summary>
        /// Write policy for concepts
        /// </summary>
        protected override string WritePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;

        /// <summary>
        /// Delete policy for concepts
        /// </summary>
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;

        /// <summary>
        /// Alater policy for concepts
        /// </summary>
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;

        /// <summary>
        /// Searches for a concept by name and language.
        /// </summary>
        /// <param name="name">The name of the concept.</param>
        /// <param name="language">The language of the concept.</param>
        /// <returns>Returns a list of concepts.</returns>
        public IEnumerable<Concept> FindConceptsByName(string name, string language)
        {
            return base.Find(o => o.ConceptNames.Any(n => n.Name == name && n.Language == language && n.ObsoleteVersionSequenceId == null));
        }

        /// <summary>
        /// Finds a concept by reference term.
        /// </summary>
        /// <param name="code">The code of the reference term.</param>
        /// <param name="codeSystem">The code system OID of the reference term.</param>
        /// <returns>Returns a list of concepts.</returns>
        public IEnumerable<ConceptReferenceTerm> FindConceptsByReferenceTerm(string code, Uri codeSystem)
        {
            return this.FindConceptsByReferenceTerm(code, codeSystem.OriginalString);
        }

        /// <summary>
        /// Find concepts by reference terms
        /// </summary>
        /// <remarks>This returns all reference terms regardless of their relationship type</remarks>
        public IEnumerable<ConceptReferenceTerm> FindConceptsByReferenceTerm(string code, string codeSystemDomain)
        {
            // Concept is loaded
            if (codeSystemDomain.StartsWith("urn:oid:"))
                codeSystemDomain = codeSystemDomain.Substring(8);

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            var cacheKey = $"{code}.{codeSystemDomain}";
            var retVal = this.m_adhocCacheService?.Get<IEnumerable<ConceptReferenceTerm>>(cacheKey);

            if (retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
                return retVal;
            if (codeSystemDomain.StartsWith("http:") || codeSystemDomain.StartsWith("urn:"))
                retVal = this.m_referenceTermService.Query(o => o.ReferenceTerm.CodeSystem.Url == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else if (oidRegex.IsMatch(codeSystemDomain))
                retVal = this.m_referenceTermService.Query(o => o.ReferenceTerm.CodeSystem.Oid == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else
                retVal = this.m_referenceTermService.Query(o => o.ReferenceTerm.CodeSystem.Authority == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);

            this.m_adhocCacheService?.Add(cacheKey, retVal);
            return retVal;
        }

        /// <summary>
        /// Finds a concept by reference term only where the concept is equivalent
        /// </summary>
        /// <param name="code">The code of the reference term.</param>
        /// <param name="codeSystemDomain">The code system OID of the reference term.</param>
        /// <returns>Returns a list of concepts.</returns>
        /// <remarks>This function requires that the reference term <paramref name="code"/> and <paramref name="codeSystemDomain"/> have relationship SAME_AS</remarks>
        public Concept GetConceptByReferenceTerm(string code, String codeSystemDomain)
        {
            return this.FindConceptsByReferenceTerm(code, codeSystemDomain).FirstOrDefault(o => o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs).LoadProperty<Concept>(nameof(ConceptReferenceTerm.SourceEntity));
        }

        /// <summary>
        /// Get a concept by its mnemonic
        /// </summary>
        /// <param name="mnemonic">The concept mnemonic to get.</param>
        /// <returns>Returns the concept.</returns>
        public Concept GetConcept(string mnemonic)
        {
            var cacheKey = $"concept.{mnemonic}";
            var retVal = this.m_adhocCacheService?.Get<Guid>(cacheKey);

            if (retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
                return this.Get(retVal.Value);
            else
            {
                var obj = base.Find(o => o.Mnemonic == mnemonic).FirstOrDefault();
                this.m_adhocCacheService?.Add(cacheKey, obj.Key.Value);
                return obj;
            }
        }

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        /// <param name="codeSystem">The code system to lookup the reference term</param>
        /// <param name="conceptId">The concept identifier to fetch reference term for</param>
        /// <param name="exact">True if an exact match should be returned</param>
        public ReferenceTerm GetConceptReferenceTerm(Guid conceptId, string codeSystem, bool exact = true)
        {
            var cacheKey = $"refTerm.{conceptId}.{codeSystem}.{exact}";
            var retVal = this.m_adhocCacheService?.Get<ReferenceTerm>(cacheKey);

            if (retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
                return retVal;

            // Filter expression
            Expression<Func<ConceptReferenceTerm, bool>> filterExpression = null;
            if (String.IsNullOrEmpty(codeSystem))
                filterExpression = o => o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null;
            else if (this.m_oidRegex.IsMatch(codeSystem))
                filterExpression = o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null;
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out Uri uri))
                filterExpression = o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null;
            else
                filterExpression = o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null;

            if (exact)
            {
                var exactExpression = Expression.MakeBinary(ExpressionType.Equal, Expression.MakeMemberAccess(filterExpression.Parameters[0], typeof(ConceptReferenceTerm).GetProperty(nameof(ConceptReferenceTerm.RelationshipTypeKey))), Expression.Convert(Expression.Constant(ConceptRelationshipTypeKeys.SameAs), typeof(Guid?)));
                filterExpression = Expression.Lambda<Func<ConceptReferenceTerm, bool>>(Expression.MakeBinary(ExpressionType.And, filterExpression.Body, exactExpression), filterExpression.Parameters);
            }

            var refTermEnt = this.m_referenceTermService.Query(filterExpression, AuthenticationContext.Current.Principal).FirstOrDefault();
            retVal = refTermEnt?.LoadProperty<ReferenceTerm>("ReferenceTerm");

            this.m_adhocCacheService?.Add(cacheKey, retVal);

            return retVal;
        }

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        public IEnumerable<ConceptReferenceTerm> FindReferenceTermsByConcept(Guid conceptId, string codeSystem)
        {

            var cacheKey = $"refTermAssoc.{conceptId}.{codeSystem}";
            var retVal = this.m_adhocCacheService?.Get<ConceptReferenceTerm[]>(cacheKey);

            if(retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
            {
                return retVal;
            }

            IEnumerable<ConceptReferenceTerm> refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if(String.IsNullOrEmpty(codeSystem)) // all
                refTermEnt = this.m_referenceTermService.Query(o => o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else if (oidRegex.IsMatch(codeSystem))
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);

            this.m_adhocCacheService?.Add(cacheKey, refTermEnt);
            return refTermEnt;
        }

        /// <summary>
        /// Get set members for the specified concept set
        /// </summary>
        public IEnumerable<Concept> GetConceptSetMembers(string mnemonic)
        {
            return this.Find(o => o.ConceptSets.Any(c => c.Mnemonic == mnemonic));
        }

        /// <summary>
        /// Returns a value which indicates whether <paramref name="a"/> implies <paramref name="b"/>
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public bool Implies(Concept a, Concept b)
        {
            throw new NotImplementedException(this.m_localizationService.GetString("error.type.NotImplementedException"));
        }

        /// <summary>
        /// Determine if the concept set contains the specified concept
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="concept">The concept.</param>
        /// <returns><c>true</c> if the specified set is member; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.InvalidOperationException">ConceptSet persistence service not found.</exception>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public bool IsMember(ConceptSet set, Concept concept) => this.IsMember(set.Key.GetValueOrDefault(), concept.Key.GetValueOrDefault());

        /// <summary>
		/// Determine if the concept set contains the specified concept
		/// </summary>
		/// <param name="set">The set.</param>
		/// <param name="concept">The concept.</param>
		/// <returns><c>true</c> if the specified set is member; otherwise, <c>false</c>.</returns>
		/// <exception cref="System.InvalidOperationException">ConceptSet persistence service not found.</exception>
		[PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public bool IsMember(Guid set, Guid concept)
        {
            return this.m_conceptSetService.Query(o => o.Key == set && o.Concepts.Any(c => c.Key == concept), AuthenticationContext.Current.Principal).Any();
        }

        /// <summary>
        /// Get the concept reference term in the code system by the concept mnemonic
        /// </summary>
        public ReferenceTerm GetConceptReferenceTerm(string conceptMnemonic, string codeSystem)
        {
            var cacheKey = $"refTerm.{conceptMnemonic}.{codeSystem}";
            var retVal = this.m_adhocCacheService?.Get<ReferenceTerm>(cacheKey);

            if (retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
            {
                return retVal;
            }

            ConceptReferenceTerm refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if(String.IsNullOrEmpty(codeSystem))
                refTermEnt = this.m_referenceTermService.Query(o => o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, AuthenticationContext.Current.Principal).FirstOrDefault();
            else if (oidRegex.IsMatch(codeSystem))
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, AuthenticationContext.Current.Principal).FirstOrDefault();
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, AuthenticationContext.Current.Principal).FirstOrDefault();
            else
                refTermEnt = this.m_referenceTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, AuthenticationContext.Current.Principal).FirstOrDefault();
            retVal = refTermEnt.LoadProperty<ReferenceTerm>("ReferenceTerm");

            this.m_adhocCacheService?.Add(cacheKey, retVal);

            return retVal;
        }

        /// <summary>
        /// Get the name of the concept
        /// </summary>
        /// <param name="conceptId">The concept to get</param>
        /// <param name="twoLetterISOLanguageName">The language code to get</param>
        /// <returns>The name</returns>
        public string GetName(Guid conceptId, string twoLetterISOLanguageName)
        {
            var cacheKey = $"concept.name.{conceptId}";
            var retVal = this.m_adhocCacheService?.Get<IEnumerable<ConceptName>>(cacheKey);

            if (retVal != null || this.m_adhocCacheService?.Exists(cacheKey) == true)
                return retVal.FirstOrDefault(o => o.Language == twoLetterISOLanguageName)?.Name;

            retVal = this.m_conceptNameService.Query(o => o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal) ;

            this.m_adhocCacheService?.Add(cacheKey, retVal);

            return retVal.FirstOrDefault(o=>o.Language == twoLetterISOLanguageName)?.Name;
        }
    }
}