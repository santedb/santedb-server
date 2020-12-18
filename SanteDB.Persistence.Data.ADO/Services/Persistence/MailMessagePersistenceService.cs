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
using SanteDB.Core.Mail;
using SanteDB.Core.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Mail;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents an alert persistence service.
    /// </summary>
    public class MailMessagePersistenceService : BaseDataPersistenceService<MailMessage, DbMailMessage>
	{

        public MailMessagePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

		/// <summary>
		/// Converts a <see cref="MailMessage"/> instance to an <see cref="DbMailMessage"/> instance.
		/// </summary>
		/// <param name="modelInstance">The alert message instance.</param>
		/// <param name="context">The data context.</param>
		/// <param name="princpal">The authentication context.</param>
		/// <returns>Returns the converted instance.</returns>
		public override object FromModelInstance(MailMessage modelInstance, DataContext context)
		{
			var alert = base.FromModelInstance(modelInstance, context) as DbMailMessage;

			alert.Flags = (int)modelInstance.Flags;

			return alert;
		}

		/// <summary>
		/// Inserts an alert.
		/// </summary>
		/// <param name="context">The data context.</param>
		/// <param name="data">The alert to insert.</param>
		/// <param name="principal">The authentication context.</param>
		/// <returns>Returns the inserted alert.</returns>
		public override MailMessage InsertInternal(DataContext context, MailMessage data)
		{
			var alert = base.InsertInternal(context, data);

			foreach (var securityUser in alert.RcptToXml)
			{
				context.Insert(new DbMailMessageRcptTo(data.Key.Value, securityUser));
			}

			return alert;
		}

		/// <summary>
		/// Converts a <see cref="DbMailMessage"/> instance to a <see cref="MailMessage"/> instance.
		/// </summary>
		/// <param name="dataInstance">The db alert message instance.</param>
		/// <param name="context">The data context.</param>
		/// <param name="principal">The authentication context.</param>
		/// <returns>Returns the converted instance.</returns>
		public override MailMessage ToModelInstance(object dataInstance, DataContext context)
		{
			var modelInstance = base.ToModelInstance(dataInstance, context);

			modelInstance.Flags = (MailMessageFlags)(dataInstance as DbMailMessage).Flags;

            SqlStatement rcptStatement = context.CreateSqlStatement()
                .SelectFrom(typeof(DbSecurityUser))
                .InnerJoin<DbSecurityUser, DbMailMessageRcptTo> (o=>o.Key, o=>o.SourceKey)
                .Where<DbMailMessageRcptTo>(o=>o.Key == modelInstance.Key).Build();

            modelInstance.RcptToXml = context.Query<DbSecurityUser>(rcptStatement).Select(o => o.Key).ToList();
			return modelInstance;
		}
	}
}
