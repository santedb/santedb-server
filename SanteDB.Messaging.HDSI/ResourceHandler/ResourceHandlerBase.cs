using SanteDB.Core.Model;
using SanteDB.Messaging.HDSI.Wcf;
using System;

namespace SanteDB.Messaging.HDSI.ResourceHandler
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
        override public Type Scope => typeof(IHdsiServiceContract);
    }
}