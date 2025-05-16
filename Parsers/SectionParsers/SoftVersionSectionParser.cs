using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class SoftVersionSectionParser
    {
        private string? _currentSoftVersionKey = null;

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
        public void ResetState() {
            _currentSoftVersionKey = null;
        }
    }
}