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
 * Date: 2022-5-30
 */
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Represents an abstract data test tool.
    /// </summary>
    //[DeploymentItem(@"santedb_test.fdb")]
    //[DeploymentItem(@"fbclient.dll")]
    //[DeploymentItem(@"firebird.conf")]
    //[DeploymentItem(@"firebird.msg")]
    //[DeploymentItem(@"ib_util.dll")]
    //[DeploymentItem(@"icudt52.dll")]
    //[DeploymentItem(@"icudt52l.dat")]
    //[DeploymentItem(@"icuin52.dll")]
    //[DeploymentItem(@"icuuc52.dll")]
    //[DeploymentItem(@"plugins\engine12.dll", "plugins")]
    //[DeploymentItem(@"FirebirdSql.Data.FirebirdClient.dll")]
    [ExcludeFromCodeCoverage]
    public abstract class DataTest
    {

        /// <summary>
        /// Starts the data test 
        /// </summary>
        protected DataTest()
        {
        }
    }
}
