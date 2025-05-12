using DmcBlueprint.Models;
using System;
using System.Globalization;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class MachineDataSectionParser
    {
        private string? _currentMachineDataKey = null;

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