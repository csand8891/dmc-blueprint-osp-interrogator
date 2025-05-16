using DmcBlueprint.Models;
using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization; 
using System.Text.RegularExpressions; 
using DmcBlueprint.Parsers.SectionParsers;

namespace DmcBlueprint.Parsers
{
    /// <summary>
    /// Parses a Software Data Management Card (DMC) file.
    /// It reads the file line by line, identifies different sections (e.g., Machine Data, Customer Data, Spec Codes),
    /// and delegates parsing of each section to specialized section parsers.
    /// </summary>
    public class DataManagementCardParser
    {
        /// <summary>
        /// Compiled regular expression to identify section headers in the DMC file.
        /// Section headers are expected to be in the format "===[ Section Name ]===".
        /// </summary>
        private static readonly Regex SectionHeaderRegex = new Regex(@"^=+\s*\[(?<name>[^\]]+)\]\s*=+$", RegexOptions.Compiled);

        /// <summary>
        /// Parses a DMC file from the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the DMC file.</param>
        /// <returns>A <see cref="SoftwareDataManagementCard"/> object populated with data from the file.</returns>
        public SoftwareDataManagementCard Parse(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            return Parse(lines);
        }

        /// <summary>
        /// Parses a DMC file from an enumerable collection of lines.
        /// This is the core parsing logic that iterates through lines and delegates to section-specific parsers.
        /// </summary>
        /// <param name="lines">An enumerable collection of strings, where each string is a line from the DMC file.</param>
        /// <returns>A <see cref="SoftwareDataManagementCard"/> object populated with data from the lines.</returns>
        public SoftwareDataManagementCard Parse(IEnumerable<string> lines)
        {
            var managementCard = new SoftwareDataManagementCard();
            _currentSection = ActiveSection.None; 

            foreach (var originalLineFromFile in lines) 
            {
                string lineAfterRomanNumeralFix = originalLineFromFile.Replace("\u2161", "II"); 
                string line = lineAfterRomanNumeralFix.Replace("\u3000", "  "); 
                string trimmedLine = line.Trim();

                bool isSpecCodeSection = _currentSection >= ActiveSection.NcSpecCode1 && _currentSection <= ActiveSection.PlcSpecCode3;
                if (string.IsNullOrWhiteSpace(trimmedLine) && !isSpecCodeSection)
                {
                    continue;
                }

                if (IsSectionHeader(trimmedLine))
                {
                    UpdateCurrentSection(trimmedLine);
                }
                else
                {
                    if (isSpecCodeSection)
                    {
                        if (line.Length == 78)
                        {
                            _specCodeParser.ParseLine(line, managementCard, _currentSection, _currentActualSectionTitle);
                        }
                        else
                        {
                            _specCodeParser.ParseLine(trimmedLine, managementCard, _currentSection, _currentActualSectionTitle);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(trimmedLine)) continue; 

                        switch (_currentSection)
                        {
                            case ActiveSection.MachineData:
                                _machineDataParser.ParseLine(trimmedLine, managementCard.MachineDetails);
                                break;
                            case ActiveSection.CustomerData:
                                _customerDataParser.ParseLine(trimmedLine, managementCard.DistributorAndCustomerDetails);
                                break;
                            case ActiveSection.Note:
                                _noteParser.ParseLine(trimmedLine, managementCard);
                                break;
                            case ActiveSection.DvdMediaVersionData:
                                _dvdMediaParser.ParseLine(trimmedLine, managementCard.DvdMediaVersions);
                                break;
                            case ActiveSection.SoftVersionExceptedOspSystemCd:
                                _softVersionParser.ParseLine(trimmedLine, managementCard.AdditionalSoftwareDvdVersions);
                                break;
                            case ActiveSection.PackageSoftComposition:
                                _packageParser.ParseLine(trimmedLine, managementCard.SoftwarePackageComposition);
                                break;
                            case ActiveSection.NcCustomSoftComposition:
                                _customSoftParser.ParseLine(trimmedLine, managementCard.CustomSoftwareComposition);
                                break;
                            case ActiveSection.None:
                                Console.WriteLine($"Warning: Unclassified line (CurrentSection is None): '{line}'");
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return managementCard;
        }

        /// <summary>
        /// Represents the different sections that can be encountered and parsed within a DMC file.
        /// </summary>
        public enum ActiveSection 
        {
            /// <summary>Indicates that no specific section is currently being parsed, or the section is unrecognized.</summary>
            None,
            /// <summary>The "[ Machine Data ]" section.</summary>
            MachineData,
            /// <summary>The "[ Customer Data ]" section.</summary>
            CustomerData,
            /// <summary>The "[ NOTE ]" section.</summary>
            Note, 
            /// <summary>The "[ DVD Media Version Data ]" section.</summary>
            DvdMediaVersionData,
            /// <summary>The "[ Soft Version Excepted OSP System CD/DVD ]" section.</summary>
            SoftVersionExceptedOspSystemCd,
            /// <summary>The "[ Package Soft composition ]" section.</summary>
            PackageSoftComposition,
            /// <summary>The "[ NC Custom Soft composition ]" section.</summary>
            NcCustomSoftComposition,
            /// <summary>The "[ NC-SPEC CODE No.1 ]" section (or NC-B SPEC CODE No.1).</summary>
            NcSpecCode1,
            /// <summary>The "[ NC-SPEC CODE No.2 ]" section.</summary>
            NcSpecCode2,
            /// <summary>The "[ NC-SPEC CODE No.3 ]" section.</summary>
            NcSpecCode3,
            /// <summary>The "[ PLC-SPEC CODE No.1 ]" section.</summary>
            PlcSpecCode1,
            /// <summary>The "[ PLC-SPEC CODE No.2 ]" section.</summary>
            PlcSpecCode2,
            /// <summary>The "[ PLC-SPEC CODE No.3 ]" section.</summary>
            PlcSpecCode3,
            
        }
        
        /// <summary>
        /// The currently active section being parsed.
        /// </summary>
        private ActiveSection _currentSection = ActiveSection.None;
        /// <summary>
        /// The exact title string of the currently active section as read from the file.
        /// </summary>
        private string _currentActualSectionTitle = ""; 

        /// <summary>
        /// Gets the current section being parsed. Used for testing purposes.
        /// </summary>
        internal ActiveSection CurrentSectionForTesting => _currentSection;

        /// <summary>Instance of the parser for the Machine Data section.</summary>
        private readonly MachineDataSectionParser _machineDataParser = new MachineDataSectionParser();
        /// <summary>Instance of the parser for the Customer Data section.</summary>
        private readonly CustomerDataSectionParser _customerDataParser = new CustomerDataSectionParser();
        /// <summary>Instance of the parser for the Note section.</summary>
        private readonly NoteSectionParser _noteParser = new NoteSectionParser();
        /// <summary>Instance of the parser for the DVD Media Version Data section.</summary>
        private readonly DvdMediaVersionDataSectionParser _dvdMediaParser = new DvdMediaVersionDataSectionParser();
        /// <summary>Instance of the parser for the Soft Version (Excepted OSP System CD/DVD) section.</summary>
        private readonly SoftVersionSectionParser _softVersionParser = new SoftVersionSectionParser();
        /// <summary>Instance of the parser for the Package Soft Composition section.</summary>
        private readonly PackageSoftCompositionSectionParser _packageParser = new PackageSoftCompositionSectionParser();
        /// <summary>Instance of the parser for the NC Custom Soft Composition section.</summary>
        private readonly NcCustomSoftCompositionSectionParser _customSoftParser = new NcCustomSoftCompositionSectionParser();
        /// <summary>Instance of the parser for all Spec Code sections (NC and PLC).</summary>
        private readonly SpecCodeSectionParser _specCodeParser = new SpecCodeSectionParser();

        /// <summary>
        /// Checks if the given line is a section header.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line matches the section header pattern, false otherwise.</returns>
        private bool IsSectionHeader(string line)
        {
            return SectionHeaderRegex.IsMatch(line);
        }

        /// <summary>
        /// Updates the current parsing section based on the provided header line.
        /// It normalizes the header title and sets the <see cref="_currentSection"/> and <see cref="_currentActualSectionTitle"/> fields.
        /// It also resets state for relevant section parsers when a new section begins.
        /// </summary>
        /// <param name="headerLine">The section header line read from the file.</param>
        private void UpdateCurrentSection(string headerLine)
        {
            var match = SectionHeaderRegex.Match(headerLine);
            ActiveSection oldSection = _currentSection;

            if (match.Success)
            {
                _currentActualSectionTitle = match.Groups["name"].Value.Trim();
                string normalizedTitle = _currentActualSectionTitle.ToUpperInvariant().Replace(" ", "").Replace("NO.", "");
                Console.WriteLine(normalizedTitle);
                switch (normalizedTitle)
                {
                    case "MACHINEDATA":
                        _currentSection = ActiveSection.MachineData;
                        break;
                    case "CUSTOMERDATA":
                        _currentSection = ActiveSection.CustomerData;
                        _customerDataParser.ResetState();
                        break;
                    case "NOTE": 
                        _currentSection = ActiveSection.Note;
                        break;
                    case "DVDMEDIAVERSIONDATA":
                        _currentSection = ActiveSection.DvdMediaVersionData;
                        _dvdMediaParser.ResetState();
                        break;
                    case "SOFTVERSIONEXCEPTEDOSPSYSTEMCD":
                        _currentSection = ActiveSection.SoftVersionExceptedOspSystemCd;
                        _softVersionParser.ResetState();
                        break;
                    case "SOFTVERSIONEXCEPTEDOSPSYSTEMCD/DVD":
                        _currentSection = ActiveSection.SoftVersionExceptedOspSystemCd;
                        _softVersionParser.ResetState();
                        break;
                    case "PACKAGESOFTCOMPOSITION":
                        _currentSection = ActiveSection.PackageSoftComposition;
                        _packageParser.ResetState();
                        break;
                    case "NCCUSTOMSOFTCOMPOSITION":
                        _currentSection = ActiveSection.NcCustomSoftComposition;
                        _customSoftParser.ResetState();
                        break;
                    case "NC-SPECCODE1":
                        _currentSection = ActiveSection.NcSpecCode1;
                        break;
                    case "NC-SPECCODE2":
                        _currentSection = ActiveSection.NcSpecCode2;
                        break;
                    case "NC-BSPECCODE1":
                        _currentSection = ActiveSection.NcSpecCode1; // NC-B Spec Code 1 maps to NcSpecCode1 enum
                        break;
                    case "NC-SPECCODE3":
                        _currentSection = ActiveSection.NcSpecCode3;
                        break;
                    case "PLC-SPECCODE1":
                        _currentSection = ActiveSection.PlcSpecCode1;
                        break;
                    case "PLC-SPECCODE2":
                        _currentSection = ActiveSection.PlcSpecCode2;
                        break;
                    case "PLC-SPECCODE3":
                        _currentSection = ActiveSection.PlcSpecCode3;
                        break;
                    default:
                        Console.WriteLine($"Warning: Unrecognized section header: '{_currentActualSectionTitle}'. Treating as 'None'.");
                        _currentSection = ActiveSection.None; 
                        break;
                }
            }
            else
            {
                _currentSection = ActiveSection.None;
                _currentActualSectionTitle = ""; 
            }

            bool newSectionIsSpecCode = _currentSection >= ActiveSection.NcSpecCode1 && _currentSection <= ActiveSection.PlcSpecCode3; 
            bool oldSectionWasDifferentSpecCodeOrNotSpecCode = oldSection != _currentSection; 

            if (newSectionIsSpecCode && oldSectionWasDifferentSpecCodeOrNotSpecCode) {
                _specCodeParser.ResetLineCounter();
            }
        }
    }
}
