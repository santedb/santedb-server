using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

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
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/{*content}")]
        Stream StaticContent(string content);

        /// <summary>
        /// Create dataset 
        /// </summary>
        [OperationContract]
        [WebInvoke(Method = "POST", BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/dataset")]
        Stream CreateDataset(Stream datasetSource);
    }
}
