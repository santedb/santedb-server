using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2.Model.Jwks
{
    internal class KeySet
    {
        [JsonIgnore]
        Microsoft.IdentityModel.Tokens.JsonWebKeySet _KeySet;

        public KeySet()
        {
            _KeySet = new Microsoft.IdentityModel.Tokens.JsonWebKeySet();
            Keys = new KeyList(_KeySet.Keys, ToKey, FromKey);
        }

        public KeySet(JsonWebKeySet keySet)
        {
            _KeySet = keySet;
            Keys = new KeyList(_KeySet.Keys, ToKey, FromKey);
        }   

        private Key ToKey(JsonWebKey jwk)
        {
            if (null == jwk)
            {
                return null;
            }

            return new Key
            {
                Algorithm = jwk.Alg,
                CertificateChain = jwk.X5c,
                Exponent = jwk.E,
                KeyId = jwk.KeyId,
                KeyType = jwk.Kty,
                Modulus = jwk.N,
                Thumbprint = jwk.X5t,
                Use = jwk.Use,
                KeyOperations = jwk.KeyOps,
                CertificateUrl = jwk.X5u,
                ThumbprintSHA256 = jwk.X5tS256,
                K = jwk.K
            };
        }

        private JsonWebKey FromKey(Key key)
        {
            if (null == key)
            {
                return null;
            }

            var jwk = new JsonWebKey()
            {
                Alg = key.Algorithm,
                E = key.Exponent,
                KeyId = key.KeyId,
                Kty = key.KeyType,
                N = key.Modulus,
                X5t = key.Thumbprint,
                Use = key.Use,
                X5u = key.CertificateUrl,
                X5tS256 = key.ThumbprintSHA256,
                K = key.K
            };

            foreach(var keyop in key.KeyOperations)
            {
                jwk.KeyOps.Add(keyop);
            }

            foreach(var cert in key.CertificateChain)
            {
                jwk.X5c.Add(cert);
            }

            return jwk;
        }

        [JsonProperty("keys", TypeNameHandling = TypeNameHandling.None)]
        public IList<Key> Keys { get; }

        [JsonArray]
        private class KeyList : IList<Key>
        {
            IList<JsonWebKey> _list;
            Func<JsonWebKey, Key> _ToKeyConverter;
            Func<Key, JsonWebKey> _FromKeyConverter;

            public KeyList(IList<JsonWebKey> innerList, Func<JsonWebKey, Key> toKeyConverter, Func<Key, JsonWebKey> fromKeyConverter)
            {
                _list = innerList;
                _ToKeyConverter = toKeyConverter;
                _FromKeyConverter = fromKeyConverter;
            }

            public Key this[int index] { get => _ToKeyConverter(_list[index]); set => _list[index] = _FromKeyConverter(value); }

            public int Count => _list.Count;

            public bool IsReadOnly => _list.IsReadOnly;

            public void Add(Key item)
            {
                _list.Add(_FromKeyConverter(item));
            }

            public void Clear()
            {
                _list.Clear();
            }

            public bool Contains(Key item)
            {
                return _list.Contains(_FromKeyConverter(item));
            }

            public void CopyTo(Key[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<Key> GetEnumerator()
            {
                return _list.Select(_ToKeyConverter).GetEnumerator();
            }

            public int IndexOf(Key item)
            {
                return _list.IndexOf(_FromKeyConverter(item));
            }

            public void Insert(int index, Key item)
            {
                _list.Insert(index, _FromKeyConverter(item));
            }

            public bool Remove(Key item)
            {
                return _list.Remove(_FromKeyConverter(item));
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _list.Select(_ToKeyConverter).GetEnumerator();
            }
        }

    }
}
