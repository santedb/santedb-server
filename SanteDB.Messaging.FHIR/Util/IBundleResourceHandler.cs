using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Represents a bundle resource handler
    /// </summary>
    public interface IBundleResourceHandler
    {

        /// <summary>
        /// Maps the specified bundle entry resource to an identified data entry
        /// </summary>
        IdentifiedData MapToModel(BundleEntry bundleResource, RestOperationContext context, Bundle bundle);

    }
}
