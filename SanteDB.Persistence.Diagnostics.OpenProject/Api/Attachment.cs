using Newtonsoft.Json;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Api
{
    /// <summary>
    /// Represents an attachment
    /// </summary>
    [JsonObject]
    public class Attachment
    {
        /// <summary>
        /// Gets or sets the title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the filename
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size
        /// </summary>
        [JsonProperty("fileSize")]
        public int FileSize { get; set; }

        /// <summary>
        /// Gets or sets the file content type
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [JsonProperty("description")]
        public FormattedText Description { get; set; }
    }
}