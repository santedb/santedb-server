using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Internal interface to allow copy to a context while allowing the source to 
    /// open its own source context
    /// </summary>
    internal interface IAdoCopyProvider
    {

        /// <summary>
        /// Copy the specified keys to the specified context
        /// </summary>
        void CopyTo(Guid[] keysToCopy, DataContext toContext);

        /// <summary>
        /// Copy the specified object and all sub-objects from <paramref name="fromContext"/> to <paramref name="toContext"/>
        /// </summary>
        void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext);

    }
}
