﻿/*
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
using System;
using System.Runtime.Serialization;

namespace SanteDB.Reporting.Core.Exceptions
{
    /// <summary>
    /// Thrown when a certificate is not found.
    /// </summary>
    /// <seealso cref="Exception" />
    public class CertificateNotFoundException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateNotFoundException"/> class.
		/// </summary>
		public CertificateNotFoundException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateNotFoundException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public CertificateNotFoundException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateNotFoundException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public CertificateNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CertificateNotFoundException"/> class.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected CertificateNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
