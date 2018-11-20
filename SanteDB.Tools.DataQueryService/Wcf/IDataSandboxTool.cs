using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using RestSrvr;
using System.Text;
using System.Threading.Tasks;
using RestSrvr.Attributes;

namespace SanteDB.Tools.DataSandbox.Wcf
{
    /// <summary>
    /// Query tool contract
    /// </summary>
    [ServiceContract]
    public interface IDataSandboxTool
    {
        /// <summary>
        /// Get static content 
        /// </summary>
        /// <param name="content">The content to retrieve</param>
        /// <returns>The static content</returns>
        [RestInvoke(Method = "GET", UriTemplate = "/{*content}")]
        Stream StaticContent(string content);

        /// <summary>
        /// Create dataset 
        /// </summary>
        [RestInvoke(Method = "POST", UriTemplate = "/dataset")]
        Stream CreateDataset(Stream datasetSource);
    }
}
