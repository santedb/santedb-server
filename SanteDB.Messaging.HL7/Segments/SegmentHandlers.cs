using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
