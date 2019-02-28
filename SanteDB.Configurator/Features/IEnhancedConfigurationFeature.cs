using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator.Features
{
    /// <summary>
    /// Represents a configuration panel with a configuration panel
    /// </summary>
    public interface IEnhancedConfigurationFeature : IFeature
    {

        /// <summary>
        /// Gets the custom control to use for the configuration panel
        /// </summary>
        Control ConfigurationPanel { get; }

    }
}
