using Newtonsoft.Json;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Api
{
    /// <summary>
    /// Formatted text
    /// </summary>
    [JsonObject()]
    public class FormattedText
    {
        /// <summary>
        /// Formated text ctor
        /// </summary>
        public FormattedText()
        {
            this.Format = "markdown";
        }

        /// <summary>
        /// Gets or sets the format
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the raw format
        /// </summary>
        [JsonProperty("raw")]
        public string Raw { get; set; }

    }
}