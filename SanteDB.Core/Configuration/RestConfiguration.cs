using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// REST configuration 
    /// </summary>
    public class RestConfiguration
    {

        /// <summary>
        /// Rest configuration
        /// </summary>
        public RestConfiguration()
        {
            this.Services = new List<RestServiceConfiguration>();
        }

        /// <summary>
        /// Gets the list of services
        /// </summary>
        public List<RestServiceConfiguration> Services { get; private set; }
    }

    public class RestServiceConfiguration
    {

        /// <summary>
        /// Creates a new rest service configuration
        /// </summary>
        public RestServiceConfiguration(String name)
        {
            this.Name = name;
            this.Endpoints = new List<RestEndpointConfiguration>();
            this.Behaviors = new List<Type>();
        }

        /// <summary>
        /// Gets or sets the name of the service
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// Gets or sets the endpoints
        /// </summary>
        public List<RestEndpointConfiguration> Endpoints { get; private set; }

        /// <summary>
        /// Gets or sets the behaviors
        /// </summary>
        public List<Type> Behaviors { get; private set; }

    }

    /// <summary>
    /// Represents a single endpoint configuration
    /// </summary>
    public class RestEndpointConfiguration
    {

        /// <summary>
        /// Creates a new REST endpoint
        /// </summary>
        public RestEndpointConfiguration(Uri address, Type contract)
        {
            this.Address = address;
            this.Contract = contract;
        }

        /// <summary>
        /// Gets or sets the listening address
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Gets or sets the contract type
        /// </summary>
        public Type Contract { get; private set; }


    }
}
