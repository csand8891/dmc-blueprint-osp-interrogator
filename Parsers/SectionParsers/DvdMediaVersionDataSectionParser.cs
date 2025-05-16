using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ DVD Media Version Data ]" section of a DMC file.
    /// This section contains version information for Windows System and OSP System CD/DVDs.
    /// Keys are expected to be enclosed in square brackets (e.g., "[ Windows System CD/DVD Version ]").
    /// </summary>
    internal class DvdMediaVersionDataSectionParser
    {
        /// <summary>
        /// Stores the current key being processed from the DVD Media Version Data section.
        /// Keys are identified by being enclosed in square brackets.
        /// </summary>
        private string? _currentDvdMediaKey = null;


        /// <summary>
        /// Parses a single line from the "[ DVD Media Version Data ]" section of a DMC file.
        /// It identifies keys (e.g., "[ Windows System CD/DVD Version ]") and their corresponding version values,
        /// populating the provided <see cref="DvdMediaVersionData"/> object.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="dvdMediaData">The <see cref="DvdMediaVersionData"/> object to populate with parsed data.</param>
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
                    case "Windows System CD/DVD Version":
                        dvdMediaData.WindowsSystemCdVersion = valueCandidate;
                        break;
                    case "OSP System CD Version":
                        dvdMediaData.OspSystemCdVersion = valueCandidate;
                        break;
                    case "OSP System CD/DVD Version":
                        dvdMediaData.OspSystemCdVersion = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '[{_currentDvdMediaKey}]' in DvdMediaVersionData section.");
                        break;
                }
                _currentDvdMediaKey = null; // Typically, one value per key in this section
            }
        }

        /// <summary>
        /// Resets the internal state of the parser.
        /// This should be called when starting to parse a new DMC file or before re-parsing,
        /// to ensure that any previously stored key is cleared.
        /// </summary>
        public void ResetState()
        {
            _currentDvdMediaKey = null;
        }
    }
}