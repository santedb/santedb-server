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

using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
	/// <summary>
	/// The reason something is revoked
	/// </summary>
	[XmlType(nameof(RevokeReason), Namespace = "http://santedb.org/ami")]
	public enum RevokeReason : uint
	{
        /// <summary>
        /// No reason given
        /// </summary>
		[XmlEnum("UNSPECIFIED")]
		Unspecified = 0x0,
        /// <summary>
        /// The key has been compromised
        /// </summary>
		[XmlEnum("KEY COMPROMISED")]
		KeyCompromise = 0x1,
        /// <summary>
        /// The CA itself has been compromised
        /// </summary>
		[XmlEnum("CA COMPROMISED")]
		CaCompromise = 0x2,
        /// <summary>
        /// The affiliation of the CA has changed
        /// </summary>
		[XmlEnum("AFFILIATION CHANGED")]
		AffiliationChange = 0x3,
        /// <summary>
        /// There has been a new certificate issued
        /// </summary>
		[XmlEnum("SUPERSEDED")]
		Superseded = 0x4,
        /// <summary>
        /// The business has ceased operation
        /// </summary>
		[XmlEnum("CESSATION OF OPERATION")]
		CessationOfOperation = 0x5,
        /// <summary>
        /// The certificate is under investigation or is held by administrator
        /// </summary>
		[XmlEnum("CERTIFICATE ON HOLD")]
		CertificateHold = 0x6,
        /// <summary>
        /// Reinstantiate the certificate
        /// </summary>
		[XmlEnum("REINSTANTIATE")]
		Reinstate = 0xFFFFFFFF
	}
}