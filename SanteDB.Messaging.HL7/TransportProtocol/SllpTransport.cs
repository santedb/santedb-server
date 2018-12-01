/*
 * Copyright 2012-2013 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 18-9-2012
 */

using SanteDB.Core;
using SanteDB.Core.Auditing;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.TransportProtocol
{
    /// <summary>
    /// Secure LLP transport
    /// </summary>
    [Description("ER7 over Secure LLP")]
	public class SllpTransport : LlpTransport
	{
		/// <summary>
		/// SLLP configuration object
		/// </summary>
        [XmlType(nameof(SllpConfigurationObject), Namespace = "http://santedb.org/configuration")]
		public class SllpConfigurationObject
		{
          
            /// <summary>
            /// Check CRL
            /// </summary>
            public SllpConfigurationObject()
			{
				this.CheckCrl = true;
			}

            /// <summary>
            /// Gets the server certificate
            /// </summary>
            [XmlElement("serverCertificate")]
            public Hl7X509ConfigurationElement ServerCertificate { get; set; }

            /// <summary>
            /// Gets the server certificate
            /// </summary>
            [XmlElement("clientAuthorityCertificate")]
            public Hl7X509ConfigurationElement ClientCaCertificate { get; set; }

            /// <summary>
            /// Check revocation status
            /// </summary>
            [XmlAttribute("checkCrl")]
			public bool CheckCrl { get; set; }

			/// <summary>
			/// Enabling of the client cert negotiate
			/// </summary>
			[Description("When enabled, enforces client certificate negotiation")]
            [XmlAttribute("requireClientCert")]
			public bool EnableClientCertNegotiation { get; set; }
		}

		// SLLP configuration object
		private SllpConfigurationObject m_configuration = new SllpConfigurationObject();

		/// <summary>
		/// Protocol name
		/// </summary>
		public override string ProtocolName
		{
			get
			{
				return "sllp";
			}
		}

		/// <summary>
		/// Start the transport
		/// </summary>
		public override void Start(IPEndPoint bind, ServiceHandler handler)
		{
			this.m_timeout = new TimeSpan(0,0,0,0, handler.Definition.ReceiveTimeout);
			this.m_listener = new TcpListener(bind);
			this.m_listener.Start();
			this.m_traceSource.TraceInformation("SLLP Transport bound to {0}", bind);

            // Setup certificate
            this.m_configuration = handler.Definition.Configuration as SllpConfigurationObject;
			if (this.m_configuration.ServerCertificate == null)
				throw new InvalidOperationException("Cannot start the secure LLP listener without a server certificate");

			while (m_run) // run the service
			{
				var client = this.m_listener.AcceptTcpClient();
				Thread clientThread = new Thread(OnReceiveMessage);
				clientThread.IsBackground = true;
				clientThread.Start(client);
			}
		}

		/// <summary>
		/// Validation for certificates
		/// </summary>
		private bool RemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			// First Validate the chain
#if DEBUG
			if (certificate != null)
				this.m_traceSource.TraceInformation("Received client certificate with subject {0}", certificate.Subject);
			if (chain != null)
			{
				this.m_traceSource.TraceInformation("Client certificate is chained with {0}", chain.ChainElements.Count);

				foreach (var el in chain.ChainElements)
					this.m_traceSource.TraceInformation("\tChain Element : {0}", el.Certificate.Subject);
			}
			else
			{
				this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Warning, 0, "Didn't get a chain, so I'm making my own");
				chain = new X509Chain(true);
				X509Certificate2 cert2 = new X509Certificate2(certificate.GetRawCertData());
				chain.Build(cert2);
			}
			if (sslPolicyErrors != SslPolicyErrors.None)
			{
				this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, 0, "SSL Policy Error : {0}", sslPolicyErrors);
			}

#endif

			if (certificate == null || chain == null)
				return !this.m_configuration.EnableClientCertNegotiation;
			else
			{
				bool isValid = false;
				foreach (var cer in chain.ChainElements)
				{
					if (cer.Certificate.Thumbprint == this.m_configuration.ClientCaCertificate.GetCertificate().Thumbprint)
						isValid = true;
				}
				if (!isValid)
					this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, 0, "Certification authority from the supplied certificate doesn't match the expected thumbprint of the CA");
				foreach (var stat in chain.ChainStatus)
					this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Warning, 0, "Certificate chain validation error: {0}", stat.StatusInformation);
				//isValid &= chain.ChainStatus.Length == 0;
				return isValid;
			}
		}

		/// <summary>
		/// Receive a message
		/// </summary>
		protected override void OnReceiveMessage(object client)
		{
			TcpClient tcpClient = client as TcpClient;
			SslStream stream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(RemoteCertificateValidation));
			try
			{
				// Setup local and remote receive endpoint data for auditing
				var localEp = tcpClient.Client.LocalEndPoint as IPEndPoint;
				var remoteEp = tcpClient.Client.RemoteEndPoint as IPEndPoint;
				Uri localEndpoint = new Uri(String.Format("sllp://{0}:{1}", localEp.Address, localEp.Port));
				Uri remoteEndpoint = new Uri(String.Format("sllp://{0}:{1}", remoteEp.Address, remoteEp.Port));
				this.m_traceSource.TraceInformation("Accepted TCP connection from {0} > {1}", remoteEndpoint, localEndpoint);

				stream.AuthenticateAsServer(this.m_configuration.ServerCertificate.GetCertificate(), this.m_configuration.EnableClientCertNegotiation, System.Security.Authentication.SslProtocols.Tls, this.m_configuration.CheckCrl);

				// Now read to a string
				NHapi.Base.Parser.PipeParser parser = new NHapi.Base.Parser.PipeParser();

				DateTime lastReceive = DateTime.Now;

				while (DateTime.Now.Subtract(lastReceive) < this.m_timeout)
				{
					int llpByte = 0;
					// Read LLP head byte
					try
					{
						llpByte = stream.ReadByte();
					}
					catch (SocketException)
					{
						break;
					}

					if (llpByte != START_TX) // first byte must be HT
					{
						this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Warning, 0, "Invalid LLP First Byte expected 0x{0:x} got 0x{1:x} from {2}", START_TX, llpByte, remoteEndpoint);
						break;
					}
					//                        throw new InvalidOperationException("Invalid LLP First Byte");

					// Standard stream stuff, read until the stream is exhausted
					StringBuilder messageData = new StringBuilder();
					byte[] buffer = new byte[1024];
					bool receivedEOF = false, scanForCr = false;

					while (!receivedEOF)
					{
						int br = stream.Read(buffer, 0, 1024);
						messageData.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, br));

						// Need to check for CR?
						if (scanForCr)
							receivedEOF = buffer[0] == END_TXNL;
						else
						{
							// Look for FS
							int fsPos = Array.IndexOf(buffer, END_TX);

							if (fsPos == -1) // not found
								continue;
							else if (fsPos < buffer.Length - 1) // more room to read
								receivedEOF = buffer[fsPos + 1] == END_TXNL;
							else
								scanForCr = true; // Cannot check the end of message for CR because there is no more room in the message buffer
												  // so need to check on the next loop
						}

						// TODO: Timeout for this
					}

					// Use the nHAPI parser to process the data
					Hl7MessageReceivedEventArgs messageArgs = null;
					try
					{
						var message = parser.Parse(messageData.ToString());

#if DEBUG
						this.m_traceSource.TraceInformation("Received message from sllp://{0} : {1}", tcpClient.Client.RemoteEndPoint, messageData.ToString());
#endif

						messageArgs = new AuthenticatedHl7MessageReceivedEventArgs(message, localEndpoint, remoteEndpoint, DateTime.Now, stream.RemoteCertificate.GetPublicKey());

						// Call any bound event handlers that there is a message available
						OnMessageReceived(messageArgs);
					}
					finally
					{
						// Send the response back
						using (MemoryStream memoryWriter = new MemoryStream())
						{
							using (StreamWriter streamWriter = new StreamWriter(memoryWriter))
							{
								memoryWriter.Write(new byte[] { START_TX }, 0, 1); // header
								if (messageArgs != null && messageArgs.Response != null)
								{
									var strMessage = parser.Encode(messageArgs.Response);
#if DEBUG
									this.m_traceSource.TraceInformation("Sending message to sllp://{0} : {1}", tcpClient.Client.RemoteEndPoint, strMessage);
#endif
									// Since nHAPI only emits a string we just send that along the stream
									streamWriter.Write(strMessage);
									streamWriter.Flush();
								}
								memoryWriter.Write(new byte[] { END_TX, END_TXNL }, 0, 2); // Finish the stream with FSCR
								stream.Write(memoryWriter.ToArray(), 0, (int)memoryWriter.Position);
								stream.Flush();
							}
						}

						lastReceive = DateTime.Now; // Update the last receive time so the timeout function works
					}
				}
			}
			catch (AuthenticationException e)
			{
				// Trace authentication error
				AuditData ad = new AuditData(
					DateTime.Now,
					ActionType.Execute,
					OutcomeIndicator.MinorFail,
					EventIdentifierType.ApplicationActivity,
					new AuditCode("110113", "DCM") { DisplayName = "Security Alert" }
				);
				ad.Actors = new List<AuditActorData>() {
					new AuditActorData()
					{
						NetworkAccessPointId = Dns.GetHostName(),
						NetworkAccessPointType = NetworkAccessPointType.MachineName,
						UserName = Environment.UserName,
						UserIsRequestor = false
					},
					new AuditActorData()
					{
						NetworkAccessPointId = String.Format("sllp://{0}", tcpClient.Client.RemoteEndPoint.ToString()),
						NetworkAccessPointType = NetworkAccessPointType.MachineName,
						UserIsRequestor = true
					}
				};
				ad.AuditableObjects = new List<AuditableObject>()
				{
					new AuditableObject() {
						Type = AuditableObjectType.SystemObject,
						Role = AuditableObjectRole.SecurityResource,
						IDTypeCode = AuditableObjectIdType.Uri,
						ObjectId = String.Format("sllp://{0}", this.m_listener.LocalEndpoint)
					}
				};

                ApplicationServiceContext.Current.GetService<IAuditRepositoryService>()?.Insert(ad);
                ApplicationServiceContext.Current.GetService<IAuditDispatchService>()?.SendAudit(ad);
				this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, e.HResult, e.ToString());
			}
			catch (Exception e)
			{
				this.m_traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, e.HResult, e.ToString());
				// TODO: NACK
			}
			finally
			{
				stream.Close();
				tcpClient.Close();
			}
		}
        
	}
}