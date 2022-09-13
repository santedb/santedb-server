using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Subscription;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Subsription repository
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class TestSubscriptionRepository : ISubscriptionRepository
    {

        public const string AllEntitiesCreatedAfterDateParm = "9380E2E5-C35D-4792-A03B-BFCEC9AB2236";
        public const string AllPatientsOrPersonsEntitiesInCity = "93DDDEBD-AA97-40E8-BECE-C557E9A9745B";

        /// <summary>
        /// Get the service name
        /// </summary>
        public string ServiceName => "Test Subscription Repository";

        private SubscriptionDefinition[] m_subscriptions =
        {
            new SubscriptionDefinition()
            {
                Key = Guid.Parse(AllEntitiesCreatedAfterDateParm),
                Resource = "Entity",
                Uuid = Guid.Parse(AllEntitiesCreatedAfterDateParm),
                ClientDefinitions = new List<SubscriptionClientDefinition>()
                {
                    new SubscriptionClientDefinition()
                    {
                        Mode = SubscriptionModeType.Subscription,
                        IgnoreModifiedOn =true,
                        Resource = "Entity",
                        Trigger = SubscriptionTriggerType.Always,
                        Filters = new List<string>()
                        {
                            "_creationDate=2022-01-01"
                        }
                    }
                },
                ServerDefinitions = new List<SubscriptionServerDefinition>()
                {
                    new SubscriptionServerDefinition()
                    {
                        InvariantName = "FirebirdSQL",
                        Definition = @"SELECT * FROM ENT_VRSN_TBL INNER JOIN ENT_TBL USING (ENT_ID) WHERE CRT_UTC > ${_creationDate} ORDER BY VRSN_SEQ_ID DESC"
                    }
                }
            },
            new SubscriptionDefinition()
            {
                Key = Guid.Parse(AllPatientsOrPersonsEntitiesInCity),
                Uuid = Guid.Parse(AllPatientsOrPersonsEntitiesInCity),
                Resource = "Entity",
                ServerDefinitions = new List<SubscriptionServerDefinition>()
                {
                    new SubscriptionServerDefinition()
                    {
                        InvariantName = "FirebirdSQL",
                        Definition = @"SELECT * FROM PSN_TBL INNER JOIN ENT_VRSN_TBL  USING(ENT_VRSN_ID)
                                        WHERE 
                                        CLS_CD_ID IN ('" + EntityClassKeyStrings.Person + "', '" + EntityClassKeyStrings.Patient + @"')
                                        AND EXISTS (
                                            SELECT 1 FROM 
                                            ENT_ADDR_CMP_TBL INNER JOIN ENT_ADDR_TBL USING (ADDR_ID)
                                            WHERE VAL IN (${_city}) AND ENT_ADDR_CMP_TBL.TYP_CD_ID = '" + AddressComponentKeys.City.ToString() + @"' AND 
                                            ENT_ADDR_TBL.OBSLT_VRSN_SEQ_ID IS NULL AND ENT_ADDR_TBL.ENT_ID = ENT_VRSN_TBL.ENT_ID
                                        ) "
                    }
                }
            }
        };

        /// <summary>
        /// Delete subscription
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public SubscriptionDefinition Delete(Guid key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find subscription
        /// </summary>
        public IQueryResultSet<SubscriptionDefinition> Find(Expression<Func<SubscriptionDefinition, bool>> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find subscription
        /// </summary>
        public IEnumerable<SubscriptionDefinition> Find(Expression<Func<SubscriptionDefinition, bool>> query, int offset, int? count, out int totalResults, params ModelSort<SubscriptionDefinition>[] orderBy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get subscription definition by key
        /// </summary>
        public SubscriptionDefinition Get(Guid key)
        {
            return this.m_subscriptions.FirstOrDefault(o => o.Key == key || o.Uuid == key);
        }

        /// <summary>
        /// Get subscription with version
        /// </summary>
        public SubscriptionDefinition Get(Guid key, Guid versionKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert subscription definition
        /// </summary>
        public SubscriptionDefinition Insert(SubscriptionDefinition data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Save subscription definition
        /// </summary>
        public SubscriptionDefinition Save(SubscriptionDefinition data)
        {
            throw new NotImplementedException();
        }
    }
}