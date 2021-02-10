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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Linq;
using System.Threading;

namespace SanteDB.Caching.Memory.Test
{
    [TestClass]
    public class MemoryCacheTests
    {

        /// <summary>
        /// Setup the test class
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            typeof(MemoryCacheService).Equals(null);
            TestApplicationContext.TestAssembly = typeof(MemoryCacheTests).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);
        }

        [TestMethod]
        public void TestCanAddCacheItem()
        {
            Guid k = Guid.NewGuid();
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Add(new Concept()
            {
                Key = k,
                Mnemonic = "I AM A TEST",
                ConceptNames = new System.Collections.Generic.List<ConceptName>()
                {
                    new ConceptName("en","I AM A TEST")
                }
            });

            var ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k);
            Assert.IsNotNull(ret);
            Assert.AreEqual("I AM A TEST", ret.Mnemonic);
            Assert.AreEqual(1, ret.ConceptNames.Count);
        }

        /// <summary>
        /// Tests that cache does not return expired data
        /// </summary>
        [TestMethod]
        public void TestDoesNotReturnAfterExpiry()
        {
            Guid k = Guid.NewGuid();
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Add(new Concept()
            {
                Key = k,
                Mnemonic = "I AM A TEST TIMEOUT",
                ConceptNames = new System.Collections.Generic.List<ConceptName>()
                {
                    new ConceptName("en","I AM A TEST TIMEOUT")
                }
            });

            var ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k);
            Assert.IsNotNull(ret);

            Thread.Sleep(10000); // wait 10 secs
            ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k);
            Assert.IsNull(ret);
        }

        /// <summary>
        /// Tests that the clear method works
        /// </summary>
        [TestMethod]
        public void TestClearsCache()
        {
            Guid k = Guid.NewGuid();
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Add(new Concept()
            {
                Key = k,
                Mnemonic = "I AM A TEST CLEAR",
                ConceptNames = new System.Collections.Generic.List<ConceptName>()
                {
                    new ConceptName("en","I AM A TEST CLEAR")
                }
            });

            var ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k);
            Assert.IsNotNull(ret);
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();
            Thread.Sleep(5000); // allow cache to clear
            ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k);
            Assert.IsNull(ret);
        }

        [TestMethod]
        public void TestRemovesItemFromCache()
        {
            Guid k1 = Guid.NewGuid(), k2 = Guid.NewGuid();
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Add(new Concept()
            {
                Key = k1,
                Mnemonic = "I AM A TEST REMAIN",
                ConceptNames = new System.Collections.Generic.List<ConceptName>()
                {
                    new ConceptName("en","I AM A TEST REMAIN")
                }
            });
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Add(new Concept()
            {
                Key = k2,
                Mnemonic = "I AM A TEST REMOVE",
                ConceptNames = new System.Collections.Generic.List<ConceptName>()
                {
                    new ConceptName("en","I AM A TEST REMOVE")
                }
            });

            var ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k1);
            Assert.IsNotNull(ret);
            Assert.AreEqual("I AM A TEST REMAIN", ret.Mnemonic);
            ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k2);
            Assert.IsNotNull(ret);
            Assert.AreEqual("I AM A TEST REMOVE", ret.Mnemonic);

            ApplicationServiceContext.Current.GetService<IDataCachingService>().Remove(k2);
            ret = ApplicationServiceContext.Current.GetService<IDataCachingService>().GetCacheItem<Concept>(k2);
            Assert.IsNull(ret);
        }

        [TestMethod]
        public void TestAdHocCache()
        {
            ApplicationServiceContext.Current.GetService<IAdhocCacheService>().Add("TEST", "I AM A TEST VALUE!");
            Assert.AreEqual("I AM A TEST VALUE!", ApplicationServiceContext.Current.GetService<IAdhocCacheService>().Get<String>("TEST"));
            ApplicationServiceContext.Current.GetService<IAdhocCacheService>().Add("COMPLEX", new
            {
                Message = "I AM COMPLEX",
                Value = 2
            }, new TimeSpan(0, 0, 5));
            dynamic val = ApplicationServiceContext.Current.GetService<IAdhocCacheService>().Get<dynamic>("COMPLEX");
            Assert.AreEqual("I AM COMPLEX", val.Message);
            Thread.Sleep(10000);
            // Should be gone
            val = ApplicationServiceContext.Current.GetService<IAdhocCacheService>().Get<dynamic>("COMPLEX");
            Assert.IsNull(val);
        }

        [TestMethod]
        public void TestCanRegisterQuery()
        {
            var qps = ApplicationServiceContext.Current.GetService<IQueryPersistenceService>();
            var qid = Guid.NewGuid();
            qps.RegisterQuerySet(qid, new Guid[] {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
                }, "TEST", 100);

            Assert.AreEqual(100, qps.QueryResultTotalQuantity(qid));
            Assert.AreEqual(3, qps.GetQueryResults(qid, 0, 100).Count());
            Assert.AreEqual("TEST", qps.GetQueryTag(qid));

            qps.AddResults(qid, new Guid[] { Guid.NewGuid() });
            Assert.AreEqual(4, qps.GetQueryResults(qid, 0, 100).Count());

            var q2 = qps.FindQueryId("TEST");
            Assert.AreEqual(qid, q2);
        }
    }
}
