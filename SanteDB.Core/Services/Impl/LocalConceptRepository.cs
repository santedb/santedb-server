/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a service which is responsible for the
    /// maintenance of concepts.
    /// </summary>
    public class LocalConceptRepository : GenericLocalNullifiedRepository<Concept>, IConceptRepositoryService
	{
        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string WritePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AdministerConceptDictionary;

        /// <summary>
        /// Searches for a concept by name and language.
        /// </summary>
        /// <param name="name">The name of the concept.</param>
        /// <param name="language">The language of the concept.</param>
        /// <returns>Returns a list of concepts.</returns>
        public IEnumerable<Concept> FindConceptsByName(string name, string language)
		{
			return base.Find(o => o.ConceptNames.Any(n => n.Name == name && n.Language == language));
		}

		/// <summary>
		/// Finds a concept by reference term.
		/// </summary>
		/// <param name="code">The code of the reference term.</param>
		/// <param name="codeSystem">The code system OID of the reference term.</param>
		/// <returns>Returns a list of concepts.</returns>
        public IEnumerable<Concept> FindConceptsByReferenceTerm(string code, Uri codeSystem)
		{
			if ((codeSystem.Scheme == "urn" || codeSystem.Scheme == "oid"))
			{
				var csOid = codeSystem.LocalPath;
                if (csOid.StartsWith("oid"))
                {
                    csOid = codeSystem.LocalPath.Substring(4);
                    return base.Find(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.CodeSystem.Oid == csOid && r.ReferenceTerm.Mnemonic == code));
                }
			}

			return this.Find(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.CodeSystem.Url == codeSystem.OriginalString && r.ReferenceTerm.Mnemonic == code));
		}

		/// <summary>
		/// Find concepts by reference terms
		/// </summary>
        public IEnumerable<Concept> FindConceptsByReferenceTerm(string code, string codeSystemDomain)
		{
            if (codeSystemDomain.StartsWith("urn:oid:"))
                codeSystemDomain = codeSystemDomain.Substring(8);
            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            if(codeSystemDomain.StartsWith("http:") || codeSystemDomain.StartsWith("urn:"))
                return base.Find(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.CodeSystem.Url == codeSystemDomain && r.ReferenceTerm.Mnemonic == code));
            else if(oidRegex.IsMatch(codeSystemDomain))
                return base.Find(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.CodeSystem.Oid == codeSystemDomain && r.ReferenceTerm.Mnemonic == code));
            else
                return base.Find(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.CodeSystem.Authority == codeSystemDomain && r.ReferenceTerm.Mnemonic == code));
		}
        
        /// <summary>
        /// Get a concept by its mnemonic
        /// </summary>
        /// <param name="mnemonic">The concept mnemonic to get.</param>
        /// <returns>Returns the concept.</returns>
		public Concept GetConcept(string mnemonic)
		{
			return base.Find(o => o.Mnemonic == mnemonic).FirstOrDefault();
		}

        /// <summary>
        /// Get the specified reference term for the specified code system
        /// </summary>
        public ReferenceTerm GetConceptReferenceTerm(Guid conceptId, string codeSystem)
        {
            // Concept is loaded
            var refTermService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ConceptReferenceTerm>>();

            if (refTermService == null)
                throw new InvalidOperationException("Cannot find concept/reference term service");

            int tr;
            ConceptReferenceTerm refTermEnt = null;

            Regex oidRegex = new Regex("^(\\d+?\\.){1,}\\d+$");
            Uri uri = null;
            if (oidRegex.IsMatch(codeSystem))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Oid == codeSystem) && o.SourceEntityKey == conceptId, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else if (Uri.TryCreate(codeSystem, UriKind.Absolute, out uri))
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Url == codeSystem) && o.SourceEntityKey == conceptId, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            else
                refTermEnt = refTermService.Query(o => (o.ReferenceTerm.CodeSystem.Authority == codeSystem) && o.SourceEntityKey == conceptId, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
            return refTermEnt.LoadProperty<ReferenceTerm>("ReferenceTerm");
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

			return persistence.Count(o => o.Key == set.Key &&  o.ConceptsXml.Any(c => c == concept.Key)) > 0;
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

            return persistence.Count(o => o.Key == set &&  o.ConceptsXml.Any(c => c == concept)) > 0;
        }


    }
}