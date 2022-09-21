using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model.Jwks
{
    /// <summary>
    /// Serialization class for the jwks endpoint. This hides all the internal details from the Key.
    /// </summary>
    internal class Key
    {
        [JsonProperty("alg")]
        public string Algorithm { get; set; }
        [JsonProperty("kty")]
        public string KeyType { get; set; }
        [JsonProperty("use")]
        public string Use { get; set; }
        [JsonProperty("x5c")]
        public IList<string> CertificateChain { get; set; }
        [JsonProperty("n")]
        public string Modulus { get; set; }
        [JsonProperty("e")]
        public string Exponent { get; set; }
        [JsonProperty("kid")]
        public string KeyId { get; set; }
        [JsonProperty("x5t")]
        public string Thumbprint { get; set; }
        [JsonProperty("x5t#S256")]
        public string ThumbprintSHA256 { get; set; }
        [JsonProperty("key_ops")]
        public IList<string> KeyOperations { get; set; }
        [JsonProperty("x5u")]
        public string CertificateUrl { get; set; }
        [JsonProperty("k")]
        public string K { get; set; }

        public bool ShouldSerializeCertificateChain() => CertificateChain.Count > 0;
        public bool ShouldSerializeKeyOperations() => KeyOperations.Count > 0;
    }
}
