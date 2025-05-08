using DmcBlueprint.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DmcBlueprint.Parsers
{
    public class DataManagementCardParser
    {
        private static readonly Regex SectionHeaderRegex =
            new Regex(@"^=+\s*\[(?<name>[^\]]+)\]\s*=+$", RegexOptions.Compiled);

        /// <summary>
        /// Parses the specified Data Management Card file and returns a structured representation.
        /// </summary>
        /// <param name="filePath">The full path to the Data Management Card file.</param>
        /// <returns>A <see cref="SoftwareDataManagementCard"/> object populated with data from the file.</returns>
        public SoftwareDataManagementCard Parse(string filePath)
        {
            var managementCard = new SoftwareDataManagementCard();
            var lines = File.ReadAllLines(filePath);

            var managementCard = new SoftwareDataManagementCard();
            var lines = File.ReadAllLines(filePath);
            _currentSection = CurrentSection.None; // Initialize at the start of parsing

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue; // Skip empty or whitespace-only lines
                }

                if (IsSectionHeader(trimmedLine))
                {
                    UpdateCurrentSection(trimmedLine);
                    _currentCustomSoftGroup = null; // Reset any section-specific sub-state
                }
                else
                {
                    // This is a data line, process it based on the _currentSection
                    switch (_currentSection)
                    {
                        case CurrentSection.MachineData:
                            // TODO: Parse machine data line and populate managementCard.MachineDetails
                            break;
                        case CurrentSection.CustomerData:
                            // TODO: Parse customer data line and populate managementCard.DistributorAndCustomerDetails
                            break;
                        case CurrentSection.Note:
                            if (IsCommentLine(trimmedLine))
                            {
                                var revisionEntry = ParseRevisionEntryFromLine(trimmedLine);
                                if (revisionEntry != null)
                                {
                                    managementCard.RevisionAndCustomization ??= new RevisionAndCustomizationInfo();
                                    managementCard.RevisionAndCustomization.RevisionEntries.Add(revisionEntry);
                                }
                            }
                            else
                            {
                                // TODO: Handle other types of lines within the NOTE section
                                // e.g., < Note NO.1 >, < Special Spec >, [ Not attached Windows System Disk ]
                            }
                            break;
                        case CurrentSection.DvdMediaVersionData:
                            // TODO: Parse DVD media version data line and populate managementCard.DvdMediaVersion
                            break;
                        case SoftVersionExceptedOspSystemCd:
                            // TODO: Parse Soft Version Excepted OSP System CD line and populate managementCard.SoftVersionExceptedOspSystemCd
                            break;
                        case PackageSoftComposition:
                            // TODO: Parse Package Soft Composition line and populate managementCard.PackageSoftComposition
                            break;
                        case NcCustomSoftComposition:
                            // TODO: Parse NC Custom Soft Composition line and populate managementCard.NcCustomSoftComposition
                            break;
                        case NcSpecCode1:
                            // TODO: Parse NC-SPEC CODE No.1 line and populate managementCard.NcSpecCode1
                            break;
                        case NcSpecCode2:
                            // TODO: Parse NC-SPEC CODE No.2 line and populate managementCard.NcSpecCode2
                            break;
                        case NcSpecCode3:
                            // TODO: Parse NC-SPEC CODE No.3 line and populate managementCard.NcSpecCode3
                            break;
                        case PlcSpecCode1:
                            // TODO: Parse PLC-SPEC CODE No.1 line and populate managementCard.
                            break;
                        case PlcSpecCode2:
                            // TODO: Parse PLC-SPEC CODE No.2 line and populate managementCard.
                            break;
                        case PlcSpecCode3:
                            // TODO: Parse PLC-SPEC CODE No.3 line and populate managementCard.
                            break;
                        case CurrentSection.None:
                            // This line is before any recognized section header or is otherwise unclassified.
                            // You might want to log it or parse it if it's part of a general file header.
                            // Example: The "cspsVer.12.1.0     2023-03-07  19:21:26" line from your example.
                            break;
                        default:
                            // Potentially log unhandled lines for a known section
                            break;
                    }
                }
            }

            return managementCard;
        }
        public enum CurrentSection
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

        // Property to expose _currentSection for testing
        internal CurrentSection CurrentSectionForTesting => _currentSection;

        // --- Helper Functions ---

        /// <summary>
        /// Checks if the given line matches the expected section header format (e.g., ===[Section Name]===).
        /// </summary>
        /// <param name="line">The line to check, expected to be already trimmed of leading/trailing whitespace from the caller.</param>
        /// <returns>True if the line is a section header, false otherwise.</returns>
        internal bool IsSectionHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }
            // The regex matches:
            // ^                  Start of the string
            // =+                 One or more '=' characters
            // \s*                Zero or more whitespace characters
            // \[                 A literal '['
            // (?<name>[^\]]+)   A named capture group "name" matching one or more characters that are not ']'
            // \]                 A literal ']'
            // \s*                Zero or more whitespace characters
            // =+                 One or more '=' characters
            // $                  End of the string
            return SectionHeaderRegex.IsMatch(line); // Assuming line is already trimmed by caller
        }
        
        /// <summary>
        /// Updates the internal current section state based on the provided section header line.
        /// This method assumes the <paramref name="sectionHeaderLine"/> has already been validated by <see cref="IsSectionHeader"/>.
        /// </summary>
        /// <param name="sectionHeaderLine">The validated section header line from the input file.</param>
        internal void UpdateCurrentSection(string sectionHeaderLine)
        {
            // This method now assumes sectionHeaderLine has already been validated by IsSectionHeader
            string trimmedHeader = sectionHeaderLine.Trim();
            
            // Extracting the name using Regex can be more robust if IsSectionHeader also provides the name
            // Or, we can stick to IndexOf if IsSectionHeader guarantees the brackets exist.
            Match match = SectionHeaderRegex.Match(trimmedHeader);
            string sectionName = "";

            if (match.Success && match.Groups["name"].Success)
            {
                sectionName = match.Groups["name"].Value.Trim();
            }
            else
            {
                // Fallback or error if regex matching fails unexpectedly after IsSectionHeader passed.
                // This path should ideally not be hit if IsSectionHeader is working correctly.
                // For robustness, we can still use the IndexOf approach as a fallback,
                // or simply log an error and set to None.
                int openBracketIndex = trimmedHeader.IndexOf('[');
                int closeBracketIndex = trimmedHeader.IndexOf(']');
                if (openBracketIndex != -1 && closeBracketIndex != -1 && closeBracketIndex > openBracketIndex)
                {
                    sectionName = trimmedHeader.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();
                }
                else
                {
                    Console.WriteLine($"Error: UpdateCurrentSection called with invalid header format despite IsSectionHeader: {sectionHeaderLine}");
                    _currentSection = CurrentSection.None;
                    return;
                }
            }

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
                case "Soft Version Excepted OSP System CD":
                    _currentSection = CurrentSection.SoftVersionExceptedOspSystemCd;
                    break;
                case "Package Soft composition":
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
                    _currentSection = CurrentSection.None; 
                    break;
            }
        }

        /// <summary>
        /// Determines if a given line from the "NOTE" section should be treated as a comment line (typically a revision entry).
        /// </summary>
        /// <param name="line">The line to check, expected to be already trimmed.</param>
        /// <returns>True if the line is considered a comment/revision entry; otherwise, false.</returns>
        internal bool IsCommentLine(string line)
        {
            // Called when parsing RevisionAndCustomizationInfo section
            string trimmedLine = line.Trim();
            return !string.IsNullOrWhiteSpace(trimmedLine) &&
                   !trimmedLine.StartsWith("<") &&
                   !trimmedLine.StartsWith("[") &&
                   !trimmedLine.StartsWith("=");
        }

        /// <summary>
        /// Parses a line (assumed to be a revision entry from the "NOTE" section) into a <see cref="RevisionEntry"/> object.
        /// </summary>
        /// <param name="line">The revision entry line to parse.</param>
        /// <returns>A populated <see cref="RevisionEntry"/> object, or null if the line is empty, whitespace, or cannot be minimally parsed.</returns>
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