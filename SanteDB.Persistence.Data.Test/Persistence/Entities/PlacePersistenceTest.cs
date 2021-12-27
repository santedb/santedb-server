using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Tests the peristence of places
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Place Entity")]
    public class PlacePersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests that we can perform simple inserts
        /// </summary>
        [Test]
        public void TestInsertSimple()
        {

            using(AuthenticationContext.EnterSystemContext())
            {
                var place = new Place()
                {
                    ClassConceptKey = EntityClassKeys.ServiceDeliveryLocation,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Good Health Hospital")
                    },
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new Core.Model.DataTypes.EntityIdentifier(AssigningAuthorityKeys.Gs1GlobalLocationNumber, "34943934943")
                    },
                    GeoTag = new Core.Model.DataTypes.GeoTag(10, 10, true)
                };

                var afterInsert = base.TestInsert(place);
                Assert.AreEqual(1, afterInsert.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Good Health Hospital", afterInsert.Names.First().LoadProperty(o=> o.Component).First().Value);
                Assert.AreEqual(1, afterInsert.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual(AssigningAuthorityKeys.Gs1GlobalLocationNumber, afterInsert.Identifiers.First().AuthorityKey);
                Assert.AreEqual("34943934943", afterInsert.Identifiers.First().Value);
                Assert.IsNull(afterInsert.GeoTag);
                Assert.AreEqual(10, afterInsert.LoadProperty(o => o.GeoTag).Lat);

                // Test querying 
                var afterQuery = base.TestQuery<Place>(o => o.Identifiers.Any(i => i.Value == "34943934943" && i.Authority.DomainName == "GLN"), 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Names);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual("Good Health Hospital", afterQuery.Names.First().LoadProperty(o => o.Component).First().Value);
                Assert.IsNull(afterQuery.Identifiers);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual(AssigningAuthorityKeys.Gs1GlobalLocationNumber, afterQuery.Identifiers.First().AuthorityKey);
                Assert.AreEqual("34943934943", afterQuery.Identifiers.First().Value);
                Assert.IsNull(afterQuery.GeoTag);
                Assert.IsFalse(afterQuery.IsMobile);
                Assert.AreEqual(10, afterQuery.LoadProperty(o => o.GeoTag).Lat);

                // Test update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LoadProperty(a => a.Identifiers).First().Value = "999999999";
                    o.LoadProperty(a => a.Addresses).Add(new EntityAddress(AddressUseKeys.Direct, "123 Main Street", "Hamilton", "Ontario", "CA", "L8K5N2"));
                    o.IsMobile = true;
                    return o;
                });
                Assert.IsTrue(afterUpdate.IsMobile);

                afterQuery = base.TestQuery<Place>(o => o.Identifiers.Any(i => i.Value == "34943934943" && i.Authority.DomainName == "GLN"), 0).FirstOrDefault();
                afterQuery = base.TestQuery<Place>(o => o.Identifiers.Any(i => i.Value == "999999999" && i.Authority.DomainName == "GLN"), 1).AsResultSet().First();
                afterQuery = base.TestQuery<Place>(o => o.IsMobile == true && o.GeoTag.Lat == 10 && o.GeoTag.Lng == 10, 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Identifiers);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual(AssigningAuthorityKeys.Gs1GlobalLocationNumber, afterQuery.Identifiers.First().AuthorityKey);
                Assert.AreEqual("999999999", afterQuery.Identifiers.First().Value);
                Assert.IsTrue(afterQuery.IsMobile);
                Assert.IsNull(afterQuery.Addresses);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Addresses).Count);
                Assert.AreEqual("Hamilton", afterQuery.Addresses.First().LoadProperty(o => o.Component).First(o => o.ComponentTypeKey == AddressComponentKeys.City).Value);
                Assert.IsNull(afterQuery.GeoTag);
                Assert.AreEqual(10, afterQuery.LoadProperty(o => o.GeoTag).Lat);
            }
        }

        /// <summary>
        /// Tests that we can insert and administer places with services
        /// </summary>
        [Test]
        public void TestInsertServices()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                Guid service1 = Guid.Parse("15973a71-5f36-4359-bd94-ec70b5823105"),
                    service2 = Guid.Parse("de4025a5-4372-4150-8b22-8739b22ea91e");

                var place = new Place()
                {
                    ClassConceptKey = EntityClassKeys.ServiceDeliveryLocation,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Other Health Clinic")
                    },
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new Core.Model.DataTypes.EntityIdentifier(AssigningAuthorityKeys.Gs1GlobalLocationNumber, "1234567890987654321")
                    },
                    GeoTag = new Core.Model.DataTypes.GeoTag(20, 20, true),
                    Services = new List<PlaceService>()
                    {
                        new PlaceService()
                        {
                            ServiceSchedule = "Monday 9-5 ; Tuesday 10-11",
                            ServiceConceptKey = service1 // Immunization Service Type
                        }
                    }
                };

                var afterInsert = base.TestInsert(place);
                Assert.AreEqual(1, afterInsert.LoadProperty(o => o.Services).Count);


                // Test querying 
                var afterQuery = base.TestQuery<Place>(o => o.Services.Any(s => s.ServiceConceptKey == service1), 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Services);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Services).Count);
                Assert.AreEqual(service1, afterQuery.Services.First().ServiceConceptKey);

                // Test update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LoadProperty(a => a.Services).Add(new PlaceService()
                    {
                        ServiceSchedule = "Monday 18:00-23:00",
                        ServiceConceptKey = service2
                    });
                    return o;
                });

                afterQuery = base.TestQuery<Place>(o => o.Identifiers.Any(i => i.Value == "1234567890987654321" && i.Authority.DomainName == "GLN"), 1).AsResultSet().First();
                afterQuery = base.TestQuery<Place>(o => o.Services.Any(s => s.ServiceConceptKey == service2), 1).AsResultSet().First();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Services).Count);

                // Ensure removal of service
                // Test update
                afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LoadProperty(a => a.Services).RemoveAll(s=>s.ServiceConceptKey == service1);
                    return o;
                });

                base.TestQuery<Place>(o => o.Services.Any(s => s.ServiceConceptKey == service1), 0);
                afterQuery = base.TestQuery<Place>(o => o.Services.Any(s => s.ServiceConceptKey == service2), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Services).Count);

            }
        }
        
    }
}
