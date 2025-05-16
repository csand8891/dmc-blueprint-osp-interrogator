using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ Soft Version Excepted OSP System CD/DVD ]" section of a DMC file.
    /// This section lists version information for various software components like Windows System,
    /// Custom API, and MTConnect. Keys are expected to be enclosed in square brackets.
    /// </summary>
    internal class SoftVersionSectionParser
    {
        private string? _currentSoftVersionKey = null;

        /// <summary>
        /// Parses a single line from the "[ Soft Version Excepted OSP System CD/DVD ]" section.
        /// It identifies software component names (keys) and their version strings (values),
        /// populating the provided <see cref="SoftwareVersionIdentifier"/> object.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="softVersions">The <see cref="SoftwareVersionIdentifier"/> object to populate.</param>
        public void ParseLine(string line, SoftwareVersionIdentifier softVersions)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                _currentSoftVersionKey = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentSoftVersionKey != null)
            {
                switch (_currentSoftVersionKey)
                {
                    case "Windows System Version":
                        softVersions.WindowsSystemVersion = valueCandidate;
                        break;
                    case " Windows System Version ":
                        softVersions.WindowsSystemVersion = valueCandidate;
                        break;
                    case "Custom API Additional DVD Version":
                        softVersions.ApiDvdVersion = valueCandidate;
                        break;
                    case "MTconnect Version@(Included in App_THINC_API DVD)": // Special char might need exact match
                    case "MTconnect Version": // Fallback
                        softVersions.MtConnectVersion = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '[{_currentSoftVersionKey}]' in SoftVersionExceptedOspSystemCd section.");
                        break;
                }
                _currentSoftVersionKey = null; // Typically one value per key
            }
        }

        /// <summary>
        /// Resets the internal state of the parser.
        /// This should be called when starting to parse a new DMC file or before re-parsing,
        /// to ensure that any previously stored software version key is cleared.
        /// </summary>
        public void ResetState() {
            _currentSoftVersionKey = null;
        }
    }
}