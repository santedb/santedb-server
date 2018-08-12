using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.AMI.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Applet
{
    /// <summary>
    /// Represents meta information about a solution
    /// </summary>
    [XmlType(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AppletManifestInfo))]
    public class AppletSolutionInfo : AppletManifestInfo
    {

        /// <summary>
		/// Initializes a new instance of the <see cref="AppletSolutionInfo"/> class
		/// with a specific applet manifest instance.
		/// </summary>
		public AppletSolutionInfo(AppletSolution soln, X509Certificate2Info publisher) : base(soln.Meta, publisher)
        {
            this.Include = soln.Include.Select(s => new AppletManifestInfo(s.Meta, null)).ToList();
        }

        /// <summary>
        /// Gets the data this includes
        /// </summary>
        [XmlElement("include")]
        public List<AppletManifestInfo> Include { get; set; }
    }
}
