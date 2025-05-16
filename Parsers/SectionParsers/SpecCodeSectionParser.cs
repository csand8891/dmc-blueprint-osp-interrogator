using DmcBlueprint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class SpecCodeSectionParser
    {
        private int _featureLineCounterInSection = 0;
        private static readonly Regex HexCodeLineRegex = new Regex(@"^[0-9A-Fa-f]{4}(?:-[0-9A-Fa-f]{4})*$", RegexOptions.Compiled);

        public void ParseLine(string line, SoftwareDataManagementCard card, DataManagementCardParser.CurrentSection currentSection, string currentActualSectionTitle)
        {
            // 'line' can be raw 78-char or pre-trimmed.
            // Console.WriteLine($"DEBUG (SpecCodeParser): ParseLine received line (length {line.Length}): '{line}' for section {currentActualSectionTitle}");

            if ((line.StartsWith("====") || line.StartsWith("----")) &&
                line.Replace(" ", "").All(c => c == '=' || c == '-'))
            {
                // Console.WriteLine("DEBUG (SpecCodeParser): Line is a separator, returning.");
                return; // Ignore separator lines
            }

            SpecCodeSection? specSection = GetOrCreateSpecCodeSection(card, currentSection, currentActualSectionTitle);
            if (specSection == null)
            {
                Console.WriteLine($"Error (SpecCodeParser): Could not get or create SpecCodeSection for: {currentActualSectionTitle}.");
                return;
            }

            // Check for footer/metadata lines before attempting feature parsing
            // These checks should also work on the raw 'line'.
            // Trim() is used for HexCodeLineRegex match because the line itself might be padded if it's a 78-char line.
            if (line.Contains("<") && line.Contains(">") &&
                (line.Contains("/") || Regex.IsMatch(line, @"\d+/\d+")) &&
                !HexCodeLineRegex.IsMatch(line.Trim())) 
            {
                // Console.WriteLine($"DEBUG (SpecCodeParser): Detected footer/metadata line, skipping feature parsing: '{line}'");
                return;
            }
            
            string trimmedLineForHexCheck = line.Trim();
            if (HexCodeLineRegex.IsMatch(trimmedLineForHexCheck))
            {
                // Console.WriteLine($"DEBUG (SpecCodeParser): Detected Hex Code Line: '{trimmedLineForHexCheck}'");
                specSection.HexCodes.Add(trimmedLineForHexCheck);
            }
            else
            {
                // Console.WriteLine($"DEBUG (SpecCodeParser): Applying feature parsing to line (length {line.Length}): \"{line}\"");

                bool featureAddedThisLine = false; 
                int successfullyParsedFeatureColumn = 0; 
                const int featureNameLength = 17;
                const int statusLength = 1;
                const int featureBlockSize = featureNameLength + statusLength; // 18
                const int columnSeparatorLength = 2;
                const string columnSeparator = "  ";

                int currentIndex = 0;
                while (currentIndex < line.Length && successfullyParsedFeatureColumn < 4) // Max 4 columns
                {
                    if (currentIndex + featureBlockSize > line.Length)
                    {
                        // Console.WriteLine($"DEBUG (SpecCodeParser): Remaining characters ({line.Length - currentIndex}) less than featureBlockSize ({featureBlockSize}). End of line or partial block: '{line.Substring(currentIndex)}'");
                        break;
                    }

                    string featureBlock = line.Substring(currentIndex, featureBlockSize);
                    string rawNamePart = featureBlock.Substring(0, featureNameLength);
                    char statusChar = featureBlock[featureNameLength]; // Character at index 17 of the block (0-indexed)

                    // Console.WriteLine($"DEBUG (SpecCodeParser): Processing block {successfullyParsedFeatureColumn}: '{featureBlock}'. Raw Name='{rawNamePart}', StatusChar='{statusChar}' at line index {currentIndex + featureNameLength}");

                    string namePart = rawNamePart.Trim();
                    bool isEnabled;

                    if (statusChar == 'o' || statusChar == '-')
                    {
                        isEnabled = (statusChar == 'o');
                        
                        int bit = _featureLineCounterInSection % 8;
                        int number = ((successfullyParsedFeatureColumn * 8) + 8) - (_featureLineCounterInSection / 8) ;


                        // Console.WriteLine($"DEBUG (SpecCodeParser):     Adding SpecFeature: Name='{namePart}', Enabled={isEnabled}, No={number}, Bit={bit} (Col {successfullyParsedFeatureColumn})");
                        specSection.SpecCodes.Add(new SpecFeature(namePart, isEnabled, number, bit));
                        featureAddedThisLine = true; 
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(rawNamePart)) 
                        {
                             Console.WriteLine($"DEBUG (SpecCodeParser): FAILED PARSE for feature block: RawName='{rawNamePart}' found, but statusChar='{statusChar}' is invalid. Full line: '{line}'");
                        }
                        else
                        {
                             Console.WriteLine($"DEBUG (SpecCodeParser): Invalid status char '{statusChar}' in an otherwise empty/whitespace block. RawName='{rawNamePart}'. Skipping this block for feature addition.");
                        }
                    }
                    
                    successfullyParsedFeatureColumn++;
                    currentIndex += featureBlockSize;

                    if (successfullyParsedFeatureColumn < 4) 
                    {
                        if (currentIndex + columnSeparatorLength <= line.Length &&
                            line.Substring(currentIndex, columnSeparatorLength) == columnSeparator)
                        {
                            // Console.WriteLine($"DEBUG (SpecCodeParser): Consumed separator '{columnSeparator}' at index {currentIndex}");
                            currentIndex += columnSeparatorLength;
                        }
                        else if (currentIndex < line.Length) 
                        {
                            // Console.WriteLine($"DEBUG (SpecCodeParser): Expected separator '{columnSeparator}' after column {successfullyParsedFeatureColumn - 1} but found '{line.Substring(currentIndex, Math.Min(columnSeparatorLength, line.Length - currentIndex))}'.");
                            break; 
                        }
                    }
                }
                // Console.WriteLine($"DEBUG (SpecCodeParser): Line processed. Successfully parsed feature columns this line: {successfullyParsedFeatureColumn}.");

                if (featureAddedThisLine)
                {
                    _featureLineCounterInSection++;
                }
            }
         }

        private SpecCodeSection? GetOrCreateSpecCodeSection(SoftwareDataManagementCard card, DataManagementCardParser.CurrentSection currentSection, string sectionTitle)
        {
            if (string.IsNullOrEmpty(sectionTitle)) return null;

            List<SpecCodeSection> targetList;
            if (currentSection >= DataManagementCardParser.CurrentSection.NcSpecCode1 && currentSection <= DataManagementCardParser.CurrentSection.NcSpecCode3)
            {
                targetList = card.NcSpecCodes;
            }
            else if (currentSection >= DataManagementCardParser.CurrentSection.PlcSpecCode1 && currentSection <= DataManagementCardParser.CurrentSection.PlcSpecCode3)
            {
                targetList = card.PlcSpecCodes;
            }
            else
            {
                return null; 
            }

            SpecCodeSection? specSection = targetList.FirstOrDefault(s => s.SectionTitle == sectionTitle);
            if (specSection == null)
            {
                specSection = new SpecCodeSection { SectionTitle = sectionTitle };
                targetList.Add(specSection);
            }
            return specSection;
        }

        public void ResetLineCounter() => _featureLineCounterInSection = 0;
    }
}
