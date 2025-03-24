/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Messaging.HDSI.Test
{
    /// <summary>
    /// Tests for the HTTP expression writer
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "REST API")]
    public class HttpQueryExpressionTest
    {

        [Test]
        public void TestExplicitOrFilterParameter()
        {
            // This query semantically should be interpreted as 
            //  - The community service delivery location for the object name is FOO_BAR
            //  OR
            //  - The community service delivery location for the object's parent is FOO_BAR
            var query = "relationship[CommunityServiceDeliveryLocation].source.name.component.value||relationship[Parent].source.relationship[CommunityServiceDeliveryLocation].source.name.component.value=FOO_BAR";
            var linq = QueryExpressionParser.BuildLinqExpression<Patient>(query.ParseQueryString());
            var roundtrip = QueryExpressionBuilder.BuildQuery<Patient>(linq).ToHttpString();
            Assert.IsTrue(roundtrip.Contains("||"), "Expected OR operator on query");
            Assert.AreEqual(query, roundtrip);
        }


        [Test]
        public void TestExplicitOrComplexFilterParameter()
        {
            // This query semantically should be interpreted as 
            //  - The community service delivery location for the object name is FOO_BAR
            //  OR
            //  - The community service delivery location for the object's parent is FOO_BAR
            var query = "relationship[CommunityServiceDeliveryLocation|DedicatedServiceDeliveryLocation].source.name.component.value||relationship[Parent|Child].source.relationship[CommunityServiceDeliveryLocation|DedicatedServiceDeliveryLocation].source.name.component.value=FOO_BAR";
            var linq = QueryExpressionParser.BuildLinqExpression<Patient>(query.ParseQueryString());
            var roundtrip = QueryExpressionBuilder.BuildQuery<Patient>(linq).ToHttpString();
            Assert.IsTrue(roundtrip.Contains("||"), "Expected OR operator on query");
            Assert.AreEqual(query, roundtrip);
        }

        [Test]
        public void TestComposeComplexGuards()
        {
            var http = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Relationships.Where(guard => guard.RelationshipType.Mnemonic == "Father" && guard.NegationIndicator == false).Any(patient => patient.TargetEntity.Identifiers.Any(identifier => identifier.Value == "FOO-30493")));
            Assert.AreEqual("relationship[relationshipType.mnemonic%3DFather%26negationInd%3Dfalse].target.identifier.value=FOO-30493", http.ToHttpString());
        }

        [Test]
        public void TestComposeControlParameters()
        {
            var query = "dateOfBirth=!null&_someOtherValue=true".ParseQueryString();
            var linq = QueryExpressionParser.BuildLinqExpression<Patient>(query, relayControlVariables: true);
            Assert.IsTrue(linq.ToString().Contains("WithControl"));
            Assert.IsTrue((bool)linq.Compile().DynamicInvoke(new Patient() { DateOfBirth = DateTime.Now }));

            query = "dateOfBirth=!null&_sub=$sub.id&_sub=$sub2.id".ParseQueryString();
            linq = QueryExpressionParser.BuildLinqExpression<Patient>(query, new System.Collections.Generic.Dictionary<string, Func<object>>()
            {
                { "sub", () => new Patient() { Key = Guid.Empty } },
                { "sub2", () => new Patient() { Key = Guid.Empty } }
            }, safeNullable: false, lazyExpandVariables: false, relayControlVariables: true);
            Assert.IsTrue((bool)linq.Compile().DynamicInvoke(new Patient() { DateOfBirth = DateTime.Now }));

            // Re-parse
            var parsedQuery = QueryExpressionBuilder.BuildQuery(linq);
            var parsedQueryString = parsedQuery.ToHttpString();
            Assert.AreEqual("dateOfBirth=%21null&_sub=00000000-0000-0000-0000-000000000000&_sub=00000000-0000-0000-0000-000000000000", parsedQueryString);
        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteLookupByKey()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Key == id);
            var expression = query.ToHttpString();
            Assert.AreEqual("id=00000000-0000-0000-0000-000000000000", expression);
        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteGuardByUuid()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Relationships.Where(g => g.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother).Any(r => r.TargetEntity.StatusConcept.Mnemonic == "ACTIVE"));
            var expression = query.ToHttpString();

            Assert.AreEqual("relationship[29ff64e5-b564-411a-92c7-6818c02a9e48].target.statusConcept.mnemonic=ACTIVE", expression);
        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedParse()
        {
            Guid id = Guid.Empty;
            var qstr = "classConcept.mnemonic=GenderCode&statusConcept.mnemonic=ACTIVE";

            var query = QueryExpressionParser.BuildLinqExpression<Place>(qstr.ParseQueryString());



        }

        /// <summary>
        /// Test that <see cref="QueryParameterAttribute"/> is used properly
        /// </summary>
        [Test]
        public void TestExtensionValue()
        {
            var hdsi = "extension[http://foo.com].$object@Entity.id";
            var query = QueryExpressionParser.BuildPropertySelector(typeof(Patient), hdsi, forceLoad: false, convertReturn: typeof(Guid?), returnNewObjectOnNull: false);

        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedWriter()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Place>(o => o.ClassConcept.Mnemonic == "GenderCode" && o.StatusConcept.Mnemonic == "ACTIVE");
            var expression = query.ToHttpString();

            Assert.AreEqual("classConcept.mnemonic=GenderCode&statusConcept.mnemonic=ACTIVE", expression);


        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedWriter2()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Concept>(o => o.ConceptSets.Any(p => p.Mnemonic == "GenderCode"));
            var expression = query.ToHttpString();

            Assert.AreEqual("conceptSet.mnemonic=GenderCode", expression);

        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteLookupAnd()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Key == id && o.GenderConcept.Mnemonic == "Male");
            var expression = query.ToHttpString();

            Assert.AreEqual("id=00000000-0000-0000-0000-000000000000&genderConcept.mnemonic=Male", expression);
        }

        /// <summary>
        /// Test query by or
        /// </summary>
        [Test]
        public void TestWriteLookupOr()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic == "Male" || o.GenderConcept.Mnemonic == "Female");
            var expression = query.ToHttpString();

            Assert.AreEqual("genderConcept.mnemonic=Male&genderConcept.mnemonic=Female", expression);
        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupGreaterThanEqual()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth >= dt);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3E%3D0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupGreaterThan()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth > dt);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3E0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupLessThanEqual()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth <= dt);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3C%3D0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupLessThan()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth < dt);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3C0001-01-01T00%3A00%3A00.0000000", expression);

        }


        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupNotEqual()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic != "Male");
            var expression = query.ToHttpString();

            Assert.AreEqual("genderConcept.mnemonic=%21Male", expression);
        }


        /// <summary>
        /// Test write of lookup approximately
        /// </summary>
        [Test]
        public void TestWriteLookupApproximately()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic.Contains("M"));
            var expression = query.ToHttpString();

            Assert.AreEqual("genderConcept.mnemonic=~M", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupAny()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Any(p => p.NameUse.Mnemonic == "Legal"));
            var expression = query.ToHttpString();

            Assert.AreEqual("name.use.mnemonic=Legal", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupAnyAnd()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Any(p => p.NameUse.Mnemonic == "Legal" && p.Component.Any(n => n.Value == "Smith")));
            var expression = query.ToHttpString();

            Assert.AreEqual("name.use.mnemonic=Legal&name.component.value=Smith", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupWhereAnd()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Where(p => p.NameUse.Mnemonic == "Legal").Any(p => p.Component.Any(n => n.Value == "Smith")));
            var expression = query.ToHttpString();

            Assert.AreEqual("name[Legal].component.value=Smith", expression);
        }


        /// <summary>
        /// Tests that the [QueryParameter] attribute is adhered to
        /// </summary>
        [Test]
        public void TestWriteNonSerializedProperty()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Extensions.Any(e => e.ExtensionDisplay == "1"));
            var expression = query.ToHttpString();

            Assert.AreEqual("extension.display=1", expression);
        }

        /// <summary>
        /// Test that the query writes out extended filter
        /// </summary>
        [Test]
        public void TestWriteSimpleExtendedFilter()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtension());
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Identifiers.Any(i => i.Value.TestExpression() <= 2));
            var expression = query.ToHttpString();

            Assert.AreEqual("identifier.value=%3A%28test%29%3C%3D2", expression);
        }

        /// <summary>
        /// Test that an HTTP query has extended filter
        /// </summary>
        [Test]
        public void TestWriteSimpleExtendedFilterWithParms()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            DateTime other = DateTime.Parse("1980-01-01");
            TimeSpan myTime = new TimeSpan(1, 0, 0, 0);
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth.Value.TestExpressionEx(other) < myTime);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3A%28testEx%7C1980-01-01T00%3A00%3A00.0000000%29%3C1.00%3A00%3A00", expression);
        }

        /// <summary>
        /// Test that an HTTP query has extended filter
        /// </summary>
        [Test]
        public void TestWriteSelfParameterRef()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            DateTime other = DateTime.Parse("1980-01-01");
            TimeSpan myTime = new TimeSpan(1, 0, 0, 0);
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth.Value.TestExpressionEx(o.CreationTime.DateTime) < myTime);
            var expression = query.ToHttpString();

            Assert.AreEqual("dateOfBirth=%3A%28testEx%7C%22%24_.creationTime%22%29%3C1.00%3A00%3A00", expression);
        }

    }
}
