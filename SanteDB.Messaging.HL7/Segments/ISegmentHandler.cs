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
using System;
using System.Collections.Generic;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;

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
        IEnumerable<ISegment> Create(IdentifiedData data, IGroup context, string[] exportDomains);

    }
}
