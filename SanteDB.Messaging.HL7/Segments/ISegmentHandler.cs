using NHapi.Base.Model;
using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a class that can handle an individual segment
    /// </summary>
    public interface ISegmentHandler
    {

        /// <summary>
        /// Gets the name of the segment this handles
        /// </summary>
        String Name { get; }

        /// <summary>
        /// Parse the segment into a model object
        /// </summary>
        /// <param name="segment">The segment to be parsed</param>
        /// <returns>The parsed data</returns>
        IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context);

        /// <summary>
        /// Create necesary segment(s) from the specified data
        /// </summary>
        /// <param name="data">The data to be translated into segment(s)</param>
        /// <param name="context">The message in which the segment is created</param>
        /// <returns>The necessary segments to be added to the message</returns>
        IEnumerable<ISegment> Create(IdentifiedData data, IGroup context);

    }
}
