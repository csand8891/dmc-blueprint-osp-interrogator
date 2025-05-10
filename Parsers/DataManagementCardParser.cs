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

        private static readonly Regex NameAndStatusRegex = new Regex(@"^(.*?)\s+([o-])\s*$", RegexOptions.Compiled);
        private static readonly Regex StatusOnlyRegex = new Regex(@"^\s*([o-])\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Parses the specified Data Management Card file and returns a structured representation.
        /// </summary>
        /// <param name="filePath">The full path to the Data Management Card file.</param>
        /// <returns>A <see cref="SoftwareDataManagementCard"/> object populated with data from the file.</returns>
        public SoftwareDataManagementCard Parse(string filePath)
        {
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
                    _currentMachineDataKey = null; // Reset when a new section starts
                    _currentCustomerEntityType = CustomerEntityType.Distributor; // Default to Distributor when section changes
                    _currentCustomerDataKey = null;
                    _currentDvdMediaKey = null; // Reset when a new section starts
                    _currentSoftVersionKey = null; // Reset when a new section starts
                    _currentPackageName = null; // Reset when a new section starts
                    _currentCustomSoftGroup = null; // Reset when a new section starts or for a new group
                }
                else
                {
                    // This is a data line, process it based on the _currentSection
                    switch (_currentSection)
                    {
                        case CurrentSection.MachineData:
                            ParseMachineDataLine(trimmedLine, managementCard.MachineDetails);
                            break;
                        case CurrentSection.CustomerData:
                            ParseCustomerDataLine(trimmedLine, managementCard.DistributorAndCustomerDetails);
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
                            ParseDvdMediaVersionDataLine(trimmedLine, managementCard.DvdMediaVersions);
                            break;
                        case CurrentSection.SoftVersionExceptedOspSystemCd:
                            ParseSoftVersionIdentifierLine(trimmedLine, managementCard.AdditionalSoftwareDvdVersions);
                            break;
                        case CurrentSection.PackageSoftComposition:
                            ParsePackageSoftCompositionLine(trimmedLine, managementCard.SoftwarePackageComposition);
                            break;
                        case CurrentSection.NcCustomSoftComposition:
                            ParseNcCustomSoftCompositionLine(trimmedLine, managementCard.CustomSoftwareComposition);
                            break;
                        case CurrentSection.NcSpecCode1:
                            ParseSpecCodeLine(trimmedLine, managementCard);
                            break;
                        case CurrentSection.NcSpecCode2:
                            ParseSpecCodeLine(trimmedLine, managementCard);
                            break;
                        case CurrentSection.NcSpecCode3:
                            ParseSpecCodeLine(trimmedLine, managementCard);
                            break;
                        case CurrentSection.PlcSpecCode1:
                            ParseSpecCodeLine(trimmedLine, managementCard);
                            break;
                        case CurrentSection.PlcSpecCode2:
                            ParseSpecCodeLine(trimmedLine, managementCard);
                            break;
                        case CurrentSection.PlcSpecCode3:
                            ParseSpecCodeLine(trimmedLine, managementCard);
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

        private enum CustomerEntityType
        {
            Distributor,
            EndCustomer
        }

        // --- State Variables ---

        private CurrentSection _currentSection = CurrentSection.None;
        private CustomSoftwareGroup? _currentCustomSoftGroup = null; // Changed type to CustomSoftwareGroup
        private string? _currentMachineDataKey = null; // State for parsing MachineData

        private CustomerEntityType _currentCustomerEntityType = CustomerEntityType.Distributor;
        private string? _currentCustomerDataKey = null; // State for parsing CustomerData (e.g., Name, Address, Phone)
        private string? _currentDvdMediaKey = null; // State for parsing DvdMediaVersionData
        private string? _currentSoftVersionKey = null; // State for parsing SoftwareVersionIdentifier
        private string? _currentPackageName = null; // State for parsing PackageSoftComposition
        private int _featureLineCounterInSection = 0; // To track line number for No./Bit calculation in spec codes
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
            
            CurrentSection oldSection =  _currentSection; // Store old section before update

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
            // Reset feature line counter if we are entering a new spec code section
            // or changing from one spec code section to another.
            bool newSectionIsSpecCode = _currentSection >= CurrentSection.NcSpecCode1 && _currentSection <= CurrentSection.PlcSpecCode3;
            bool oldSectionWasDifferentSpecCodeOrNotSpecCode = oldSection != _currentSection;

            if (newSectionIsSpecCode && oldSectionWasDifferentSpecCodeOrNotSpecCode) {
                _featureLineCounterInSection = 0;
            }
        }

        /// <summary>
        /// Parses a line within the "MachineData" section.
        /// It identifies keys (e.g., "&lt;Type of OSP&gt;") and their corresponding values.
        /// </summary>
        /// <param name="line">The line from the MachineData section to parse.</param>
        /// <param name="machineDetails">The <see cref="MachineIdentifier"/> object to populate.</param>
        internal void ParseMachineDataLine(string line, MachineIdentifier machineDetails)
        {
            // Note: The input 'line' to this function is already trimmed by the main Parse loop.
            string valueCandidate = line.Trim();

            if (line.TrimStart().StartsWith("<") && line.TrimEnd().EndsWith(">")) // Check original line for key structure
            {
                
                _currentMachineDataKey = line.Trim().Substring(1, line.Trim().Length - 2).Trim();
                // If the key implies a list that might be re-populated, clear it.
                if (_currentMachineDataKey == "Type of Machine")
                {
                    machineDetails.MachineType.Clear(); 
                }
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentMachineDataKey != null)
            {
                // This is a value line for the current key
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
                            Console.WriteLine($"Warning: Could not parse Software Production Date '{valueCandidate}' in MachineData section. It will retain its default value.");
                        }
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '{_currentMachineDataKey}' in MachineData section.");
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a line within the "CustomerData" section.
        /// It identifies keys (e.g., "&lt;Name&gt;", "&lt;Address&gt;", "&lt;Phone&gt;", or "&lt;Customer&gt;")
        /// and their corresponding values, assigning them to the correct Distributor or EndCustomer contact.
        /// </summary>
        /// <param name="line">The line from the CustomerData section to parse.</param>
        /// <param name="customerInfo">The <see cref="DistributorAndCustomerInfo"/> object to populate.</param>
        internal void ParseCustomerDataLine(string line, DistributorAndCustomerInfo customerInfo)
        {
            string valueCandidate = line.Trim();

            if (line.TrimStart().StartsWith("<") && line.TrimEnd().EndsWith(">")) // Check original line for key structure
            {
                // This is a key line
                string key = line.Trim().Substring(1, line.Trim().Length - 2).Trim();
                if (key == "Customer")
                {
                    _currentCustomerEntityType = CustomerEntityType.EndCustomer;
                    _currentCustomerDataKey = null; // Reset key when switching entity
                }
                else
                {
                    _currentCustomerDataKey = key;
                }
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentCustomerDataKey != null)
            {
                // This is a value line for the current key and entity
                ContactEntry currentContact = _currentCustomerEntityType == CustomerEntityType.Distributor
                    ? customerInfo.Distributor
                    : customerInfo.EndCustomer;

                switch (_currentCustomerDataKey)
                {
                    case "Name":
                        currentContact.Name = valueCandidate;
                        break;
                    case "Address":
                        currentContact.Address = valueCandidate;
                        break;
                    case "Phone":
                        currentContact.Phone = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '{_currentCustomerDataKey}' in CustomerData section for entity '{_currentCustomerEntityType}'.");
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a line within the "DvdMediaVersionData" section.
        /// It identifies keys (e.g., "[Windows System CD Version]") and their corresponding values.
        /// </summary>
        /// <param name="line">The line from the DvdMediaVersionData section to parse.</param>
        /// <param name="dvdMediaData">The <see cref="DvdMediaVersionData"/> object to populate.</param>
        internal void ParseDvdMediaVersionDataLine(string line, DvdMediaVersionData dvdMediaData)
        {
            string valueCandidate = line.Trim();

            // Keys in this section are like "[Windows System CD Version]"
            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                // This is a key line
                _currentDvdMediaKey = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentDvdMediaKey != null)
            {
                // This is a value line for the current key
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
                // Optional: Reset _currentDvdMediaKey after processing a value if each key only has one value line.
                // If a key could have multiple value lines (though not apparent from the example), do not reset here.
                // _currentDvdMediaKey = null; 
            }
        }

        /// <summary>
        /// Parses a line within the "Soft Version Excepted OSP System CD" section.
        /// It identifies keys (e.g., "[Windows System Version]") and their corresponding values.
        /// </summary>
        /// <param name="line">The line from the "Soft Version Excepted OSP System CD" section to parse.</param>
        /// <param name="softVersions">The <see cref="SoftwareVersionIdentifier"/> object to populate.</param>
        internal void ParseSoftVersionIdentifierLine(string line, SoftwareVersionIdentifier softVersions)
        {
            string valueCandidate = line.Trim();

            // Keys in this section are like "[Windows System Version]"
            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                // This is a key line
                _currentSoftVersionKey = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentSoftVersionKey != null)
            {
                // This is a value line for the current key
                switch (_currentSoftVersionKey)
                {
                    case "Windows System Version":
                        softVersions.WindowsSystemVersion = valueCandidate;
                        break;
                    case "Custom API Additional DVD Version":
                        softVersions.ApiDvdVersion = valueCandidate;
                        break;
                    case "MTconnect Version@(Included in App_THINC_API DVD)": // Note: Special character might need handling or exact match
                    case "MTconnect Version": // Adding a fallback in case the special character is problematic
                        softVersions.MtConnectVersion = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '[{_currentSoftVersionKey}]' in SoftVersionExceptedOspSystemCd section.");
                        break;
                }
                // Optional: Reset _currentSoftVersionKey after processing a value if each key only has one value line.
                // For this section, it seems each key has one value.
                // _currentSoftVersionKey = null; 
            }
        }

        /// <summary>
        /// Parses a line within the "Package Soft composition" section.
        /// It identifies package names (e.g., "[NC INSTALLER]") and their corresponding identifiers on the next line.
        /// </summary>
        /// <param name="line">The line from the "Package Soft composition" section to parse.</param>
        /// <param name="packageList">The list of <see cref="SoftwarePackage"/> objects to populate.</param>
        internal void ParsePackageSoftCompositionLine(string line, List<SoftwarePackage> packageList)
        {
            string valueCandidate = line.Trim();

            // Package names are like "[NC INSTALLER]"
            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                // This is a package name line
                _currentPackageName = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentPackageName != null)
            {
                // This is an identifier line for the current package name
                packageList.Add(new SoftwarePackage
                {
                    PackageName = _currentPackageName,
                    Identifier = valueCandidate
                });
                _currentPackageName = null; // Reset after capturing the identifier, as each package name has one identifier line
            }
            // If it's a blank line between package name and identifier, or after an identifier, it will be skipped
            // by the IsNullOrWhiteSpace check or because _currentPackageName is null.
        }

        /// <summary>
        /// Parses a line within the "NC Custom Soft composition" section.
        /// It identifies group names (e.g., "[LPP]") and their corresponding file paths.
        /// </summary>
        /// <param name="line">The line from the "NC Custom Soft composition" section to parse.</param>
        /// <param name="customSoftwareList">The list of <see cref="CustomSoftwareGroup"/> objects to populate.</param>
        internal void ParseNcCustomSoftCompositionLine(string line, List<CustomSoftwareGroup> customSoftwareList)
        {
            string valueCandidate = line.Trim();

            // Group names are like "[LPP]"
            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                // This is a new group name line
                string groupName = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
                _currentCustomSoftGroup = new CustomSoftwareGroup { GroupName = groupName };
                customSoftwareList.Add(_currentCustomSoftGroup);
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentCustomSoftGroup != null)
            {
                // This is a file path line for the current group
                _currentCustomSoftGroup.FilePaths.Add(valueCandidate);
            }
            // If it's a blank line, it will be skipped by the IsNullOrWhiteSpace check.
            // If a file path appears before a group name, _currentCustomSoftGroup would be null,
            // and the line would be ignored (which is the desired behavior for this structure).
        }

        /// <summary>
        /// Parses a line within one of the NC-SPEC or PLC-SPEC CODE sections.
        /// It identifies hex code lines and spec feature lines, populating the appropriate
        /// <see cref="SpecCodeSection"/> object in the <see cref="SoftwareDataManagementCard"/>.
        /// </summary>
        /// <param name="line">The line from the spec code section to parse.</param>
        /// <param name="card">The <see cref="SoftwareDataManagementCard"/> to populate.</param>
        internal void ParseSpecCodeLine(string line, SoftwareDataManagementCard card)
        {
            Console.WriteLine($"DEBUG: ParseSpecCodeLine received line: '{line}'"); // Diagnostic
            string trimmedLine = line.Trim(); // Caller already trims, but good for safety.
            if (string.IsNullOrWhiteSpace(trimmedLine))            {
                Console.WriteLine("DEBUG: Line is null or whitespace, returning."); // Diagnostic
                return;
            }
            
            // Check if the line is a separator line (e.g., "==== ... ====" or "---- ... ----")
            if ((trimmedLine.StartsWith("====") || trimmedLine.StartsWith("----")) &&
                trimmedLine.Replace(" ", "").All(c => c == '=' || c == '-'))
            {
                Console.WriteLine("DEBUG: Line is a separator, returning."); // Diagnostic
                return; // Ignore separator lines
            }

            SpecCodeSection? currentSpecSection = GetOrCreateSpecCodeSection(card);
            if (currentSpecSection == null)
            {
                Console.WriteLine($"Error: Could not get or create SpecCodeSection for current section: {_currentSection}. DEBUG: currentSpecSection is null, returning."); // Diagnostic
                return;
            }
            Console.WriteLine($"DEBUG: currentSpecSection title: {currentSpecSection.SectionTitle}"); // Diagnostic
            // Split the line into feature entries. The delimiter appears to be "  " (two spaces).
            // string[] featureEntries = trimmedLine.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
            // Console.WriteLine($"DEBUG: featureEntries count: {featureEntries.Length}"); // Diagnostic

            Regex hexCodeLineRegex = new Regex(@"^[0-9A-Fa-f]{4}(?:-[0-9A-Fa-f]{4})*$"); // Or more specific

            // Check for footer/metadata lines before attempting feature parsing
            if (trimmedLine.Contains("<") && trimmedLine.Contains(">") &&
                (trimmedLine.Contains("/") || Regex.IsMatch(trimmedLine, @"\d+/\d+")) &&
                !hexCodeLineRegex.IsMatch(trimmedLine) && 
                !((trimmedLine.StartsWith("====") || trimmedLine.StartsWith("----")) && trimmedLine.Replace(" ", "").All(c => c == '=' || c == '-')))
            {
                Console.WriteLine($"DEBUG: Detected footer/metadata line, skipping feature parsing: '{trimmedLine}'");
                return;
            }
            else if (hexCodeLineRegex.IsMatch(trimmedLine))
            {
                Console.WriteLine($"DEBUG: Detected Hex Code Line: '{trimmedLine}'");
                currentSpecSection.HexCodes.Add(trimmedLine);
            }
            else
            {
                // Diagnostic: If it wasn't a hex line, but looks like it might have been, print char codes
                if (trimmedLine.Contains("-") && trimmedLine.Length > 10 && (trimmedLine.All(c => Uri.IsHexDigit(c) || c == '-')))
                {
                    Console.WriteLine($"DEBUG: Line '{trimmedLine}' was NOT matched by hex regex. Char codes: {string.Join(", ", trimmedLine.Select(c => (int)c))}");
                }
                Console.WriteLine($"DEBUG: Applying feature parsing to trimmedLine (length {trimmedLine.Length}): \"{trimmedLine}\"");

                string[] segments = trimmedLine.Split(new[] { "  " }, StringSplitOptions.None);

                int successfullyParsedFeatureColumn = 0; // Tracks the visual column of successfully parsed features


                for (int i = 0; i < segments.Length; /* i is incremented inside loop */)
                {
                    string currentSegmentRaw = segments[i];
                    string currentSegmentTrimmed = currentSegmentRaw.Trim();

                    if (string.IsNullOrWhiteSpace(currentSegmentTrimmed))
                    {
                        i++;
                        continue;
                    }

                    string namePart = null;
                    string statusString = null;
                    int segmentsConsumedThisFeature = 0;

                    Console.WriteLine($"DEBUG: Processing segment {i}: Raw='{currentSegmentRaw}', Trimmed='{currentSegmentTrimmed}'");

                    // Case 1: Current segment is "Name o/-"
                    Match fullNameStatusMatch = NameAndStatusRegex.Match(currentSegmentTrimmed);
                    if (fullNameStatusMatch.Success)
                    {
                        namePart = fullNameStatusMatch.Groups[1].Value.Trim();
                        statusString = fullNameStatusMatch.Groups[2].Value;
                        segmentsConsumedThisFeature = 1;
                        Console.WriteLine($"DEBUG:   Case 1: Parsed from single segment: Name='{namePart}', Status='{statusString}'");
                    }
                    else
                    {
                        // Current segment is not "Name o/-". It could be a Name, or just a Status.
                        string potentialNameFromNameSegment = currentSegmentTrimmed;

                        // Case 2: Current segment is Name, next segment is Status "o/-"
                        if (i + 1 < segments.Length)
                        {
                            string nextSegmentTrimmedLookahead = segments[i + 1].Trim();
                            Match statusMatch = StatusOnlyRegex.Match(nextSegmentTrimmedLookahead);
                            if (statusMatch.Success)
                            {
                                namePart = potentialNameFromNameSegment;
                                statusString = statusMatch.Groups[1].Value;
                                segmentsConsumedThisFeature = 2;
                                Console.WriteLine($"DEBUG:   Case 2: Name='{namePart}', NextSegmentStatus='{statusString}'");
                            }
                            // Case 2.5: Current segment is Name, next is whitespace, segment after is Status "o/-"
                            else if (string.IsNullOrWhiteSpace(nextSegmentTrimmedLookahead) && i + 2 < segments.Length)
                            {
                                string thirdSegmentTrimmedLookahead = segments[i + 2].Trim();
                                Match thirdStatusMatch = StatusOnlyRegex.Match(thirdSegmentTrimmedLookahead);
                                if (thirdStatusMatch.Success)
                                {
                                    namePart = potentialNameFromNameSegment;
                                    statusString = thirdStatusMatch.Groups[1].Value;
                                    segmentsConsumedThisFeature = 3;
                                    Console.WriteLine($"DEBUG:   Case 2.5: Name='{namePart}', Whitespace, ThenStatus='{statusString}'");
                                }
                            }
                        }

                        // If no status found by lookahead, current segment might be just a Name, or just a Status.
                        if (segmentsConsumedThisFeature == 0)
                        {
                            Match loneStatusMatch = StatusOnlyRegex.Match(potentialNameFromNameSegment);
                            if (loneStatusMatch.Success)
                            {
                                // Case 0: Current segment is just "o" or "-" (a nameless feature)
                                namePart = ""; 
                                statusString = loneStatusMatch.Groups[1].Value;
                                segmentsConsumedThisFeature = 1;
                                Console.WriteLine($"DEBUG:   Case 0: Nameless feature from single segment: Status='{statusString}'");
                            }
                            else
                            {
                                // Case 3/4: Current segment is a Name without a paired status found by above logic.
                                namePart = potentialNameFromNameSegment;
                                statusString = null; // Explicitly null
                                segmentsConsumedThisFeature = 1; // Consume this segment
                                Console.WriteLine($"DEBUG:   Case 3/4: Name segment '{namePart}' with no status found through lookahead.");
                            }
                        }
                    }

                    i += segmentsConsumedThisFeature; // Advance by however many segments were processed for this feature attempt

                    if (namePart != null && statusString != null)
                    {
                        char statusChar = statusString[0];
                        bool isEnabled = (statusChar == 'o');

                        int bit = _featureLineCounterInSection % 8;
                        int number = ((successfullyParsedFeatureColumn + 1) * 8) - (_featureLineCounterInSection / 8);

                        Console.WriteLine($"DEBUG:     Adding SpecFeature: Name='{namePart}', Enabled={isEnabled}, No={number}, Bit={bit} (Col {successfullyParsedFeatureColumn})");
                        currentSpecSection.SpecCodes.Add(new SpecFeature(namePart, isEnabled, number, bit));
                        successfullyParsedFeatureColumn++;
                    }
                    else if (namePart != null && !string.IsNullOrEmpty(namePart.Trim())) // Name was found (and isn't empty), but no status
                    {
                        Console.WriteLine($"DEBUG: FAILED PARSE for feature: Name='{namePart}' found, but no status. Full line: '{trimmedLine}'");
                    }
                }

                Console.WriteLine($"DEBUG: Line processed. Successfully parsed feature columns this line: {successfullyParsedFeatureColumn}.");
                // Only increment the line counter if the line was meant for features (i.e., not a hex line and successfully parsed at least one column or was identified as a feature line)
                // The initial check for hex lines already prevents increment for those.
                // If featuresFoundThisLine is 0 but successfullyParsedFeatureColumn is also 0, it means the line might have been empty or only contained unparseable segments.
                // We should increment if it was a line *intended* for features.
                // A simple proxy: if we attempted to parse features (i.e., not a hex/separator/footer line), increment the counter.
                _featureLineCounterInSection++;
            } 
        }

        internal SpecCodeSection? GetOrCreateSpecCodeSection(SoftwareDataManagementCard card)
        {
            string sectionTitle = GetTitleForCurrentSpecSection();
            if (string.IsNullOrEmpty(sectionTitle)) return null;

            List<SpecCodeSection> targetList;
            if (_currentSection >= CurrentSection.NcSpecCode1 && _currentSection <= CurrentSection.NcSpecCode3)
            {
                targetList = card.NcSpecCodes;
            }
            else if (_currentSection >= CurrentSection.PlcSpecCode1 && _currentSection <= CurrentSection.PlcSpecCode3)
            {
                targetList = card.PlcSpecCodes;
            }
            else
            {
                return null; // Should not happen if called from ParseSpecCodeLine context
            }

            SpecCodeSection? specSection = targetList.FirstOrDefault(s => s.SectionTitle == sectionTitle);
            if (specSection == null)
            {
                specSection = new SpecCodeSection { SectionTitle = sectionTitle };
                targetList.Add(specSection);
            }
            return specSection;
        }

        internal string GetTitleForCurrentSpecSection()
        {
            // This maps the enum to the exact string found in the section headers.
            bool isNcSpec = _currentSection >= CurrentSection.NcSpecCode1 && _currentSection <= CurrentSection.NcSpecCode3;
            bool isPlcSpec = _currentSection >= CurrentSection.PlcSpecCode1 && _currentSection <= CurrentSection.PlcSpecCode3;

            if (!isNcSpec && !isPlcSpec)
            {
                return string.Empty; // Not a spec code section
            }
            return _currentSection.ToString().Replace("NcSpecCode", "NC-SPEC CODE No.").Replace("PlcSpecCode", "PLC-SPEC CODE No.");
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

            string? salesOrderNum = null;
            string? projectNum = null;

            for (int i = 2; i < parts.Length; i++) // Start searching after potential ID and Date parts
            {
                if (salesOrderNum == null) // Only find the first occurrence
                {
                    if (parts[i] == "SO#" && i + 1 < parts.Length && !parts[i+1].StartsWith("P#"))
                    {
                        salesOrderNum = "SO#" + parts[i + 1];
                        i++; // Consume the number part
                    }
                    else if (parts[i].StartsWith("SO#") && parts[i].Length > 3) // Handles SO#12345
                    {
                        salesOrderNum = parts[i];
                    }
                }

                if (projectNum == null) // Only find the first occurrence
                {
                    if (parts[i] == "P#" && i + 1 < parts.Length && !parts[i+1].StartsWith("SO#"))
                    {
                        projectNum = "P#" + parts[i + 1];
                        i++; // Consume the number part
                    }
                    else if (parts[i].StartsWith("P#") && parts[i].Length > 2) // Handles P#12345
                    {
                        projectNum = parts[i];
                    }
                }
            }
            entry.SalesOrderNumber = salesOrderNum;
            entry.ProjectNumber = projectNum;
            
            return entry;
        }
    }
}