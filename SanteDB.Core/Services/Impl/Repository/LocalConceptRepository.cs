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
    /// Represents a service which is responsible for the
    /// maintenance of concepts.
    /// </summary>
    public class LocalConceptRepository : GenericLocalRepositoryEx<Concept>, IConceptRepositoryService
    {
        // OID regex
        private Regex m_oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");

        /// <summary>
        /// Privacy enforcement service
        /// </summary>
        public LocalConceptRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService,
            IPrivacyEnforcementService privacyService = null) : base(policyService, localizationService, privacyService)
        {
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
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.conceptTerm"));

            if (codeSystemDomain.StartsWith("urn:oid:"))
                codeSystemDomain = codeSystemDomain.Substring(8);

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
            var retVal = adhocCache?.Get<IEnumerable<ConceptReferenceTerm>>($"{code}.{codeSystemDomain}");

            if (retVal != null)
                return retVal;
            if (codeSystemDomain.StartsWith("http:") || codeSystemDomain.StartsWith("urn:"))
                retVal = refTermService.Query(o => o.ReferenceTerm.CodeSystem.Url == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else if (oidRegex.IsMatch(codeSystemDomain))
                retVal = refTermService.Query(o => o.ReferenceTerm.CodeSystem.Oid == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);
            else
                retVal = refTermService.Query(o => o.ReferenceTerm.CodeSystem.Authority == codeSystemDomain && o.ReferenceTerm.Mnemonic == code && o.ObsoleteVersionSequenceId == null, AuthenticationContext.Current.Principal);

            adhocCache?.Add($"{code}.{codeSystemDomain}", retVal, new TimeSpan(0, 0, 30));
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
            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
            var retVal = adhocCache?.Get<Guid>($"concept.{mnemonic}");

            if (retVal != null)
                return this.Get(retVal.Value);
            else
            {
                var obj = base.Find(o => o.Mnemonic == mnemonic).FirstOrDefault();
                if (obj != null)
                    adhocCache?.Add($"concept.{mnemonic}", obj.Key.Value, new TimeSpan(1, 0, 0));
                return obj;
            }
        }

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        /// <param name="codeSystem">The code system to lookup the reference term</param>
        /// <param name="conceptId">The concept identifier to fetch reference term for</param>
        /// <param name="relationshipType">The type of relationship (default SAME AS)</param>
        public ReferenceTerm GetConceptReferenceTerm(Guid conceptId, string codeSystem, bool exact = true)
        {
            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
            var retVal = adhocCache?.Get<ReferenceTerm>($"refTerm.{conceptId}.{codeSystem}");

            if (retVal != null)
                return retVal;

            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.conceptTerm"));

            // Filter expression
            Expression<Func<ConceptReferenceTerm, bool>> filterExpression = null;
            if (this.m_oidRegex.IsMatch(codeSystem))
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

            var refTermEnt = refTermService.Query(filterExpression, 0, 1, out int _, AuthenticationContext.Current.Principal).FirstOrDefault();
            retVal = refTermEnt?.LoadProperty<ReferenceTerm>("ReferenceTerm");

            adhocCache?.Add($"refTerm.{conceptId}.{codeSystem}", retVal, new TimeSpan(0, 0, 30));

            return retVal;
        }

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        public IEnumerable<ConceptReferenceTerm> FindReferenceTermsByConcept(Guid conceptId, string codeSystem)
        {
            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.conceptTerm"));

            int tr;
            IEnumerable<ConceptReferenceTerm> refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, 0, 100, out tr, AuthenticationContext.Current.Principal);
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, 0, 100, out tr, AuthenticationContext.Current.Principal);
            else
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null, 0, 100, out tr, AuthenticationContext.Current.Principal);

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
        public bool IsMember(ConceptSet set, Concept concept)
        {
            var persistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptSet>>();

            if (persistence == null)
            {
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = nameof(IDataPersistenceService<ConceptSet>)
                    }));
            }

            return persistence.Count(o => o.Key == set.Key && o.ConceptsXml.Any(c => c == concept.Key)) > 0;
        }

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
            var persistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptSet>>();

            if (persistence == null)
            {
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.servicePersistence", new
                    {
                        param = nameof(IDataPersistenceService<ConceptSet>)
                    }));
            }

            return persistence.Count(o => o.Key == set && o.ConceptsXml.Any(c => c == concept)) > 0;
        }

        /// <summary>
        /// Get the concept reference term in the code system by the concept mnemonic
        /// </summary>
        public ReferenceTerm GetConceptReferenceTerm(string conceptMnemonic, string codeSystem)
        {
            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
            var retVal = adhocCache?.Get<ReferenceTerm>($"refTerm.{conceptMnemonic}.{codeSystem}");

            if (retVal != null)
                return retVal;

            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.conceptTerm"));

            int tr;
            ConceptReferenceTerm refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            retVal = refTermEnt.LoadProperty<ReferenceTerm>("ReferenceTerm");

            adhocCache?.Add($"refTerm.{conceptMnemonic}.{codeSystem}", retVal, new TimeSpan(0, 0, 30));

            return retVal;
        }
    }
}