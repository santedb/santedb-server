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
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Roles;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers.Postgres;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.Diagnostics;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    [TestFixture(Category = "Persistence")]
    public class QueryExpressionWriterTest
    {

        private QueryBuilder m_builder = new QueryBuilder(new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName)), new PostgreSQLProvider());

        /// <summary>
        /// Test that the function constructs an empty select statement
        /// </summary>
        [Test]
        public void TestConstructsEmptySelectStatement()
        {
            SqlStatement sql = new SqlStatement<DbAct>(new PostgreSQLProvider()).SelectFrom().Build();
            Assert.IsTrue(sql.SQL.Contains("SELECT act_tbl.tpl_id,act_tbl.cls_cd_id,act_tbl.mod_cd_id,act_tbl.act_id FROM act_tbl"));
            Assert.AreEqual(0, sql.Arguments.Count());
        }

        /// <summary>
        /// Tests that the function constructs parameters
        /// </summary>
        [Test]
        public void TestConstructsParameters()
        {
            SqlStatement sql = new SqlStatement<DbAct>(new PostgreSQLProvider()).SelectFrom().Where("act_id = ?", Guid.NewGuid()).Build();
            Assert.IsTrue(sql.SQL.Contains("SELECT act_tbl.tpl_id,act_tbl.cls_cd_id,act_tbl.mod_cd_id,act_tbl.act_id FROM act_tbl"));
            Assert.AreEqual(1, sql.Arguments.Count());
            Assert.IsTrue(sql.SQL.Contains("act_id = "));
        }


        /// <summary>
        /// Tests that the function constructs parameters
        /// </summary>
        [Test]
        public void TestConstructLocalParameters()
        {
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom().Where("act_id = ?", Guid.NewGuid()).And("act_utc < ?", DateTime.Now).Build();
            Assert.IsTrue(sql.SQL.Contains("AND"));
            Assert.IsTrue(sql.SQL.Contains("act_id"));
            Assert.IsTrue(sql.SQL.Contains("act_utc"));
            Assert.IsTrue(sql.SQL.Contains("SELECT act_vrsn_tbl.neg_ind,act_vrsn_tbl.act_utc,act_vrsn_tbl.act_start_utc,act_vrsn_tbl.act_stop_utc,act_vrsn_tbl.rsn_cd_id,act_vrsn_tbl.sts_cd_id,act_vrsn_tbl.typ_cd_id,act_vrsn_tbl.act_vrsn_id,act_vrsn_tbl.act_id,act_vrsn_tbl.vrsn_seq_id,act_vrsn_tbl.rplc_vrsn_id,act_vrsn_tbl.crt_prov_id,act_vrsn_tbl.obslt_prov_id,act_vrsn_tbl.crt_utc,act_vrsn_tbl.obslt_utc FROM act_vrsn_tbl"));
            Assert.AreEqual(2, sql.Arguments.Count());
            Assert.IsAssignableFrom<Guid>(sql.Arguments.First());
            Assert.IsAssignableFrom<DateTime>(sql.Arguments.Last());
        }

        /// <summary>
        /// Test re-writing of simple LINQ
        /// </summary>
        [Test]
        public void TestRewriteSimpleLinq()
        {
            Guid mg = Guid.NewGuid();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom().Where(o => o.IsNegated == true).Build();
            sw.Stop();
            Assert.IsTrue(sql.SQL.Contains("neg_ind =  ?"));
            Assert.AreEqual(1, sql.Arguments.Count());

        }

        /// <summary>
        /// Test re-writing of simple LINQ
        /// </summary>
        [Test]
        public void TestRewriteComplexLinq()
        {
            Guid mg = Guid.NewGuid();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            SqlStatement sql = new SqlStatement<DbActVersion>(new PostgreSQLProvider()).SelectFrom().Where(o => o.Key == mg || o.Key == Guid.NewGuid() && o.CreationTime <= DateTime.Now).Build();
            sw.Stop();

            Assert.IsTrue(sql.SQL.Contains("AND"));
            Assert.IsTrue(sql.SQL.Contains("act_id"));
            Assert.IsTrue(sql.SQL.Contains("crt_utc"));
            Assert.IsTrue(sql.SQL.Contains("SELECT act_vrsn_tbl.neg_ind,act_vrsn_tbl.act_utc,act_vrsn_tbl.act_start_utc,act_vrsn_tbl.act_stop_utc,act_vrsn_tbl.rsn_cd_id,act_vrsn_tbl.sts_cd_id,act_vrsn_tbl.typ_cd_id,act_vrsn_tbl.act_vrsn_id,act_vrsn_tbl.act_id,act_vrsn_tbl.vrsn_seq_id,act_vrsn_tbl.rplc_vrsn_id,act_vrsn_tbl.crt_prov_id,act_vrsn_tbl.obslt_prov_id,act_vrsn_tbl.crt_utc,act_vrsn_tbl.obslt_utc FROM act_vrsn_tbl"));
            Assert.AreEqual(1, sql.Arguments.Count());
            Assert.IsAssignableFrom<Guid>(sql.Arguments.First());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [Test]
        public void TestModelQueryShouldJoin()
        {

            var query = m_builder.CreateQuery<Patient>(o => o.DeterminerConceptKey == DeterminerKeys.Specific).Build();
            Assert.IsTrue(query.SQL.Contains("pat_tbl.ent_vrsn_id"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));

        }

        /// <summary>
        /// Test re
        /// </summary>
        [Test]
        public void TestModelQueryShouldExistsClause()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Patient>(o => o.DeterminerConcept.Mnemonic == "Instance").Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("pat_tbl.ent_vrsn_id"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("IN"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [Test]
        public void TestQueryShouldWriteNestedJoin()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Entity>(o => o.Identifiers.Any(i => i.Value == "123" && i.Authority.Oid == "1.2.3.4.6.7.8.9" || i.Value == "321" && i.Authority.Oid == "1.2.3.4.5.6.7.8") && o.ClassConceptKey == EntityClassKeys.Organization).Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT ent_vrsn_tbl.ent_id,ent_vrsn_tbl.sts_cd_id,ent_vrsn_tbl.typ_cd_id,ent_vrsn_tbl.ent_vrsn_id,ent_vrsn_tbl.crt_act_id,ent_vrsn_tbl.vrsn_seq_id,ent_vrsn_tbl.rplc_vrsn_id,ent_vrsn_tbl.crt_prov_id,ent_vrsn_tbl.obslt_prov_id,ent_vrsn_tbl.crt_utc,ent_vrsn_tbl.obslt_utc,ent_tbl.tpl_id,ent_tbl.cls_cd_id,ent_tbl.dtr_cd_id  FROM ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_tbl"));
            Assert.IsTrue(query.SQL.Contains("IN"));
            Assert.AreEqual(5, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [Test]
        public void TestModelQueryShouldSubQueryClause()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Patient>(o => o.Participations.Where(guard => guard.ParticipationRole.Mnemonic == "RecordTarget").Any(sub => sub.PlayerEntity.ObsoletionTime == null)).Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("pat_tbl.ent_vrsn_id"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("IN"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

        /// <summary>
        /// Test re
        /// </summary>
        [Test]
        public void TestModelQueryShouldSubQueryIntersect()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var query = m_builder.CreateQuery<Entity>(o => o.Names.Where(guard => guard.NameUse.Mnemonic == "Legal").Any(v => v.Component.Where(b => b.ComponentType.Mnemonic == "Family").Any(n => n.Value == "Smith")) &&
                o.Names.Where(guard => guard.NameUse.Mnemonic == "Legal").Any(v => v.Component.Where(b => b.ComponentType.Mnemonic == "Given").Any(n => n.Value == "John" || n.Value == "Jacob"))).Build();
            sw.Stop();

            Assert.IsTrue(query.SQL.Contains("SELECT ent_vrsn_tbl.ent_id,ent_vrsn_tbl.sts_cd_id,ent_vrsn_tbl.typ_cd_id,ent_vrsn_tbl.ent_vrsn_id,ent_vrsn_tbl.crt_act_id,ent_vrsn_tbl.vrsn_seq_id,ent_vrsn_tbl.rplc_vrsn_id,ent_vrsn_tbl.crt_prov_id,ent_vrsn_tbl.obslt_prov_id,ent_vrsn_tbl.crt_utc,ent_vrsn_tbl.obslt_utc,ent_tbl.tpl_id,ent_tbl.cls_cd_id,ent_tbl.dtr_cd_id  FROM ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_tbl"));
            Assert.IsTrue(query.SQL.Contains("IN"));
            Assert.IsTrue(query.SQL.Contains("mnemonic = ?"));
            Assert.AreEqual(6, query.Arguments.Count());
        }

        /// <summary>
        /// Tests that the query writer works properly when querying based on a property that is non-serialized
        /// </summary>
        [Test]
        public void TestModelQueryShouldUseNonSerialized()
        {
            var query = m_builder.CreateQuery<Entity>(o => o.Extensions.Any(e => e.ExtensionDisplay == "1")).Build();
            Assert.IsTrue(query.SQL.Contains("SELECT ent_vrsn_tbl.ent_id,ent_vrsn_tbl.sts_cd_id,ent_vrsn_tbl.typ_cd_id,ent_vrsn_tbl.ent_vrsn_id,ent_vrsn_tbl.crt_act_id,ent_vrsn_tbl.vrsn_seq_id,ent_vrsn_tbl.rplc_vrsn_id,ent_vrsn_tbl.crt_prov_id,ent_vrsn_tbl.obslt_prov_id,ent_vrsn_tbl.crt_utc,ent_vrsn_tbl.obslt_utc,ent_tbl.tpl_id,ent_tbl.cls_cd_id,ent_tbl.dtr_cd_id  FROM ent_vrsn_tbl"));
            Assert.IsTrue(query.SQL.Contains("INNER JOIN ent_tbl"));
            Assert.IsTrue(query.SQL.Contains("IN"));
            Assert.IsTrue(query.SQL.Contains("ext_disp"));
            Assert.AreEqual(1, query.Arguments.Count());
        }

     
    }
}
