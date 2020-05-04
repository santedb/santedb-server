using NHapi.Base.Model;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Represents the current operation context for the rest service thread
    /// </summary>
    public sealed class HL7OperationContext 
    {
        // Current reference for thread
        [ThreadStatic]
        private static HL7OperationContext m_current;

        // Gets the event information
        private Hl7MessageReceivedEventArgs m_eventInfo;

        /// <summary>
        /// Creates a new operation context
        /// </summary>
        internal HL7OperationContext(Hl7MessageReceivedEventArgs eventInfo)
        {
            this.m_eventInfo = eventInfo;
            this.Uuid = Guid.NewGuid();
        }

        /// <summary>
        /// Incoming request
        /// </summary>
        public IMessage IncomingRequest => this.m_eventInfo.Message;

        /// <summary>
        /// Outgoing resposne
        /// </summary>
        public IMessage OutgoingResponse => this.m_eventInfo.Response;

        /// <summary>
        /// Remote endpoint
        /// </summary>
        public Uri RemoteEndpoint => this.m_eventInfo.SolicitorEndpoint;

        /// <summary>
        /// Local endpoint
        /// </summary>
        public Uri LocalEndpoint => this.m_eventInfo.ReceiveEndpoint;

        /// <summary>
        /// Gets the current operation context
        /// </summary>
        public static HL7OperationContext Current
        {
            get { return m_current; }
            internal set { m_current = value; }
        }

        /// <summary>
        /// Gets the uuid of this request
        /// </summary>
        public Guid Uuid { get; }
    }
}
