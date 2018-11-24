using SanteDB.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local extension types
    /// </summary>
    public class LocalExtensionTypeRepository : GenericLocalMetadataRepository<ExtensionType>, IExtensionTypeRepository
    {
        /// <summary>
        /// Get the xtension type by uri
        /// </summary>
        public ExtensionType Get(Uri uri)
        {
            var name = uri.ToString();
            int t;
            return base.Find(o => o.Name == name, 0, 1, out t).FirstOrDefault();
        }
    }
}
