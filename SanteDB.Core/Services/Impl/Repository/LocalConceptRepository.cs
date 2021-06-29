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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Privacy enforcement service
        /// </summary>
        public LocalConceptRepository(IPrivacyEnforcementService privacyService = null) : base(privacyService)
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
        public IEnumerable<ConceptReferenceTerm> FindConceptsByReferenceTerm(string code, string codeSystemDomain)
        {
            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException("Cannot find concept/reference term service");

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
                if(obj != null)
                    adhocCache?.Add($"concept.{mnemonic}", obj.Key.Value, new TimeSpan(1, 0, 0));
                return obj;
            }
        }

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        public ReferenceTerm GetConceptReferenceTerm(Guid conceptId, string codeSystem)
        {

            var adhocCache = ApplicationServiceContext.Current.GetService<IAdhocCacheService>();
            var retVal = adhocCache?.Get<ReferenceTerm>($"refTerm.{conceptId}.{codeSystem}");

            if (retVal != null)
                return retVal;
            
            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException("Cannot find concept/reference term service");

            int tr;
            ConceptReferenceTerm refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            retVal = refTermEnt.LoadProperty<ReferenceTerm>("ReferenceTerm");

            adhocCache?.Add($"refTerm.{conceptId}.{codeSystem}", retVal, new TimeSpan(0,0,30));

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
                throw new InvalidOperationException("Cannot find concept/reference term service");

            int tr;
            IEnumerable<ConceptReferenceTerm> refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal);
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal);
            else
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId && o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal);
            
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
            throw new NotImplementedException();
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
                throw new InvalidOperationException($"{nameof(IDataPersistenceService<ConceptSet>)} not found");
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
                throw new InvalidOperationException($"{nameof(IDataPersistenceService<ConceptSet>)} not found");
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
                throw new InvalidOperationException("Cannot find concept/reference term service");

            int tr;
            ConceptReferenceTerm refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntity.Mnemonic == conceptMnemonic&& o.ObsoleteVersionSequenceId == null && o.RelationshipTypeKey == ConceptRelationshipTypeKeys.SameAs, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
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