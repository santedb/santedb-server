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
using SanteDB.Core.Mail;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Collections
{

    
	/// <summary>
	/// Represents an administrative collection item.
	/// </summary>
	[XmlType(nameof(AmiCollection), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AmiCollection))]
    [XmlInclude(typeof(Entity))]
    [XmlInclude(typeof(ExtensionType))]
    [XmlInclude(typeof(MailMessage))]
    [XmlInclude(typeof(TfaRequestInfo))]
    [XmlInclude(typeof(SecurityApplicationInfo))]
    [XmlInclude(typeof(SecurityDeviceInfo))]
    [XmlInclude(typeof(SecurityPolicyInfo))]
    [XmlInclude(typeof(SecurityRoleInfo))]
    [XmlInclude(typeof(SecurityUser))]
    [XmlInclude(typeof(SecurityRole))]
    [XmlInclude(typeof(SecurityDevice))]
    [XmlInclude(typeof(SecurityApplication))]
    [XmlInclude(typeof(SecurityUserInfo))]
    [XmlInclude(typeof(AuditSubmission))]
    [XmlInclude(typeof(AppletManifest))]
    [XmlInclude(typeof(AppletManifestInfo))]
    [XmlInclude(typeof(DeviceEntity))]
    [XmlInclude(typeof(DiagnosticApplicationInfo))]
    [XmlInclude(typeof(DiagnosticAttachmentInfo))]
    [XmlInclude(typeof(DiagnosticBinaryAttachment))]
    [XmlInclude(typeof(DiagnosticTextAttachment))]
    [XmlInclude(typeof(DiagnosticEnvironmentInfo))]
    [XmlInclude(typeof(DiagnosticReport))]
    [XmlInclude(typeof(DiagnosticSyncInfo))]
    [XmlInclude(typeof(DiagnosticVersionInfo))]
    [XmlInclude(typeof(SubmissionInfo))]
    [XmlInclude(typeof(SubmissionResult))]
    [XmlInclude(typeof(ApplicationEntity))]
    [XmlInclude(typeof(SubmissionRequest))]
    [XmlInclude(typeof(ServiceOptions))]
    [XmlInclude(typeof(X509Certificate2Info))]
    [XmlInclude(typeof(CodeSystem))]
    [XmlInclude(typeof(LogFileInfo))]
    public class AmiCollection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AmiCollection"/> class.
		/// </summary>
		public AmiCollection()
		{
			this.CollectionItem = new List<Object>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AmiCollection"/> class
		/// with a specific list of collection items.
		/// </summary>
		public AmiCollection(List<Object> collectionItems)
		{
			this.CollectionItem = collectionItems;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AmiCollection"/> class
		/// with a specific list of collection items.
		/// </summary>
		public AmiCollection(IEnumerable<Object> collectionItems, int offset, int totalCount)
		{
			this.CollectionItem = new List<Object>(collectionItems);
            this.Offset = offset;
            this.Size = totalCount;
		}

		/// <summary>
		/// Gets or sets a list of collection items.
		/// </summary>
		[XmlElement("item"), JsonProperty("item")]
		public List<Object> CollectionItem { get; set; }

		/// <summary>
		/// Gets or sets the total offset.
		/// </summary>
		[XmlAttribute("offset"), JsonProperty("offset")]
		public int Offset { get; set; }

		/// <summary>
		/// Gets or sets the total collection size.
		/// </summary>
		[XmlAttribute("size"), JsonProperty("size")]
		public int Size { get; set; }
    }
}