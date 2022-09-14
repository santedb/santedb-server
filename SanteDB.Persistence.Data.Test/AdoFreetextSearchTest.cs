using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Tests for the ADO.NET freetext search service
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AdoFreetextSearchTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests that the basic freetext search service works properly
        /// </summary>
        [Test]
        public void TestBasicFreetextSearch()
        {
            using(AuthenticationContext.EnterSystemContext())
            {

                // Get the ADO freetext service
                var freetextService = ApplicationServiceContext.Current.GetService<IFreetextSearchService>();
                Assert.IsNotNull(freetextService);

                // Force the rebuild
                var jobManagerService = ApplicationServiceContext.Current.GetService<IJobManagerService>();
                Assert.IsNotNull(jobManagerService);
                var rebuildJob = jobManagerService.GetJobInstance(Guid.Parse(AdoRebuildFreetextIndexJob.JobUuid));
                Assert.IsNotNull(rebuildJob, "Job was not registered");

                // Build
                rebuildJob.Run(this, EventArgs.Empty, new object[0]);

                // Ensure search for name
                var results = freetextService.SearchEntity<Place>(new string[] { "United" });
                Assert.GreaterOrEqual(results.Count(), 2);
                var ordered = results.OrderByDescending(o => o.VersionSequence);
                Assert.Greater(ordered.First().VersionSequence, ordered.Skip(1).First().VersionSequence);


            }
        }
    }
}
