/*
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
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Linq;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Represents a message handler that handles ADT messages
    /// </summary> 
    public class AdtMessageHandler : MessageHandlerBase
    {

        /// <summary>
        /// Supported triggers
        /// </summary>
        public override string[] SupportedTriggers => new string[] { "ADT^A01", "ADT^A04", "ADT^A08", "ADT^A40" };

        /// <summary>
        /// Handle the message internally
        /// </summary>
        /// <param name="e">The message receive events</param>
        /// <param name="parsed">The parsed message</param>
        /// <returns>The response to the ADT message</returns>
        protected override IMessage HandleMessageInternal(Hl7MessageReceivedEventArgs e, Bundle parsed)
        {

            var msh = e.Message.GetStructure("MSH") as MSH;
            switch (msh.MessageType.TriggerEvent.Value)
            {
                case "A01": // Admit
                case "A04": // Register
                    return this.PerformAdmit(e, parsed); // parsed.Item.OfType<Patient>().SingleOrDefault(o=>o.Tags.Any(t=>t.TagKey == ".v2.segment" && t.Value == "PID")));
                case "A08": // Update
                    return this.PerformUpdate(e, parsed);
                case "A40": // Merge
                    return this.PerformMerge(e, parsed);
                default:
                    throw new InvalidOperationException($"Do not understand event {msh.MessageType.TriggerEvent.Value}");
            }
        }

        /// <summary>
        /// Perform an admission operation
        /// </summary>
        protected virtual IMessage PerformAdmit(Hl7MessageReceivedEventArgs e, Bundle insertBundle)
        {
            try
            {
                var patient = insertBundle.Item.OfType<Patient>().FirstOrDefault(it => it.Tags.Any(t => t.TagKey == ".v2.segment" && t.Value == "PID"));
                if (patient == null)
                    throw new ArgumentNullException(nameof(insertBundle), "Message did not contain a patient");

                var repoService = ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>();
                if (repoService == null)
                    throw new InvalidOperationException("Cannot find repository for Patient");

                insertBundle = repoService.Insert(insertBundle);

                AuditUtil.AuditCreate(Core.Auditing.OutcomeIndicator.Success, null, insertBundle.Item.ToArray());
                // Create response message
                return this.CreateACK(typeof(ACK), e.Message, "CA", $"{patient.Key} created");
            }
            catch
            {
                AuditUtil.AuditUpdate(Core.Auditing.OutcomeIndicator.MinorFail, null, insertBundle.Item.ToArray());
                throw;
            }
        }

        /// <summary>
        /// Perform an update of the specified patient
        /// </summary>
        protected virtual IMessage PerformUpdate(Hl7MessageReceivedEventArgs e, Bundle updateBundle)
        {
            try
            {
                var patient = updateBundle.Item.OfType<Patient>().FirstOrDefault(it => it.Tags.Any(t => t.TagKey == ".v2.segment" && t.Value == "PID"));
                if (patient == null)
                    throw new ArgumentNullException(nameof(updateBundle), "Message did not contain a patient");
                else if (!patient.Key.HasValue)
                    throw new InvalidOperationException("Update can only be performed on existing patients. Ensure that a unique identifier exists on the update record");
                var repoService = ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>();
                if (repoService == null)
                    throw new InvalidOperationException("Cannot find repository for Patient");

                updateBundle = repoService.Save(updateBundle);
                AuditUtil.AuditUpdate(Core.Auditing.OutcomeIndicator.Success, null, updateBundle.Item.ToArray());

                // Create response message
                return this.CreateACK(typeof(ACK), e.Message, "CA", $"{patient.Key} updated");
            }
            catch
            {
                AuditUtil.AuditUpdate(Core.Auditing.OutcomeIndicator.MinorFail, null, updateBundle.Item.ToArray());
                throw;
            }

        }

        /// <summary>
        /// Performs a merge of the specified patient
        /// </summary>
        protected virtual IMessage PerformMerge(Hl7MessageReceivedEventArgs e, Bundle b)
        {
            return null;
        }

        /// <summary>
        /// Validate the incoming message
        /// </summary>
        /// <param name="message">The message to be validated</param>
        /// <returns>The validated message</returns>
        protected override bool Validate(IMessage message)
        {
            return true;
        }
    }
}
