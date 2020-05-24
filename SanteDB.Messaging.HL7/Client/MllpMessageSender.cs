/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using NHapi.Base.Model;
using NHapi.Base.Parser;
using SanteDB.Core.Diagnostics;
using SanteDB.Messaging.HL7.Utils;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SanteDB.Messaging.HL7.Client
{
	/// <summary>
	/// Represents an MLLP message sender.
	/// </summary>
	public class MllpMessageSender
	{
		/// <summary>
		/// The internal reference to the client certificate.
		/// </summary>
		private readonly X509Certificate2 clientCertificate;

		/// <summary>
		/// The internal reference to the endpoint.
		/// </summary>
		private readonly Uri endpoint;

		/// <summary>
		/// The internal reference to the server certificate chain.
		/// </summary>
		private readonly X509Certificate2 serverCertificateChain;

		/// <summary>
		/// The internal reference to the <see cref="TraceSource"/> instance.
		/// </summary>
		private readonly Tracer tracer = new Tracer(Hl7Constants.TraceSourceName);

		/// <summary>
		/// Initializes a new instance of the <see cref="MllpMessageSender"/> class
		/// with a specific endpoint, client certificate, and server certificate chain.
		/// </summary>
		/// <param name="endpoint">The endpoint address.</param>
		/// <param name="clientCertificate">The client certificate.</param>
		/// <param name="serverCertificateChain">The server certificate chain.</param>
		public MllpMessageSender(Uri endpoint, X509Certificate2 clientCertificate, X509Certificate2 serverCertificateChain)
		{
			this.endpoint = endpoint;
			this.clientCertificate = clientCertificate;
			this.serverCertificateChain = serverCertificateChain;
		}

		/// <summary>
		/// Performs remote certificate validation.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="certificate">The certificate.</param>
		/// <param name="chain">The chain.</param>
		/// <param name="sslPolicyErrors">The SSL policy errors.</param>
		/// <returns><c>true</c> if the certificate chain is valid, <c>false</c> otherwise.</returns>
		private bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
#if DEBUG
			if (certificate != null)
			{
				this.tracer.TraceEvent(EventLevel.Informational,  "Received client certificate with subject {0}", certificate.Subject);
			}
			if (chain != null)
			{
				this.tracer.TraceEvent(EventLevel.Informational,  "Client certificate is chained with {0}", chain.ChainElements.Count);

				foreach (var chainElement in chain.ChainElements)
				{
					this.tracer.TraceEvent(EventLevel.Informational,  "\tChain Element : {0}", chainElement.Certificate.Subject);
				}
			}

			if (sslPolicyErrors != SslPolicyErrors.None)
			{
				this.tracer.TraceEvent(EventLevel.Error,  "SSL Policy Error : {0}", sslPolicyErrors);
			}
#endif

			// First Validate the chain

			if (certificate == null || chain == null)
			{
				return this.serverCertificateChain == null;
			}
			else
			{
				var isValid = false;

				foreach (var cer in chain.ChainElements.Cast<X509ChainElement>().Where(cer => cer.Certificate.Thumbprint == this.serverCertificateChain.Thumbprint))
				{
					isValid = true;
				}

				if (!isValid)
				{
					this.tracer.TraceEvent(EventLevel.Error,  "Certification authority from the supplied certificate doesn't match the expected thumbprint of the CA");
				}

				foreach (var stat in chain.ChainStatus)
				{
					this.tracer.TraceEvent(EventLevel.Warning, "Certificate chain validation error: {0}", stat.StatusInformation);
				}

				return isValid;
			}
		}

		/// <summary>
		/// Send a message and receive the message
		/// </summary>
		public IMessage SendAndReceive(IMessage message)
		{
			// Encode the message
			var strMessage = MessageUtils.EncodeMessage(message, (message.GetStructure("MSH") as NHapi.Model.V25.Segment.MSH).VersionID.VersionID.Value);

#if DEBUG
			this.tracer.TraceEvent(EventLevel.Informational,  strMessage);
#endif

			// Open a TCP port
			using (var client = new TcpClient(AddressFamily.InterNetwork))
			{
				try
				{
					// Connect on the socket
					client.Connect(this.endpoint.Host, this.endpoint.Port);

					// Get the stream
					using (var stream = client.GetStream())
					{
						Stream realStream = stream;

						if (this.clientCertificate != null)
						{
							realStream = new SslStream(stream, false, this.RemoteCertificateValidation);

							var collection = new X509CertificateCollection
							{
								this.clientCertificate
							};

							((SslStream)realStream).AuthenticateAsClient(this.endpoint.ToString(), collection, System.Security.Authentication.SslProtocols.Tls, true);
						}

						// Write message in ASCII encoding
						byte[] buffer = Encoding.UTF8.GetBytes(strMessage);
						byte[] sendBuffer = new byte[buffer.Length + 3];

						sendBuffer[0] = 0x0b;

						Array.Copy(buffer, 0, sendBuffer, 1, buffer.Length);
						Array.Copy(new byte[] { 0x1c, 0x0d }, 0, sendBuffer, sendBuffer.Length - 2, 2);

						stream.Write(sendBuffer, 0, sendBuffer.Length);

						// Write end message
						stream.Flush(); // Ensure all bytes get sent down the wire

						// Now read the response
						StringBuilder response = new StringBuilder();

						buffer = new byte[1024];

						while (!buffer.Contains((byte)0x1c)) // HACK: Keep reading until the buffer has the FS character
						{
							int br = stream.Read(buffer, 0, 1024);

							int ofs = 0;

							if (buffer[ofs] == '\v')
							{
								ofs = 1;
								br = br - 1;
							}

							response.Append(Encoding.UTF8.GetString(buffer, ofs, br));
						}

#if DEBUG
						this.tracer.TraceEvent(EventLevel.Informational,  response.ToString());
#endif
                        String version = null;
						return MessageUtils.ParseMessage(response.ToString(), out version);
					}
				}
				catch (Exception e)
				{
#if DEBUG
					this.tracer.TraceEvent(EventLevel.Error,  e.StackTrace);
#endif
					this.tracer.TraceEvent(EventLevel.Error,  e.Message);

					throw;
				}
			}
		}
	}
}