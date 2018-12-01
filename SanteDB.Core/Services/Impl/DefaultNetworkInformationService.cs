using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Default network information service
    /// </summary>
    [ServiceProvider("Default Network Information Service")]
    public class DefaultNetworkInformationService : INetworkInformationService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Default Network Information Service";

        /// <summary>
        /// Get whether network is available
        /// </summary>
        public bool IsNetworkAvailable => NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// Determine if the network is wifi
        /// </summary>
        public bool IsNetworkWifi
        {
            get
            {
                return NetworkInterface.GetAllNetworkInterfaces().Any(o => o.OperationalStatus == OperationalStatus.Up &&
                    (o.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || o.NetworkInterfaceType == NetworkInterfaceType.Ethernet));
            }
        }

        /// <summary>
        /// True if the network is wifi
        /// </summary>
        public bool IsNetworkConnected => NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// Fired when the network stats has changed
        /// </summary>
        public event EventHandler NetworkStatusChanged;

        /// <summary>
        /// Default network information service
        /// </summary>
        public DefaultNetworkInformationService()
        {
            NetworkChange.NetworkAvailabilityChanged += (o, e) =>
            {
                this.NetworkStatusChanged?.Invoke(this, e);
            };
        }

        /// <summary>
        /// Get the local host name
        /// </summary>
        public string GetHostName()
        {
            return Dns.GetHostName();
        }

        /// <summary>
        /// Get all network interfaces
        /// </summary>
        public IEnumerable<NetworkInterfaceInfo> GetInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Select(o => new NetworkInterfaceInfo(
                o.Name, o.GetPhysicalAddress().ToString(), o.OperationalStatus == OperationalStatus.Up, o.Description, o.GetIPProperties().UnicastAddresses.FirstOrDefault()?.ToString()
            ));

        }

        /// <summary>
        /// Perform a NSLookup
        /// </summary>
        public string Nslookup(string address)
        {
            try
            {
                System.Uri uri = null;
                if (System.Uri.TryCreate(address, UriKind.RelativeOrAbsolute, out uri))
                    address = uri.Host;
                var resolution = System.Net.Dns.GetHostEntry(address);
                return resolution.AddressList.First().ToString();
            }
            catch
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Ping the specified host
        /// </summary>
        public long Ping(string hostName)
        {
            try
            {
                System.Uri uri = null;
                if (System.Uri.TryCreate(hostName, UriKind.RelativeOrAbsolute, out uri))
                    hostName = uri.Host;
                System.Net.NetworkInformation.Ping p = new System.Net.NetworkInformation.Ping();
                var reply = p.Send(hostName);
                return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Get the machine name
        /// </summary>
        public string GetMachineName()
        {
            return Environment.MachineName;
        }
    }
}
