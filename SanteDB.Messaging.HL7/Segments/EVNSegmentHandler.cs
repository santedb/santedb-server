using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents the clinical event
    /// </summary>
    public class EVNSegmentHandler : ISegmentHandler
    {

        private const string EventReasonCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.62";

        /// <summary>
        /// Segment this handles
        /// </summary>
        public string Name => "EVN";

        public IEnumerable<ISegment> Create(IdentifiedData data, IMessage context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the segment data
        /// </summary>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            var evnSegment = segment as EVN;

            // Now load event segments
            var retVal = new ControlAct() { Key = Guid.Empty };

            // Recorded event time
            if (!evnSegment.RecordedDateTime.IsEmpty())
                retVal.CreationTime = (DateTimeOffset)evnSegment.RecordedDateTime.ToModel();

            // Planned event time
            if(!evnSegment.DateTimePlannedEvent.IsEmpty())
            {
                retVal.ActTime = (DateTimeOffset)evnSegment.RecordedDateTime.ToModel();
                retVal.MoodConceptKey = ActMoodKeys.Intent;
            }

            // Reason code
            if(!evnSegment.EventReasonCode.IsEmpty())
                retVal.ReasonConcept = evnSegment.EventReasonCode.ToConcept(EventReasonCodeSystem);

            // Operator ID - I.E. the author of this event - XCN to be extracted and created as author
            if (evnSegment.OperatorIDRepetitionsUsed > 0)
                foreach (var op in evnSegment.GetOperatorID())
                    ;

            if (!evnSegment.EventOccurred.IsEmpty()) {
                retVal.ActTime = (DateTimeOffset)evnSegment.EventOccurred.ToModel();
                retVal.MoodConceptKey = ActMoodKeys.Eventoccurrence;
            }

            // Facility ID  - TODO: Fetch facility identifier
            if (!evnSegment.EventFacility.IsEmpty())
                ;

            return new IdentifiedData[] { retVal };
        }
    }
}
