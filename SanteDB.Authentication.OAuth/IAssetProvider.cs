using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// OAuth Asset Provider Interface
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// Gets a stream for an asset from the asset provider.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="locale"></param>
        /// <param name="bindingValues"></param>
        /// <returns></returns>
        (Stream content, string mimeType) GetAsset(string id, string locale, IDictionary<string, string> bindingValues);
    }
}
