using Hl7.Fhir.Model;
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Messaging.FHIR.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Profile utility which has methods for profile
    /// </summary>
    public static class ProfileUtil
    {

        // Handlers
        private static List<IFhirExtensionHandler> s_handlers;

        /// <summary>
        /// Creates a profile utility
        /// </summary>
        static ProfileUtil ()
        {
            s_handlers = ApplicationServiceContext.Current.GetService<IServiceManager>()
                .GetAllTypes()
                .Where(t => typeof(IFhirExtensionHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => Activator.CreateInstance(t))
                .OfType<IFhirExtensionHandler>()
                .ToList();
        }

        /// <summary>
        /// Create extensions for the 
        /// </summary>
        public static IEnumerable<Extension> CreateExtensions(this IIdentifiedEntity me, ResourceType applyTo)
        {
            return s_handlers.Where(o => o.AppliesTo == null || o.AppliesTo == applyTo).Select(o => o.Construct(me));
        }

        /// <summary>
        /// Try to apply the specified extension to the specified object
        /// </summary>
        public static bool TryApplyExtension(this Extension me, IdentifiedData applyTo)
        {
            return s_handlers.Where(o => o.Uri.ToString() == me.Url).Select(r => r.Parse(me, applyTo)).All(o=>o);
        }
    }
}
