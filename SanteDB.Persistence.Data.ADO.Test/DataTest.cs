using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.IO;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Represents an abstract data test tool
    /// </summary>
    [DeploymentItem(@"santedb_test.fdb")]
    [DeploymentItem(@"fbclient.dll")]
    [DeploymentItem(@"firebird.conf")]
    [DeploymentItem(@"firebird.msg")]
    [DeploymentItem(@"ib_util.dll")]
    [DeploymentItem(@"icudt52.dll")]
    [DeploymentItem(@"icudt52l.dat")]
    [DeploymentItem(@"icuin52.dll")]
    [DeploymentItem(@"icuuc52.dll")]
    [DeploymentItem(@"plugins\engine12.dll", "plugins")]
    [DeploymentItem(@"FirebirdSql.Data.FirebirdClient.dll")]
    public abstract class DataTest
    {
        
        /// <summary>
        /// Starts the data test 
        /// </summary>
        public DataTest()
        {
        }
    }
}