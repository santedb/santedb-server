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
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Persistence test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class PersonPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// test inserting a person with basic information
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var occupationKey = Guid.Parse("f76f5352-487c-11eb-b378-0242ac130002");
                var person = new Person()
                {
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street", "Hamilton", "ON", "CA", "H0H0H0")
                    },
                    DateOfBirth = new DateTime(1990, 01, 01),
                    DateOfBirthPrecision = Core.Model.DataTypes.DatePrecision.Day,
                    GenderConceptKey = NullReasonKeys.NoInformation,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new Core.Model.DataTypes.EntityIdentifier(IdentityDomainKeys.Gs1GlobalTradeIdentificationNumber, "123")
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Kris", "Kringle")
                    },
                    OccupationKey = occupationKey, // occupation = finance,
                    Template = new Core.Model.DataTypes.TemplateDefinition()
                    {
                        Mnemonic = "org.santedb.template.sample.santa",
                        Name = "Sample Santa Template",
                        Description = "A description of the santa template",
                        Oid = "2.25.404040404"
                    }
                };

                var afterInsert = base.TestInsert(person);
                Assert.IsInstanceOf<Person>(afterInsert);
                Assert.AreEqual(occupationKey, afterInsert.OccupationKey);

                // Test query
                var dtb = new DateTime(1990, 01, 01);
                base.TestQuery<Person>(o => o.DateOfBirth == dtb, 1);
                base.TestQuery<Person>(o => o.OccupationKey == occupationKey, 1);
                base.TestQuery<Person>(o => o.GenderConceptKey == NullReasonKeys.NoInformation, 1);
                base.TestQuery<Person>(o => o.OccupationKey == occupationKey && o.Template.Mnemonic == "org.santedb.template.sample.santa", 1);
                var afterQuery = base.TestQuery<Person>(o => o.Occupation.Mnemonic == "OccupationType-BusinessFinance" && o.GenderConceptKey == NullReasonKeys.NoInformation, 1).AsResultSet().First();

                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsNotNull(afterQuery.OccupationKey);
                Assert.AreEqual("OccupationType-BusinessFinance", afterQuery.LoadProperty(o => o.Occupation).Mnemonic);
                Assert.AreEqual("NullFlavor-NoInformation", afterQuery.LoadProperty(o => o.GenderConcept).Mnemonic);
                Assert.AreEqual("org.santedb.template.sample.santa", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.AreEqual(dtb, afterQuery.DateOfBirth);
                Assert.AreEqual(DatePrecision.Day, afterQuery.DateOfBirthPrecision);

                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.OccupationKey = NullReasonKeys.NotApplicable; // Change occupation
                    o.GenderConceptKey = null; // clear gender
                    o.GenderConcept = null;
                    return o;
                });
                Assert.IsNull(afterUpdate.GenderConceptKey);

                // Should not return on old occ code
                base.TestQuery<Person>(o => o.OccupationKey == occupationKey, 0);
                // Should not return on old gender code
                base.TestQuery<Person>(o => o.GenderConceptKey == NullReasonKeys.NoInformation, 0);
                base.TestQuery<Person>(o => o.OccupationKey == occupationKey && o.Template.Mnemonic == "org.santedb.template.sample.santa", 0);

                // Should return on new occ code
                afterQuery = base.TestQuery<Person>(o => o.OccupationKey == NullReasonKeys.NotApplicable, 1).AsResultSet().First();
                Assert.IsNull(afterQuery.GenderConceptKey);
                Assert.AreEqual(NullReasonKeys.NotApplicable, afterQuery.OccupationKey);
                Assert.AreEqual("NullFlavor-NotApplicable", afterQuery.LoadProperty(o => o.Occupation).Mnemonic);

            }
        }

        /// <summary>
        /// Test insertion of a person with language comms
        /// </summary>
        [Test]
        public void TestInsertWithLanguage()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var person = new Person()
                {
                    DateOfBirth = new DateTime(1991, 01, 01),
                    DateOfBirthPrecision = Core.Model.DataTypes.DatePrecision.Day,
                    GenderConceptKey = NullReasonKeys.NotAsked,
                    LanguageCommunication = new List<PersonLanguageCommunication>()
                    {
                        new PersonLanguageCommunication("en", true)
                    }
                };

                var dtb = new DateTime(1991, 01, 01);
                var afterInsert = base.TestInsert(person);
                Assert.IsInstanceOf<Person>(afterInsert);

                // Query on dob and lang
                var afterQuery = base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "en" && l.IsPreferred == true), 1).AsResultSet().First();
                Assert.AreEqual(NullReasonKeys.NotAsked, afterQuery.GenderConceptKey);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);

                // Add language
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LanguageCommunication.Add(new PersonLanguageCommunication("fr", false));
                    return o;
                });

                base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "fr" && l.IsPreferred == true), 0);
                afterQuery = base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "fr" && l.IsPreferred == false), 1).AsResultSet().First();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);

                // Remove a language
                afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LanguageCommunication.RemoveAll(l => l.IsPreferred);
                    o.LanguageCommunication.First().IsPreferred = true;
                    return o;
                });
                // Should not return an old lang
                base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "en" && l.IsPreferred == true), 0);
                // old FR which is not preferred should be removed
                base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "fr" && l.IsPreferred == false), 0);
                afterQuery = base.TestQuery<Person>(o => o.LanguageCommunication.Any(l => l.LanguageCode == "fr" && l.IsPreferred == true), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);
            }
        }
    }
}
