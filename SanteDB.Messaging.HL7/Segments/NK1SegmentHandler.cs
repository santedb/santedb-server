using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHapi.Base.Model;
using SanteDB.Core.Model;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a NK1 segment
    /// </summary>
    public class NK1SegmentHandler : ISegmentHandler
    {
        /// <summary>
        /// Gets or sets the name of the segment
        /// </summary>
        public string Name => "NK1";

        public IEnumerable<ISegment> Create(IdentifiedData data, IMessage context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the Next Of Kin Data
        /// </summary>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            return new IdentifiedData[0];
        }
    }
}
