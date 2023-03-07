﻿/*
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
 * Date: 2022-5-30
 */
using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Messaging.HDSI.Test
{

    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "REST API")]
    public class QueryParameterLinqBuilderTest
    {
        /// <summary>
        /// Test tht building of a simple AND & OR method
        /// </summary>
        [Test]
        public void TestAnyCondition()
        {
            var dtString = DateTime.Now;
            Expression<Func<Patient, bool>> expected = (o => o.Names.Any(name => name.NameUse.Mnemonic == "L"));

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name.use.mnemonic", "L");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test that using UUID as guard works
        /// </summary>
        [Test]
        public void TestBuildGuardMultiUuid()
        {
            String expected = "o => o.Names.Where(guard => ((guard.NameUseKey == Convert(effe122d-8d30-491d-805d-addcb4466c35)) OrElse (guard.NameUseKey == Convert(95e6843a-26ff-4046-b6f4-eb440d4b85f7)))).Any(name => name.Component.Any(component => (component.Value == \"SMITH\")))";

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add($"name[{NameUseKeys.Legal}|{NameUseKeys.Anonymous}].component.value", "SMITH");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test that using UUID as guard works
        /// </summary>
        [Test]
        public void TestBuildGuardUuid()
        {
            String expected = "o => o.Names.Where(guard => (guard.NameUseKey == Convert(effe122d-8d30-491d-805d-addcb4466c35))).Any(name => name.Component.Any(component => (component.Value == \"SMITH\")))";

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add($"name[{NameUseKeys.Legal}].component.value", "SMITH");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test tht building of a fuzzy date match
        /// </summary>
        [Test]
        public void TestBuildFuzzyDate()
        {
            String expected = $"o => ((o.DateOfBirth != null) AndAlso (((o.DateOfBirth.Value >= {new DateTime(2015, 01, 01)}) AndAlso (o.DateOfBirth.Value <= {new DateTime(2015, 12, 31, 23, 59, 59)})) == True))";

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", "~2015");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple AND method
        /// </summary>
        [Test]
        public void TestBuildSimpleAndLinqMethod()
        {
            Expression<Func<SecurityUser, bool>> expected = (o => o.UserName == "Charles" && o.Password == "20329132");

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("userName", "Charles");
            httpQueryParameters.Add("password", "20329132");
            var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple AND & OR method
        /// </summary>
        [Test]
        public void TestBuildSimpleAndOrLinqMethod()
        {
            var dtString = DateTime.Now;
            Expression<Func<SecurityUser, bool>> expected = (o => (o.UserName == "Charles" || o.UserName == "Charles2") && o.Password == "XXX");

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("userName", "Charles");
            httpQueryParameters.Add("userName", "Charles2");
            httpQueryParameters.Add("password", "XXX");
            var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test the building of a simple LINQ expression
        /// </summary>
        [Test]
        public void TestBuildSimpleLinqMethod()
        {
            Expression<Func<Concept, bool>> expected = (o => o.Mnemonic == "ENT");

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("mnemonic", "ENT");
            var expr = QueryExpressionParser.BuildLinqExpression<Concept>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple OR method
        /// </summary>
        [Test]
        public void TestBuildSimpleOrLinqMethod()
        {
            Expression<Func<Concept, bool>> expected = (o => o.Mnemonic == "EVN" || o.Mnemonic == "INT");

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("mnemonic", "EVN");
            httpQueryParameters.Add("mnemonic", "INT");
            var expr = QueryExpressionParser.BuildLinqExpression<Concept>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test query by entity identifier
        /// </summary>
        [Test]
        public void TestEntityIdentifierChain()
        {
            var dtString = DateTime.Now;
            Expression<Func<Entity, bool>> expected = (o => o.Identifiers.Any(identifier => identifier.IdentityDomain.Oid == "1.2.3.4" && identifier.Value == "123"));

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("identifier.domain.oid", "1.2.3.4");
            httpQueryParameters.Add("identifier.value", "123");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple AND & OR method
        /// </summary>
        [Test]
        public void TestGuardAndCondition()
        {
            var dtString = DateTime.Now;
            String expected = "o => o.Names.Where(guard => ((guard.NameUse ?? new Concept()).Mnemonic == \"L\")).Any(name => (name.Component.Where(guard => ((guard.ComponentType ?? new Concept()).Mnemonic == \"GIV\")).Any(component => (component.Value == \"John\")) AndAlso name.Component.Where(guard => ((guard.ComponentType ?? new Concept()).Mnemonic == \"FAM\")).Any(component => (component.Value == \"Smith\"))))";


            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name[L].component[GIV].value", "John");
            httpQueryParameters.Add("name[L].component[FAM].value", "Smith");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple AND & OR method
        /// </summary>
        [Test]
        public void TestGuardCondition()
        {
            var dtString = DateTime.Now;
            String expected = "o => o.Names.Where(guard => ((guard.NameUse ?? new Concept()).Mnemonic == \"L\")).Any(name => name.Component.Where(guard => ((guard.ComponentType ?? new Concept()).Mnemonic == \"GIV\")).Any(component => (component.Value == \"John\")))";
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name[L].component[GIV].value", "John");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Test tht building of a simple AND & OR method
        /// </summary>
        [Test]
        public void TestLessThanCreation()
        {
            var dtString = DateTime.Now;
            Expression<Func<SecurityUser, bool>> expected = (o => o.InvalidLoginAttempts < 4);

            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("invalidLoginAttempts", "<4");
            var expr = QueryExpressionParser.BuildLinqExpression<SecurityUser>(httpQueryParameters);
            Assert.AreEqual(expected.ToString(), expr.ToString());
        }

        /// <summary>
        /// Guard with null
        /// </summary>
        [Test]
        public void TestNullableCondition()
        {
            var dtString = new DateTime(1999, 01, 01);
            var tzo = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

            String expected = $"o => ((o.StartTime != null) AndAlso (o.StartTime.Value > {dtString}";
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("startTime", ">" + dtString);
            var expr = QueryExpressionParser.BuildLinqExpression<Act>(httpQueryParameters);
            Assert.IsTrue(expr.ToString().StartsWith(expected));
        }

        /// <summary>
        /// Guard with null
        /// </summary>
        [Test]
        public void TestNullGuardCondition()
        {
            var dtString = DateTime.Now;
            String expected = "o => o.Names.Where(guard => ((guard.NameUse ?? new Concept()).Mnemonic == \"L\")).Any(name => name.Component.Where(guard => (guard.ComponentType == null)).Any(component => (component.Value == \"John\")))";
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name[L].component[null].value", "John");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Tests creation of an OR guard condition
        /// </summary>
        [Test]
        public void TestOrGuardCondition()
        {
            String expected = "o => o.Names.Where(guard => (((guard.NameUse ?? new Concept()).Mnemonic == \"Legal\") Or ((guard.NameUse ?? new Concept()).Mnemonic == \"OfficialRecord\"))).Any(name => name.Component.Where(guard => (((guard.ComponentType ?? new Concept()).Mnemonic == \"Given\") Or ((guard.ComponentType ?? new Concept()).Mnemonic == \"Family\"))).Any(component => (component.Value == \"John\")))";
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name[Legal|OfficialRecord].component[Given|Family].value", "John");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());

        }


        /// <summary>
        /// Tests creation of an OR guard condition
        /// </summary>
        [Test]
        public void TestOrGuardParse()
        {
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name[Legal|OfficialRecord].component[Given|Family].value", "John");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            var pexpr = QueryExpressionBuilder.BuildQuery<Patient>(expr, true);
            Assert.AreEqual(httpQueryParameters.ToHttpString(), pexpr.ToHttpString());

        }

        /// <summary>
        /// Tests of LINQ using non-serialized property
        /// </summary>
        [Test]
        public void TestNonSerializedParse()
        {
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("extension.display", "1");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            var pexpr = QueryExpressionBuilder.BuildQuery<Patient>(expr, true);
            Assert.AreEqual(httpQueryParameters.ToHttpString(), pexpr.ToHttpString());

        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterParse()
        {
            var expected = "o => o.Names.Any(name => name.Component.Where(guard => ((guard.ComponentType ?? new Concept()).Mnemonic == \"OfficialRecord\")).Any(component => (component.Value.TestExpression() < 6)))";
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtension());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name.component[OfficialRecord].value", ":(test)<6");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterParseBool()
        {
            var expected = "o => o.Names.Any(name => name.Component.Where(guard => ((guard.ComponentType ?? new Concept()).Mnemonic == \"OfficialRecord\")).Any(component => (component.Value.BoolTest() == True)))";

            QueryFilterExtensions.AddExtendedFilter(new BoolQueryExtension());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("name.component[OfficialRecord].value", ":(testBool)");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterWithParameterParse()
        {
            var expected = $"o => ((o.DateOfBirth != null) AndAlso (o.DateOfBirth.Value.TestExpressionEx({new DateTime(2018, 01, 01)}) > 7300.00:00:00))";
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", ":(testEx|2018-01-01)>P20Y");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters);
            Assert.IsNotNull(expr);
        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterWithParameterVariableParse()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", ":(testEx|$dateOfBirth)>20y");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters, new System.Collections.Generic.Dictionary<string, Func<Object>>()
            {
                { "dateOfBirth" , () => DateTime.Parse("2018-01-01") }
            });
            Assert.IsNotNull(expr);

        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterWithParameterVariablePathParse()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", ":(testEx|$input.dateOfBirth)>2y");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters, new System.Collections.Generic.Dictionary<string, Func<Object>>()
            {
                { "input" , () => new Patient() { DateOfBirth = DateTime.Parse("2018-01-01") } }
            });
            Assert.IsNotNull(expr);
        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterWithParameterVariableComplexPathParse()
        {
            var expected = "o => ((o.DateOfBirth != null) AndAlso (o.DateOfBirth.Value.TestExpressionEx((Invoke(__xinstance => (((__xinstance.LoadCollection(\"Relationships\", False).Where(guard => ((guard.LoadProperty(\"RelationshipType\", False) ?? new Concept()).Mnemonic == \"Mother\")).FirstOrDefault() ?? new EntityRelationship()).LoadProperty(\"TargetEntity\", False) As Patient) ?? new Patient()).DateOfBirth, Convert(value(SanteDB.Messaging.HDSI.Test.QueryParameterLinqBuilderTest+<>c).<TestExtendedQueryFilterWithParameterVariableComplexPathParse>b__22_0())) ?? default(DateTime))) > 730.12:00:00))";
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", ":(testEx|$input.relationship[Mother].target@Patient.dateOfBirth)>2y");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters, "o", variables: new System.Collections.Generic.Dictionary<string, Func<Object>>()
            {
                { "input" , () => new Patient() { DateOfBirth = DateTime.Parse("2018-01-01") } }
            }, forceLoad: true);
            Assert.AreEqual(expected, expr.ToString());
        }

        /// <summary>
        /// Test the extended query filter has been parsed
        /// </summary>
        [Test]
        public void TestExtendedQueryFilterWithParameterVariableCrossReference()
        {
            var expected = "o => ((o.DateOfBirth != null) AndAlso (o.DateOfBirth.Value.TestExpressionEx((Invoke(__xinstance => (((__xinstance.LoadCollection(\"Relationships\", False).Where(guard => ((guard.LoadProperty(\"RelationshipType\", False) ?? new Concept()).Mnemonic == \"Mother\")).FirstOrDefault() ?? new EntityRelationship()).LoadProperty(\"TargetEntity\", False) As Patient) ?? new Patient()).DateOfBirth, o) ?? default(DateTime))) > 7300.00:00:00))";
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            NameValueCollection httpQueryParameters = new NameValueCollection();
            httpQueryParameters.Add("dateOfBirth", ":(testEx|$_.relationship[Mother].target@Patient.dateOfBirth)>P20Y");
            var expr = QueryExpressionParser.BuildLinqExpression<Patient>(httpQueryParameters, "o", safeNullable: true, forceLoad: true);
            Assert.AreEqual(expected, expr.ToString());
        }
    }
}