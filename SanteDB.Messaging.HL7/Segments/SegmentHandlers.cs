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
 * Date: 2018-10-14
 */
using System;
using System.Collections.Generic;
using System.Linq;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;

namespace SanteDB.Messaging.HL7.Segments
{
    /// <summary>
    /// Represents a segment handler
    /// </summary>
    public static class SegmentHandlers
    {
        // Segment handlers
        private static Dictionary<String, ISegmentHandler> s_segmentHandlers = new Dictionary<string, ISegmentHandler>();

        /// <summary>
        /// Scan types for message handler
        /// </summary>
        static SegmentHandlers()
        {
            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes.Where(t => typeof(ISegmentHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)))
            {
                var instance = Activator.CreateInstance(t) as ISegmentHandler;
                s_segmentHandlers.Add(instance.Name, instance);
            }
        }

        /// <summary>
        /// Gets the segment handler for the specified segment
        /// </summary>
        public static ISegmentHandler GetSegmentHandler(string name)
        {
            ISegmentHandler handler = null;
            s_segmentHandlers.TryGetValue(name, out handler);
            return handler;
        }

    }
}
