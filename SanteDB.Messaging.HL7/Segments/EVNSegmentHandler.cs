/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-9-25
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using System;
using System.Collections.Generic;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents the clinical event
    /// </summary>
    public class EVNSegmentHandler : ISegmentHandler
    {

        private const string EventReasonCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.62";
        private const string EventTriggerCodeSystem = "1.3.6.1.4.1.33349.3.1.5.9.3.200.3";

        /// <summary>
        /// Segment this handles
        /// </summary>
        public string Name => "EVN";

        public IEnumerable<ISegment> Create(IdentifiedData data, IGroup context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parse the segment data
        /// </summary>
        public IEnumerable<IdentifiedData> Parse(ISegment segment, IEnumerable<IdentifiedData> context)
        {
            var evnSegment = segment as EVN;
            var mshData = segment.Message.GetStructure("MSH") as MSH;
            // Now load event segments
            var retVal = new ControlAct() { Key = Guid.NewGuid(), MoodConceptKey = ActMoodKeys.Eventoccurrence, ActTime = DateTimeOffset.Now };

            // Message trigger
            if(!mshData.MessageType.TriggerEvent.IsEmpty())
                retVal.TypeConcept = mshData.MessageType.TriggerEvent.ToConcept(EventTriggerCodeSystem);

            // Recorded event time
            if (!evnSegment.RecordedDateTime.IsEmpty())
            {
                retVal.CreationTime = (DateTimeOffset)evnSegment.RecordedDateTime.ToModel();
                retVal.ActTime = (DateTimeOffset)evnSegment.RecordedDateTime.ToModel();
            }

            // Planned event time
            if (!evnSegment.DateTimePlannedEvent.IsEmpty())
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
