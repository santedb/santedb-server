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
    public class HL7DatatypeProcessingException : Exception
    {
      
        /// <summary>
        /// Gets the component
        /// </summary>
        public Int32 Component { get; }
        
        /// <summary>
        /// Creates a new HL7 processing exception
        /// </summary>
        public HL7DatatypeProcessingException(String message,  Int32 component) : this(message, component, null)
        {
            this.Component = component;
        }

        /// <summary>
        /// Creates a new HL7 processing exception
        /// </summary>
        public HL7DatatypeProcessingException(String message, Int32 component, Exception cause) : base(message, cause)
        {
            this.Component = component;
        }

    }
}
