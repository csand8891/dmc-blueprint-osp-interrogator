using DmcBlueprint.Models;
using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization; // Retained for potential future use, though not directly used by this class now
using System.Text.RegularExpressions; // Retained for SectionHeaderRegex
using DmcBlueprint.Parsers.SectionParsers;

namespace DmcBlueprint.Parsers
{
    public class DataManagementCardParser
    {
        private static readonly Regex SectionHeaderRegex = new Regex(@"^=+\s*\[(?<name>[^\]]+)\]\s*=+$", RegexOptions.Compiled);

        public SoftwareDataManagementCard Parse(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            return Parse(lines);
        }

        public SoftwareDataManagementCard Parse(IEnumerable<string> lines)
        {
            var managementCard = new SoftwareDataManagementCard();
            _currentSection = CurrentSection.None; // Initialize at the start of parsing

            foreach (var originalLineFromFile in lines) 
            {
                string lineAfterRomanNumeralFix = originalLineFromFile.Replace("\u2161", "II"); 
                string line = lineAfterRomanNumeralFix.Replace("\u3000", "  "); 
                string trimmedLine = line.Trim();

                bool isSpecCodeSection = _currentSection >= CurrentSection.NcSpecCode1 && _currentSection <= CurrentSection.PlcSpecCode3;
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
                            case CurrentSection.MachineData:
                                _machineDataParser.ParseLine(trimmedLine, managementCard.MachineDetails);
                                break;
                            case CurrentSection.CustomerData:
                                _customerDataParser.ParseLine(trimmedLine, managementCard.DistributorAndCustomerDetails);
                                break;
                            case CurrentSection.Note:
                                _noteParser.ParseLine(trimmedLine, managementCard);
                                break;
                            case CurrentSection.DvdMediaVersionData:
                                _dvdMediaParser.ParseLine(trimmedLine, managementCard.DvdMediaVersions);
                                break;
                            case CurrentSection.SoftVersionExceptedOspSystemCd:
                                _softVersionParser.ParseLine(trimmedLine, managementCard.AdditionalSoftwareDvdVersions);
                                break;
                            case CurrentSection.PackageSoftComposition:
                                _packageParser.ParseLine(trimmedLine, managementCard.SoftwarePackageComposition);
                                break;
                            case CurrentSection.NcCustomSoftComposition:
                                _customSoftParser.ParseLine(trimmedLine, managementCard.CustomSoftwareComposition);
                                break;
                            case CurrentSection.None:
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
        
        private CurrentSection _currentSection = CurrentSection.None;
        private string _currentActualSectionTitle = ""; 

        internal CurrentSection CurrentSectionForTesting => _currentSection;

        private readonly MachineDataSectionParser _machineDataParser = new MachineDataSectionParser();
        private readonly CustomerDataSectionParser _customerDataParser = new CustomerDataSectionParser();
        private readonly NoteSectionParser _noteParser = new NoteSectionParser();
        private readonly DvdMediaVersionDataSectionParser _dvdMediaParser = new DvdMediaVersionDataSectionParser();
        private readonly SoftVersionSectionParser _softVersionParser = new SoftVersionSectionParser();
        private readonly PackageSoftCompositionSectionParser _packageParser = new PackageSoftCompositionSectionParser();
        private readonly NcCustomSoftCompositionSectionParser _customSoftParser = new NcCustomSoftCompositionSectionParser();
        private readonly SpecCodeSectionParser _specCodeParser = new SpecCodeSectionParser();

        private bool IsSectionHeader(string line)
        {
            return SectionHeaderRegex.IsMatch(line);
        }

        private void UpdateCurrentSection(string headerLine)
        {
            var match = SectionHeaderRegex.Match(headerLine);
            CurrentSection oldSection = _currentSection;

            if (match.Success)
            {
                _currentActualSectionTitle = match.Groups["name"].Value.Trim();
                string normalizedTitle = _currentActualSectionTitle.ToUpperInvariant().Replace(" ", "").Replace("NO.", "");
                Console.WriteLine(normalizedTitle);
                switch (normalizedTitle)
                {
                    case "MACHINEDATA":
                        _currentSection = CurrentSection.MachineData;
                        // _machineDataParser.ResetState(); // Assuming MachineDataParser has a ResetState or handles it internally
                        break;
                    case "CUSTOMERDATA":
                        _currentSection = CurrentSection.CustomerData;
                        _customerDataParser.ResetState();
                        break;
                    case "NOTE": 
                        _currentSection = CurrentSection.Note;
                        break;
                    case "DVDMEDIAVERSIONDATA":
                        _currentSection = CurrentSection.DvdMediaVersionData;
                        _dvdMediaParser.ResetState();
                        break;
                    case "SOFTVERSIONEXCEPTEDOSPSYSTEMCD":
                        _currentSection = CurrentSection.SoftVersionExceptedOspSystemCd;
                        _softVersionParser.ResetState();
                        break;
                    case "SOFTVERSIONEXCEPTEDOSPSYSTEMCD/DVD":
                    
                    case "PACKAGESOFTCOMPOSITION":
                        _currentSection = CurrentSection.PackageSoftComposition;
                        _packageParser.ResetState();
                        break;
                    case "NCCUSTOMSOFTCOMPOSITION":
                        _currentSection = CurrentSection.NcCustomSoftComposition;
                        _customSoftParser.ResetState();
                        break;
                    case "NC-SPECCODE1":
                        _currentSection = CurrentSection.NcSpecCode1;
                        break;
                    case "NC-SPECCODE2":
                        _currentSection = CurrentSection.NcSpecCode2;
                        break;
                    case "NC-BSPECCODE1":
                        _currentSection = CurrentSection.NcSpecCode1;
                        break;
                    case "NC-SPECCODE3":
                        _currentSection = CurrentSection.NcSpecCode3;
                        break;
                    case "PLC-SPECCODE1":
                        _currentSection = CurrentSection.PlcSpecCode1;
                        break;
                    case "PLC-SPECCODE2":
                        _currentSection = CurrentSection.PlcSpecCode2;
                        break;
                    case "PLC-SPECCODE3":
                        _currentSection = CurrentSection.PlcSpecCode3;
                        break;
                    default:
                        Console.WriteLine($"Warning: Unrecognized section header: '{_currentActualSectionTitle}'. Treating as 'None'.");
                        _currentSection = CurrentSection.None; 
                        break;
                }
            }
            else
            {
                _currentSection = CurrentSection.None;
                _currentActualSectionTitle = ""; 
            }

            bool newSectionIsSpecCode = _currentSection >= CurrentSection.NcSpecCode1 && _currentSection <= CurrentSection.PlcSpecCode3;
            bool oldSectionWasDifferentSpecCodeOrNotSpecCode = oldSection != _currentSection;

            if (newSectionIsSpecCode && oldSectionWasDifferentSpecCodeOrNotSpecCode) {
                _specCodeParser.ResetLineCounter();
            }
        }
    }
}
