using DmcBlueprint.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DmcBlueprint.Parsers
{
    public class DataManagementCardParser
    {
        public DataManagementCard Parse(string filePath)
        {
            var managementCard = new SoftwareDataManagementCard();
            var lines = File.ReadAllLines(filePath);

            
        }
        private enum CurrentSection
            {
                None,
                Header,
                MachineIdentification,
                DistributorAndCustomerInfo,
                RevisionAndCustomizationInfo,
                DvdMediaVersionData,
                SoftwareVersionIdentifier,
                SoftwarePackageComposition,
                
            }

        // --- State Variables ---

        private CurrentSection _currentSection = CurrentSection.None;
        private string _currentCustomSoftGroup = null;

        // --- Helper Functions ---
        
        private void UpdateCurrentSection(string sectionHeaderLine)
        {
            // Map header line to CurrentSection enum ...
        }

        internal bool IsCommentLine(string line)
        {
            // Called when parsing RevisionAndCustomizationInfo section
            string trimmedLine = line.Trim();
            return !string.IsNullOrWhiteSpace(trimmedLine) &&
                   !trimmedLine.StartsWith("<") &&
                   !trimmedLine.StartsWith("[") &&
                   !trimmedLine.StartsWith("=");
        }

        internal RevisionEntry ParseRevisionEntryFromLine(string line)
        {
            // Example line: "1  03/07/23  SO# 2170654  P# 1080911"
            var entry = new RevisionEntry { FullCommentLine = line };
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1) // Need at least an identifier
            {
                entry.Identifier = parts[0];
            }
            else
            {
                Console.WriteLine($"Warning: Could not parse identifier from comment line: {line}");
                return null; // Or handle error appropriately
            }

            if (parts.Length >= 2) // Check if there's a potential date part
            {
                // Attempt to parse date as DateTime
                // The source format "MM/dd/yy" does not include time.
                // DateTime will store this with a default time (usually 00:00:00).
                if (DateTime.TryParseExact(parts[1], "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                {
                    entry.SoftwareProductionDate = dateTimeValue;
                }
                else
                {
                    // Log parsing error for date or handle as needed
                    Console.WriteLine($"Warning: Could not parse date '{parts[1]}' as DateTime from comment line: {line}");
                }
            }
            else
            {
                Console.WriteLine($"Warning: Not enough parts to parse date from comment line: {line}");
            }

            // Find SO# and P# - this can be made more robust
            // The existing logic for SO# and P# from your snippet:
            entry.SalesOrderNumber = parts.FirstOrDefault(p => p.StartsWith("SO#"));
            entry.ProjectNumber = parts.FirstOrDefault(p => p.StartsWith("P#"));

            // The more robust (commented-out) section for finding SO# and P# can be implemented here if needed.
            // For example:
            // string soPattern = "SO#";
            // string pPattern = "P#";
            // bool soFound = false;
            // bool pFound = false;

            // for (int i = 2; i < parts.Length; i++) // Start searching after ID and Date parts
            // {
            //     if (!soFound && parts[i].StartsWith(soPattern))
            //     {
            //         entry.SalesOrderNumber = parts[i];
            //         // Potentially look at parts[i+1] if SO# can span multiple parts,
            //         // but be careful not to consume parts belonging to P#.
            //         soFound = true;
            //     }
            //     else if (!pFound && parts[i].StartsWith(pPattern))
            //     {
            //         entry.ProjectNumber = parts[i];
            //         // Similar logic if P# can span multiple parts.
            //         pFound = true;
            //     }
            // }
            // if (!soFound) Console.WriteLine($"Warning: SO# not found or not in expected format in line: {line}");
            // if (!pFound) Console.WriteLine($"Warning: P# not found or not in expected format in line: {line}");


            // Basic check: ensure at least an identifier was found.
            // You might want more sophisticated validation if SO# or P# are mandatory.
            if (string.IsNullOrEmpty(entry.Identifier))
            {
            // This case is handled by the initial parts.Length check for identifier
            // If we reach here, Identifier should be set unless the first part was empty (which SplitRemoveEmptyEntries handles)
            }
            
            return entry;
        }
    }
}