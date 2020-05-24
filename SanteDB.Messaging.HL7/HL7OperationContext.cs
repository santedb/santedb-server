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
 * Date: 2020-5-4
 */
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
