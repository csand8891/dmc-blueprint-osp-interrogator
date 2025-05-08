using DmcBlueprint.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;

namespace DmcBlueprint.Parsers
{
    public class DataManagementCardParser
    {
        public SoftwareDataManagementCard Parse(string filePath)
        {
            var managementCard = new SoftwareDataManagementCard();
            var lines = File.ReadAllLines(filePath);

            return managementCard;
        }
        private enum CurrentSection
            {
                None,
                MachineData,
                CustomerData,
                Note,
                DvdMediaVersionData,
                SoftVersionExceptedOspSystemCd,
                PackageSoftComposition,
                NcCustomSoftComposition,
                NcSpecCode1,
                NcSpecCode2,
                NcSpecCode3,
                PlcSpecCode1,
                PlcSpecCode2,
                PlcSpecCode3,
                
            }

        // --- State Variables ---

        private CurrentSection _currentSection = CurrentSection.None;
        private string? _currentCustomSoftGroup = null;

        // --- Helper Functions ---
        
        private void UpdateCurrentSection(string sectionHeaderLine)
        {
            // Map header line to CurrentSection enum ...
            string trimmedHeader = sectionHeaderLine.Trim();
            int openBracketIndex = trimmedHeader.IndexOf('[');
            int closBracketIndex = trimmedHeader.IndexOf(']');

            if (openBracketIndex != -1 && closeBracketIndex != -1 && closeBracketIndex > openBracketIndex)
            {
                string sectionName = trimmedHeader.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();

                switch (sectionName)
                {
                    case "Machine Data":
                        _currentSection = CurrentSection.MachineData;
                        break;
                    case "Customer Data":
                        _currentSection = CurrentSection.CustomerData;
                        break;
                    case "NOTE":
                        _currentSection = CurrentSection.Note;
                        break;
                    case "DVD Media Version Data":
                        _currentSection = CurrentSection.DvdMediaVersionData;
                        break;
                    case "Soft Version Excepted OSP System CD": // Note: "Excepted" might be a typo in the file for "Expected" or "Excluding"
                        _currentSection = CurrentSection.SoftVersionExceptedOspSystemCd;
                        break;
                    case "Package Soft composition": // Note: "composition" vs "Composition"
                        _currentSection = CurrentSection.PackageSoftComposition;
                        break;
                    case "NC Custom Soft composition":
                        _currentSection = CurrentSection.NcCustomSoftComposition;
                        break;
                    case "NC-SPEC CODE No.1":
                        _currentSection = CurrentSection.NcSpecCode1;
                        break;
                    case "NC-SPEC CODE No.2":
                        _currentSection = CurrentSection.NcSpecCode2;
                        break;
                    case "NC-SPEC CODE No.3":
                        _currentSection = CurrentSection.NcSpecCode3;
                        break;
                    case "PLC-SPEC CODE No.1":
                        _currentSection = CurrentSection.PlcSpecCode1;
                        break;
                    case "PLC-SPEC CODE No.2":
                        _currentSection = CurrentSection.PlcSpecCode2;
                        break;
                    case "PLC-SPEC CODE No.3":
                        _currentSection = CurrentSection.PlcSpecCode3;
                        break;
                    default:
                        Console.WriteLine($"Warning: Unknown section name encountered: [{sectionName}] in line: {sectionHeaderLine}");
                        // Decide if _currentSection should be set to None or retain its previous value
                        // _currentSection = CurrentSection.None; 
                        break;
                }
            }
            else
            {
                // This line was passed to UpdateCurrentSection but doesn't seem to be a valid header.
                // This might happen if your IsSectionHeader check in the main loop is too broad.
                // For now, we'll assume it's not a section we should change to.
                // Console.WriteLine($"Warning: Line treated as section header but not in expected format: {sectionHeaderLine}");
            }
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

        internal RevisionEntry? ParseRevisionEntryFromLine(string line)
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
            //if (string.IsNullOrEmpty(entry.Identifier))
            //{
            // This case is handled by the initial parts.Length check for identifier
            // If we reach here, Identifier should be set unless the first part was empty (which SplitRemoveEmptyEntries handles)
            //}
            
            return entry;
        }
    }
}