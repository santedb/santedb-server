/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
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
            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a =>
            {
                try
                {
                    return a.ExportedTypes;
                }
                catch
                {
                    return Type.EmptyTypes;
                }
            }).Where(t => typeof(ISegmentHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface))
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
