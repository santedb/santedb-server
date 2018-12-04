using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Query;
using SanteDB.Messaging.HL7.ParameterMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Utils
{
    /// <summary>
    /// Represents a query parsers utils
    /// </summary>
    public static class MessageUtils
    {

        /// <summary>
        /// Parse a message
        /// </summary>
        public static IMessage ParseMessage(String messageData, out string originalVersion)
        {
            Regex versionRegex = new Regex(@"^(MSH\|.*?)(2\.[0-9\.]+)(.*)$", RegexOptions.Multiline);
            var match = versionRegex.Match(messageData);
            if (!match.Success)
                throw new InvalidOperationException("Message appears to be invalid");
            else
            {
                originalVersion = match.Groups[2].Value;
                PipeParser parser = new PipeParser();
                return parser.Parse(messageData, "2.5");
            }
        }

        /// <summary>
        /// Rewrite a QPD query to an HDSI query
        /// </summary>
        public static NameValueCollection ParseQueryElement(IEnumerable<Varies> varies, Hl7QueryParameterType map, String matchAlgorithm = null, double? matchStrength = null )
        {
            NameValueCollection retVal = new NameValueCollection();

            // Query parameters
            foreach (var qp in varies)
            {
                var composite = qp.Data as GenericComposite;

                // Parse the parameters
                var qfield = (composite.Components[0] as Varies)?.Data?.ToString();
                var qvalue = (composite.Components[1] as Varies)?.Data?.ToString();

                // Attempt to find the query parameter and map
                var parm = map.Parameters.Where(o => o.Hl7Name == qfield || o.Hl7Name == qfield + ".1" || o.Hl7Name == qfield + ".1.1").OrderBy(o => o.Hl7Name.Length - qfield.Length).FirstOrDefault();
                if (parm == null)
                    throw new ArgumentOutOfRangeException($"{qfield} not mapped to query parameter");

                switch (parm.ParameterType)
                {
                    case "concept":
                        retVal.Add($"{parm.ModelName}.referenceTerm.term.mnemonic", qvalue);
                        break;
                    case "string": // Enables phonetic matching
                        String transform = null;
                        switch (matchAlgorithm.ToLower())
                        {
                            case "exact":
                                transform = "{0}";
                                break;
                            case "soundex":
                                if (matchStrength.HasValue)
                                    transform = ":(soundex){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},soundex)<={matchStrength * qvalue.Length}";
                                break;
                            case "metaphone":
                                if (matchStrength.HasValue)
                                    transform = ":(metaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},metaphone)<={matchStrength * qvalue.Length}";
                                break;
                            case "dmetaphone":
                                if (matchStrength.HasValue)
                                    transform = ":(dmetaphone){0}";
                                else
                                    transform = $":(phonetic_diff|{{0}},dmetaphone)<={matchStrength * qvalue.Length}";
                                break;
                            case "alias":
                                transform = $":(alias|{{0}})>={matchStrength ?? 3}";
                                break;
                            default:
                                transform = $":(phonetic_diff|{{0}})<={matchStrength * qvalue.Length},:(alias|{{0}})>={matchStrength ?? 3}";
                                break;
                        }
                        retVal.Add(parm.ModelName, transform.Split(',').Select(tx => String.Format(tx, qvalue)).ToList());
                        break;
                    default:
                        var txv = parm.ValueTransform ?? "{0}";
                        retVal.Add(parm.ModelName, txv.Split(',').Select(tx => String.Format(tx, qvalue)).ToList());
                        break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Encode the specified message
        /// </summary>
        public static String EncodeMessage(IMessage response, string originalVersion)
        {
            // Rewrite back to original version
            (response.GetStructure("MSH") as MSH).VersionID.VersionID.Value = originalVersion;
            return new PipeParser().Encode(response);
        }
    }
}
