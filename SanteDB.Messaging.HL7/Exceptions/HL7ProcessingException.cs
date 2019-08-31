using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Exceptions
{
    /// <summary>
    /// Assigning authority was not found
    /// </summary>
    public class HL7ProcessingException : Exception
    {

        /// <summary>
        /// Gets the segment
        /// </summary>
        public String Segment { get; }

        /// <summary>
        /// Gets the repetition
        /// </summary>
        public String Repetition { get; }

        /// <summary>
        /// Gets the field
        /// </summary>
        public Int32 Field { get; }

        /// <summary>
        /// Gets the component
        /// </summary>
        public Int32 Component { get; }

        /// <summary>
        /// Creates a new HL7 processing exception
        /// </summary>
        public HL7ProcessingException(String message, String segment, String repetition, Int32 field, Int32 component) : this(message, segment, repetition, field, component, null)
        {
        }

        /// <summary>
        /// Creates a new HL7 processing exception
        /// </summary>
        public HL7ProcessingException(String message, String segment, String repetition, Int32 field, Int32 component, Exception cause) : base(message, cause)
        {
            this.Segment = segment;
            this.Repetition = repetition;
            this.Field = field;
            this.Component = component;
        }

    }
}
