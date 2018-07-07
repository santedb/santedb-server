using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Messaging.AMI.Wcf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.ResourceHandler
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to HDSI
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public class ResourceHandlerBase<TData> : SanteDB.Messaging.Common.ResourceHandlerBase<TData> where TData : IdentifiedData
    {

        /// <summary>
        /// Gets the scope
        /// </summary>
        override public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the resource capabilities for the object
        /// </summary>
        public override ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Create | ResourceCapability.Update | ResourceCapability.Get | ResourceCapability.Search;
            }
        }

    }
}
