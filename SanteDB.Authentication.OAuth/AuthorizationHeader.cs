using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2
{
    internal class AuthorizationHeader : IEquatable<AuthorizationHeader>, IEquatable<string>
    {
        public AuthorizationHeader()
        {

        }

        public AuthorizationHeader(string scheme, string value)
        {
            Scheme = scheme;
            Value = value;
        }

        public string Scheme { get; set; }
        public string Value { get; set; }

        public bool IsScheme(string scheme) => Scheme.Equals(scheme, StringComparison.InvariantCultureIgnoreCase);

        public override string ToString()
        {
            return $"{Scheme} {Value}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (null == this && null == obj)
            {
                return true;
            }
            else if (null == this || null == obj)
            {
                return false;
            }
            else
            {
                if (obj is string s)
                {
                    return this.Equals(s);
                }
                else if (obj is AuthorizationHeader ah)
                {
                    return this.Equals(ah);
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool TryParse(string s, out AuthorizationHeader header)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                header = null;
                return false;
            }

            var parts = s.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                header = new AuthorizationHeader(parts[0], parts[1]);
                return true;
            }

            header = null;
            return false;
        }

        public static AuthorizationHeader Parse(string s)
        {
            if (!TryParse(s, out var val))
            {
                throw new FormatException("Header format is malformed.");
            }

            return val;
        }

        public bool Equals(AuthorizationHeader other)
        {
            if (null == this && null == other)
            {
                return true;
            }
            else if (null == this || null == other)
            {
                return false;
            }
            else
            {
                return Scheme.Equals(other.Scheme) && Value.Equals(other.Value);
            }
        }

        public bool Equals(string other)
        {
            if (null == this && null == other)
            {
                return true;
            }
            else if (null == this || null == other)
            {
                return false;
            }
            else
            {
                return other.Equals(ToString());
            }
        }

        public static readonly string Schemes_Basic = "basic";
        public static readonly string Schemes_Bearer = "bearer";
        public static readonly string Schemes_Digest = "digest";

    }
}
