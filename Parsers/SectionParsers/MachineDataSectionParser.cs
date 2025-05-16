using DmcBlueprint.Models;
using System;
using System.Globalization;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ Machine Data ]" section of a DMC file.
    /// This section contains key-value pairs identifying various machine and software details.
    /// The parser maintains state to associate values with the most recently encountered key.
    /// </summary>
    internal class MachineDataSectionParser
    {
        private string? _currentMachineDataKey = null;

        /// <summary>
        /// Parses a single line from the "[ Machine Data ]" section of a DMC file.
        /// It identifies keys (enclosed in angle brackets) and their corresponding values,
        /// populating the provided <see cref="MachineIdentifier"/> object.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="machineDetails">The <see cref="MachineIdentifier"/> object to populate with parsed data.</param>
        public void ParseLine(string line, MachineIdentifier machineDetails)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (line.StartsWith("<") && line.EndsWith(">"))
            {
                _currentMachineDataKey = line.Substring(1, line.Length - 2).Trim();
                if (_currentMachineDataKey == "Type of Machine")
                {
                    machineDetails.MachineType.Clear(); // Prepare for new list of machine types
                }
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentMachineDataKey != null)
            {
                switch (_currentMachineDataKey)
                {
                    case "Type of OSP":
                        machineDetails.OspType = valueCandidate;
                        break;
                    case "Type of Machine":
                        machineDetails.MachineType.Add(valueCandidate);
                        break;
                    case "Soft Production No":
                        machineDetails.SoftwareProductionNumber = valueCandidate;
                        break;
                    case "Project No":
                        machineDetails.ProjectNumber = valueCandidate;
                        break;
                    case "Software Production Date":
                        if (DateTime.TryParse(valueCandidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                        {
                            machineDetails.SoftwareProductionDate = dateValue;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Could not parse Software Production Date '{valueCandidate}' in MachineData section.");
                        }
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '{_currentMachineDataKey}' in MachineData section.");
                        break;
                }
            }
        }
    }
}