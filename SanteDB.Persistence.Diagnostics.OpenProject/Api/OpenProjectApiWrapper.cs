using Newtonsoft.Json;
using SanteDB.Core.Http;
using SanteDB.Core.Interop.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Api
{
    /// <summary>
    /// Represents an OpenAPI Wrapper
    /// </summary>
    public class OpenProjectApiWrapper : ServiceClientBase
    {
        /// <summary>
        /// Creates a new wrapper
        /// </summary>
        public OpenProjectApiWrapper(IRestClient restClient) : base(restClient)
        {
            this.Client.Accept = "application/json";
        }

        /// <summary>
        /// Creates a new work package on the API
        /// </summary>
        public WorkPackage CreateWorkPackage(String projectKey, WorkPackage workPackage)
        {
            return this.Client.Post<WorkPackage, WorkPackage>($"/api/v3/projects/{projectKey}/work_packages?notify", "application/json", workPackage);
        }

        /// <summary>
        /// Create an attachment for the specified work package
        /// </summary>
        public void CreateAttachments(WorkPackage issue, List<MultipartAttachment> attachments)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            foreach (var itm in attachments)
            {
                var submission = new List<MultipartAttachment>()
                {
                    new MultipartAttachment(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Attachment() {
                        ContentType = itm.MimeType,
                        FileName = itm.Name,
                        FileSize = itm.Data.Length
                    })), "application/json", "metadata")
                };
                itm.Name = "file";
                this.Client.Post<List<MultipartAttachment>, Object>($"api/v3/work_packages/{issue.Id}/attachments", String.Format("multipart/form-data; boundary={0}", boundary), submission);
            }
        }
    }
}
