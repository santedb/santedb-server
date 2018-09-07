using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Model
{
    /// <summary>
    /// Marks a class as an MDM master record
    /// </summary>
    public interface IMdmMaster<T> : IVersionedEntity, IIdentifiedEntity, ITaggable, ISecurable
    {

        /// <summary>
        /// Constructs the master record from local records
        /// </summary>
        T GetMaster(IPrincipal principal);

        /// <summary>
        /// Gets the local records
        /// </summary>
        List<T> LocalRecords { get; }

    }
}
