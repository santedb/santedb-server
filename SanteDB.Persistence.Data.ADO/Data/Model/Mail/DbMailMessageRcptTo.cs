/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-7-31
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Mail
{
	/// <summary>
	/// Represents an alert recipient.
	/// </summary>
	[Table("mail_msg_rcpt_to_tbl")]
	public class DbMailMessageRcptTo : DbAssociation
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DbMailMessageRcptTo"/> class.
		/// </summary>
		public DbMailMessageRcptTo()
		{
			
		}

		public DbMailMessageRcptTo(Guid alertId, Guid userId)
		{
			this.Key = alertId;
			this.SourceKey = userId;
		}

		/// <summary>
		/// Gets or sets the key of the object.
		/// </summary>
		[Column("mail_msg_id"), ForeignKey(typeof(DbMailMessage), nameof(DbMailMessage.Key))]
		public override Guid Key { get; set; }

		/// <summary>
		/// Gets or sets the key of the item associated with this object.
		/// </summary>
		[Column("usr_id"), ForeignKey(typeof(DbSecurityUser), nameof(DbSecurityUser.Key))]
		public override Guid SourceKey { get; set; }
	}
}
