using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Represents a message handler that handles ADT messages
    /// </summary> 
    public class AdtMessageHandler : MessageHandlerBase
    {
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
            var patient = insertBundle.Item.OfType<Patient>().FirstOrDefault(it => it.Tags.Any(t => t.TagKey == ".v2.segment" && t.Value == "PID"));
            if (patient == null)
                throw new ArgumentNullException(nameof(insertBundle), "Message did not contain a patient");

            var repoService = ApplicationContext.Current.GetService<IBatchRepositoryService>();
            if (repoService == null)
                throw new InvalidOperationException("Cannot find repository for Patient");

            insertBundle = repoService.Insert(insertBundle);

            // Create response message
            return this.CreateACK(typeof(ACK), e.Message, "CA", $"{patient.Key} created");
        }

        /// <summary>
        /// Perform an update of the specified patient
        /// </summary>
        protected virtual IMessage PerformUpdate(Hl7MessageReceivedEventArgs e, Bundle updateBundle)
        {
            var patient = updateBundle.Item.OfType<Patient>().FirstOrDefault(it => it.Tags.Any(t => t.TagKey == ".v2.segment" && t.Value == "PID"));
            if (patient == null)
                throw new ArgumentNullException(nameof(updateBundle), "Message did not contain a patient");

            var repoService = ApplicationContext.Current.GetService<IBatchRepositoryService>();
            if (repoService == null)
                throw new InvalidOperationException("Cannot find repository for Patient");

            updateBundle = repoService.Update(updateBundle);

            // Create response message
            return this.CreateACK(typeof(ACK), e.Message, "CA", $"{patient.Key} updated");

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
