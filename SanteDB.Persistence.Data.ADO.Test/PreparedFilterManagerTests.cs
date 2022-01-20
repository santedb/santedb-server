using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers.Postgres;
using SanteDB.Persistence.Data.ADO.Data.Hax;
using SanteDB.Persistence.Data.ADO.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Prepared manager filter tests
    /// </summary>
    [TestFixture]
    public class PreparedFilterManagerTests : PersistenceTest<Entity>
    {

        [Test]
        public void TestCanConstructRefreshStatementSimple()
        {
            var si = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var fm = si.CreateInjected<AdoPreparedFilterManager>();
            var expected = "SELECT ent_tbl.ent_id , T0.id_val FROM pat_tbl INNER JOIN psn_tbl USING (ent_vrsn_id) INNER JOIN ent_vrsn_tbl USING (ent_vrsn_id) INNER JOIN ent_tbl USING (ent_id) LEFT JOIN ent_id_tbl AS T0 ON (ent_tbl.ent_id = T0.ent_id  AND T0.obslt_vrsn_seq_id IS NULL)WHERE  ent_vrsn_tbl.obslt_utc IS NULL";
            var sql = fm.CreatePopulateSql(new PostgreSQLProvider(), new Data.Index.DbPreparedFilterDefinition()
            {
                FilterExpression = "identifier.value",
                Indexer = "soundex",
                Name = "Sample Test",
                StoreName = "tbl_test_pf",
                TargetType = "SanteDB.Core.Model.Roles.Patient, SanteDB.Core.Model"
            }).Build().SQL;
            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void TestCanConstructRefreshStatementSimpleGuard()
        {
            var si = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var fm = si.CreateInjected<AdoPreparedFilterManager>();
            var expected = String.Empty;
            var sql = fm.CreatePopulateSql(new PostgreSQLProvider(), new Data.Index.DbPreparedFilterDefinition()
            {
                FilterExpression = "identifier[SSN|NHID].value",
                Indexer = "soundex",
                Name = "Sample Test",
                StoreName = "tbl_test_pf",
                TargetType = "SanteDB.Core.Model.Roles.Patient, SanteDB.Core.Model"
            }).Build().SQL;
            Assert.AreEqual(expected, sql);
        }


        [Test]
        public void TestCanConstructRefreshStatementUuidGuard()
        {
            var si = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var fm = si.CreateInjected<AdoPreparedFilterManager>();
            var expected = String.Empty;
            var sql = fm.CreatePopulateSql(new PostgreSQLProvider(), new Data.Index.DbPreparedFilterDefinition()
            {
                FilterExpression = "identifier[af1ecad7-0e74-4508-8818-ed2e5c021348|EID].value",
                Indexer = "soundex",
                Name = "Sample Test",
                StoreName = "tbl_test_pf",
                TargetType = "SanteDB.Core.Model.Roles.Patient, SanteDB.Core.Model"
            }).Build().SQL;
            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void TestCanConstructRefreshStatementNested()
        {
            var si = ApplicationServiceContext.Current.GetService<IServiceManager>();
            var fm = si.CreateInjected<AdoPreparedFilterManager>();
            var expected = String.Empty;
            var sql = fm.CreatePopulateSql(new PostgreSQLProvider(), new Data.Index.DbPreparedFilterDefinition()
            {
                FilterExpression = "name.component.value",
                Indexer = "soundex",
                Name = "Sample Test",
                StoreName = "tbl_test_pf",
                TargetType = "SanteDB.Core.Model.Roles.Patient, SanteDB.Core.Model"
            }).Build().SQL;
            Assert.AreEqual(expected, sql);
        }


        [Test]
        public void TestIndexFilterQueryHack()
        {
            var modelMapper = new ModelMapper(typeof(AdoPreparedFilterManager).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName));
            List<IQueryBuilderHack> hax = new List<IQueryBuilderHack>() { new SecurityUserEntityQueryHack(), new RelationshipGuardQueryHack(), new CreationTimeQueryHack(modelMapper), new EntityAddressNameQueryHack() }; //, serviceManager.CreateInjected<AdoPreparedFilterQueryHack>() };
            var queryBuilder = new QueryBuilder(modelMapper, new PostgreSQLProvider(), hax.ToArray());
            var gender = Guid.Parse("094941e9-a3db-48b5-862c-bc289bd7f86c");
            var dob = DateTime.Parse("1976-05-01");
            var sql = queryBuilder.CreateQuery<Patient>(o => o.DateOfBirth == dob &&  o.Names.Where(guard => guard.NameUse.Mnemonic == "Legal").Any(p => p.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "Jim" || c.Value == "Smith")) && StatusKeys.ActiveStates.Contains(o.StatusConceptKey.Value)).Build().SQL;
        }
    }
}
