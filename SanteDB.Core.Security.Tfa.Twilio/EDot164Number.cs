using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Security.Tfa.Twilio
{
    /// <summary>
    /// Formatter that can parse and return a phone number from a URI. 
    /// </summary>
    internal class EDot164Number
    {
        private static readonly Regex s_NumberRegex = new Regex(@"^(?:(sms|tel|mms)\:\/?\/?)?(\+[1-9]\d{1,14})\;?(?:\;.+?)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        readonly Match _Matched;

        readonly string _NormalizedNumber;

        public EDot164Number(string number)
        {
            if (string.IsNullOrEmpty(number))
            {
                throw new ArgumentNullException(nameof(number));
            }

            _Matched = s_NumberRegex.Match(number);

            if (!_Matched.Success)
            {
                throw new FormatException($"Invalid format for EDot164Number: {number}");
            }

            _NormalizedNumber = Normalize(_Matched.Groups[2].Value);
        }

        public string Protocol => _Matched.Groups[1].Value;

        public string Number => _NormalizedNumber;

        public override string ToString() => Number;

        private static string Normalize(string number)
        {
            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < number.Length; i++)
            {
                if (char.IsDigit(number, i))
                {
                    sb.Append(number[i]);
                }
                else if (char.IsLetter(number, i))
                {
                    char c = number[i];

                    if ((c >= 'a' && c <= 'c') || (c >= 'A' && c <= 'C'))
                    {
                        sb.Append('2');
                    }
                    else if ((c >= 'd' && c <= 'f') || (c >= 'D' && c <= 'F'))
                    {
                        sb.Append('3');
                    }
                    else if ((c >= 'g' && c <= 'i') || (c >= 'G' && c <= 'I'))
                    {
                        sb.Append('4');
                    }
                    else if ((c >= 'j' && c <= 'l') || (c >= 'J' && c <= 'L'))
                    {
                        sb.Append('5');
                    }
                    else if ((c >= 'm' && c <= 'o') || (c >= 'M' && c <= 'O'))
                    {
                        sb.Append('6');
                    }
                    else if ((c >= 'p' && c <= 's') || (c >= 'P' && c <= 'S'))
                    {
                        sb.Append('7');
                    }
                    else if ((c >= 't' && c <= 'v') || (c >= 'T' && c <= 'V'))
                    {
                        sb.Append('8');
                    }
                    else if ((c >= 'w' && c <= 'z') || (c >= 'W' && c <= 'Z'))
                    {
                        sb.Append('9');
                    }
                }
            }

            sb.Insert(0, '+');

            return sb.ToString();
        }

    }
}
