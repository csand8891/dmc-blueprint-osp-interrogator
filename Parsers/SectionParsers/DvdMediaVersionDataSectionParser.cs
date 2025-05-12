using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class DvdMediaVersionDataSectionParser
    {
        private string? _currentDvdMediaKey = null;

        public void ParseLine(string line, DvdMediaVersionData dvdMediaData)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                _currentDvdMediaKey = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentDvdMediaKey != null)
            {
                switch (_currentDvdMediaKey)
                {
                    case "Windows System CD Version":
                        dvdMediaData.WindowsSystemCdVersion = valueCandidate;
                        break;
                    case "OSP System CD Version":
                        dvdMediaData.OspSystemCdVersion = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '[{_currentDvdMediaKey}]' in DvdMediaVersionData section.");
                        break;
                }
                _currentDvdMediaKey = null; // Typically, one value per key in this section
            }
        }
        public void ResetState()
        {
            _currentDvdMediaKey = null;
        }
    }
}