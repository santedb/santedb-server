using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Messaging.HL7.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Interceptors
{
    /// <summary>
    /// Represens a notification service that will handle ADT messages from the repository layer whenever data
    /// is created
    /// </summary>
    public abstract class InterceptorBase 
    {

        /// <summary>
        /// Gets the configuration for this object
        /// </summary>
        public Hl7InterceptorConfigurationElement Configuration { get; }

        /// <summary>
        /// Creates a new notifier base
        /// </summary>
        public InterceptorBase(Hl7InterceptorConfigurationElement configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Attach to whatever type
        /// </summary>
        public abstract void Attach();

        /// <summary>
        /// Detach from whatever source this listener is hooked to
        /// </summary>
        public abstract void Detach();

        /// <summary>
        /// Execute the configured guards
        /// </summary>
        protected bool ExecuteGuard<TModel>(TModel data) where TModel : IdentifiedData
        {
            var retVal = true;
            foreach(var guard in this.Configuration.Guards)
                retVal &= QueryExpressionParser.BuildLinqExpression<TModel>(NameValueCollection.ParseQueryString(guard)).Compile().Invoke(data);
            return retVal;
        }
    }
}
