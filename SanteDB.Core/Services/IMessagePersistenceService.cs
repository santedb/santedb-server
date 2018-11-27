using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services
{
    /// <summary>
    /// Identifies the status of a message
    /// </summary>
    public enum MessageState
    {
        /// <summary>
        /// The message has never been received by the system
        /// </summary>
        New,
        /// <summary>
        /// The message has been received by the system and is in process
        /// </summary>
        Active,
        /// <summary>
        /// The message has been received by the system and processing is complete
        /// </summary>
        Complete
    }

    /// <summary>
    /// Message information
    /// </summary>
    public class MessageInfo
    {
        /// <summary>
        /// Gets the id of the message
        /// </summary>
        public String Id { get; set; }
        /// <summary>
        /// Gets the message id that this message responds to or the response of this message.
        /// </summary>
        public String Response { get; set; }
        /// <summary>
        /// Gets the remote endpoint of the message
        /// </summary>
        public Uri Source { get; set; }
        /// <summary>
        /// Gets the local endpoint of the message
        /// </summary>
        public Uri Destination { get; set; }
        /// <summary>
        /// Gets the time the message was received
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets the body of the message
        /// </summary>
        public byte[] Body { get; set; }
        /// <summary>
        /// Gets or sets the state of the message
        /// </summary>
        public MessageState State { get; set; }

    }

    /// <summary>
    /// Identifies a structure for message persistence service implementations
    /// </summary>
    public interface IMessagePersistenceService : IServiceImplementation
    {

        /// <summary>
        /// Get the state of a message
        /// </summary>
        MessageState GetMessageState(string messageId);

        /// <summary>
        /// Persists the message 
        /// </summary>
        void PersistMessage(string messageId, Stream message);

        /// <summary>
        /// Persist message extension
        /// </summary>
        void PersistMessageInfo(MessageInfo message);

        /// <summary>
        /// Get the identifier of the message that represents the response to the current message
        /// </summary>
        Stream GetMessageResponseMessage(string messageId);

        /// <summary>
        /// Get a message
        /// </summary>
        /// <param name="messageId">Body</param>
        /// <returns></returns>
        Stream GetMessage(string messageId);

        /// <summary>
        /// Persist
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="response"></param>
        void PersistResultMessage(string messageId, string respondsToId, Stream response);

        /// <summary>
        /// Get all message ids between the specified time(s)
        /// </summary>
        IEnumerable<String> GetMessageIds(DateTime from, DateTime to);

        /// <summary>
        /// Get message extended attribute
        /// </summary>
        MessageInfo GetMessageInfo(String messageId);

    }
}
