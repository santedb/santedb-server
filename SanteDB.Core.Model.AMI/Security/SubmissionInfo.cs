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
 * User: khannan
 * Date: 2017-9-1
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
	/// <summary>
	/// Enrollment information
	/// </summary>
	[XmlType(nameof(SubmissionInfo), Namespace = "http://santedb.org/ami")]
	[XmlRoot(nameof(SubmissionInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SubmissionInfo))]
	public class SubmissionInfo
	{
		/// <summary>
		/// Administration contact
		/// </summary>
		[XmlElement("adminContact"), JsonProperty("adminContact")]
		public string AdminContact { get; set; }

		/// <summary>
		/// Administration
		/// </summary>
        [XmlElement("adminAddress"), JsonProperty("adminAddress")]
		public String AdminSiteAddress { get; set; }

		/// <summary>
		/// Disposition message
		/// </summary>
		[XmlElement("message"), JsonProperty("message")]
		public string DispositionMessage { get; set; }

		/// <summary>
		/// DN
		/// </summary>
		[XmlElement("dn"), JsonProperty("dn")]
		public string DistinguishedName { get; set; }

		/// <summary>
		/// Email address of user
		/// </summary>
		[XmlElement("email"), JsonProperty("email")]
		public string EMail { get; set; }

		/// <summary>
		/// Expiry
		/// </summary>
		[XmlElement("notAfter"), JsonProperty("notAfter")]
		public string NotAfter { get; set; }

		/// <summary>
		/// Before date
		/// </summary>
		[XmlElement("notBefore"), JsonProperty("notBefore")]
		public string NotBefore { get; set; }

		/// <summary>
		/// RequestId
		/// </summary>
		[XmlAttribute("id"), JsonProperty("id")]
		public string RequestID { get; set; }

		/// <summary>
		/// Resolved on
		/// </summary>
		[XmlElement("resolved"), JsonProperty("resolved")]
		public string ResolvedWhen { get; set; }

		/// <summary>
		/// Revoke reason from Keystore
		/// </summary>
		[XmlIgnore]
		public String RevokedReason
		{
			get { return ((int)this.XmlRevokeReason).ToString(); }
			set
			{
				if (value != null)
					this.XmlRevokeReason = (RevokeReason)Int32.Parse(value);
			}
		}

		/// <summary>
		/// Revoked on
		/// </summary>
		[XmlElement("revoked"), JsonProperty("revoked")]
		public string RevokedWhen { get; set; }

		/// <summary>
		/// Submitted on
		/// </summary>
		[XmlElement("submitted"), JsonProperty("submitted")]
		public string SubmittedWhen { get; set; }

		/// <summary>
		/// Revokation reason
		/// </summary>
		[XmlElement("revokationReason"), JsonProperty("revokationReason")]
		public RevokeReason XmlRevokeReason { get; set; }

		/// <summary>
		/// Status code
		/// </summary>
		[XmlAttribute("status"), JsonProperty("status")]
		public SubmissionStatus XmlStatusCode { get; set; }

        /// <summary>
        /// Create new submission info from attributes
        /// </summary>
        public static SubmissionInfo FromAttributes(List<KeyValuePair<String, String>> certAttrs)
        {
            var retVal = new SubmissionInfo();
            foreach (var kv in certAttrs)
            {
                var key = kv.Key.Replace("Request.", "");
                var pi = typeof(SubmissionInfo).GetRuntimeProperty(key);
                pi?.SetValue(retVal, kv.Value, null);
            }
            return retVal;
        }
    }
}