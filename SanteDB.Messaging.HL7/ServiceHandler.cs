/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.TransportProtocol;
using NHapi.Base.Util;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Diagnostics;

namespace SanteDB.Messaging.HL7
{
	/// <summary>
	/// Service handler which is responsible for the actual receiving of messages
	/// </summary>
	public class ServiceHandler
	{
		// The service definition
		private Hl7ServiceDefinition m_serviceDefinition;

        private Tracer m_traceSource = new Tracer(Hl7Constants.TraceSourceName);

        // Transport
        private ITransportProtocol m_transport;

		/// <summary>
		/// Constructs the new service handler
		/// </summary>
		public ServiceHandler(Hl7ServiceDefinition serviceDefinition)
		{
			this.m_serviceDefinition = serviceDefinition;
			this.m_transport = TransportUtil.CreateTransport(this.m_serviceDefinition.Address.Scheme);
			this.m_transport.MessageReceived += new EventHandler<Hl7MessageReceivedEventArgs>(m_transport_MessageReceived);
		}

		/// <summary>
		/// Gets the service definition for this handler
		/// </summary>
		public Hl7ServiceDefinition Definition { get { return this.m_serviceDefinition; } }

		/// <summary>
		/// Start the service handler
		/// </summary>
		public void Run()
		{
			IPAddress address = null;
			int port = this.m_serviceDefinition.Address.Port;
			if (this.m_serviceDefinition.Address.HostNameType == UriHostNameType.Dns)
				address = Dns.GetHostAddresses(this.m_serviceDefinition.Address.Host)[0];
			else
				address = IPAddress.Parse(this.m_serviceDefinition.Address.Host);
			if (this.m_serviceDefinition.Address.IsDefaultPort)
				port = 1025;

			try
			{
				this.m_transport.Start(new IPEndPoint(address, port), this);
			}
			catch (ThreadAbortException)
			{
				this.m_transport.Stop();
			}
		}

        /// <summary>
        /// Abort the operation
        /// </summary>
        internal void Abort()
        {
            this.m_transport.Stop();

        }

        /// <summary>
        /// Create a message stream
        /// </summary>
        private System.IO.Stream CreateMessageStream(NHapi.Base.Model.IMessage msg)
		{
			NHapi.Base.Parser.PipeParser pp = new NHapi.Base.Parser.PipeParser();
			return new MemoryStream(Encoding.ASCII.GetBytes(pp.Encode(msg)));
		}

		/// <summary>
		/// Transport has received a message!
		/// </summary>
		private void m_transport_MessageReceived(object sender, Hl7MessageReceivedEventArgs e)
		{
			IMessagePersistenceService messagePersister = ApplicationServiceContext.Current.GetService(typeof(IMessagePersistenceService)) as IMessagePersistenceService;

			// Find the message that supports the type
			Terser msgTerser = new Terser(e.Message);
			string messageType = String.Format("{0}^{1}", msgTerser.Get("/MSH-9-1"), msgTerser.Get("/MSH-9-2"));
			string messageId = msgTerser.Get("/MSH-10");

			// Find a handler
			HandlerDefinition handler = m_serviceDefinition.Handlers.Find(o => o.Types.Exists(a => a.Name == messageType)),
				defaultHandler = m_serviceDefinition.Handlers.Find(o => o.Types.Exists(a => a.Name == "*"));

			if (handler != null && handler.Types.Find(o => o.Name == messageType).IsQuery ||
				defaultHandler != null && defaultHandler.Types.Find(o => o.Name == "*").IsQuery)
				messagePersister = null;

			// Have we already processed this message?
			MessageState msgState = MessageState.New;
			if (messagePersister != null)
				msgState = messagePersister.GetMessageState(messageId);

			switch (msgState)
			{
				case MessageState.New:

					if (messagePersister != null)
						messagePersister.PersistMessage(messageId, CreateMessageStream(e.Message));

					if (handler == null && defaultHandler == null)
						throw new InvalidOperationException(String.Format("Cannot find message handler for '{0}'", messageType));

					e.Response = (handler ?? defaultHandler).Handler.HandleMessage(e);
					if (e.Response == null)
						throw new InvalidOperationException("Couldn't process message");

					msgTerser = new Terser(e.Response);
					if (messagePersister != null)
						messagePersister.PersistResultMessage(msgTerser.Get("/MSH-10"), messageId, CreateMessageStream(e.Response));
					break;

				case MessageState.Active:
					throw new InvalidOperationException("Message already in progress");
				case MessageState.Complete:
					NHapi.Base.Parser.PipeParser pp = new NHapi.Base.Parser.PipeParser();
					using (var rdr = new StreamReader(messagePersister.GetMessageResponseMessage(messageId)))
						e.Response = pp.Parse(rdr.ReadToEnd());
					break;
			}
		}
	}
}