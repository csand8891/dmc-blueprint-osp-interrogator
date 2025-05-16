using DmcBlueprint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses sections of a DMC file that contain specification codes (e.g., NC-SPEC, PLC-SPEC).
    /// These sections typically list features, their enabled/disabled status, and associated hexadecimal codes.
    /// The parser handles fixed-width feature blocks and potential variations in formatting.
    /// </summary>
    internal class SpecCodeSectionParser
    {
        /// <summary>
        /// Checks if the given character is a valid status character.
        /// 'o' typically indicates an enabled feature, and '-' indicates a disabled feature.
        /// </summary>
        /// <param name="c">The character to validate.</param>
        /// <returns>True if the character is 'o' or '-', false otherwise.</returns>
        private bool IsValidStatusChar(char c)
        {
            return c == 'o' || c == '-';
        }
        
        private int _featureLineCounterInSection = 0;
        private static readonly Regex HexCodeLineRegex = new Regex(@"^[0-9A-Fa-f]{4}(?:-[0-9A-Fa-f]{4})*$", RegexOptions.Compiled);

        /// <summary>
        /// Parses a single line from a specification code section of a DMC file.
        /// It determines if the line contains feature definitions, hexadecimal codes, or is a separator/footer,
        /// and updates the provided <see cref="SoftwareDataManagementCard"/> accordingly.
        /// </summary>
        /// <param name="line">The line of text to parse from the DMC file.</param>
        /// <param name="card">The <see cref="SoftwareDataManagementCard"/> object to populate with parsed data.</param>
        /// <param name="currentSection">An enum value indicating the type of the current section being parsed (e.g., NcSpecCode1, PlcSpecCode2).</param>
        /// <param name="currentActualSectionTitle">The exact title string of the section as read from the file (e.g., "NC-SPEC CODE No.1").</param>
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
                    string rawNamePartForParsing = featureBlock.Substring(0, featureNameLength); // Tentative name part
                    char statusCharForParsing = featureBlock[featureNameLength];                 // Tentative status char (char at index 17 of block)

                    // Console.WriteLine($"DEBUG (SpecCodeParser): Processing block {successfullyParsedFeatureColumn}: '{featureBlock}'. Initial RawName='{rawNamePartForParsing}', Initial StatusChar='{statusCharForParsing}' at line index {currentIndex + featureNameLength}");

                    bool wasRecovered = false;
                    int actualBlockConsumedIncludingStatus = featureBlockSize; // Default: 17 name + 1 status

                    // Attempt recovery if the initially parsed statusChar is not valid
                    // and a valid status char exists immediately after the 18-char block.
                    if (!IsValidStatusChar(statusCharForParsing) && (currentIndex + featureBlockSize < line.Length))
                    {
                        char charAfterBlock = line[currentIndex + featureBlockSize];
                        if (IsValidStatusChar(charAfterBlock))
                        {
                            statusCharForParsing = charAfterBlock; // Use the recovered status char
                            // rawNamePartForParsing remains the 17 chars from the original featureBlock.
                            // The name part will be trimmed from this rawNamePartForParsing.
                            wasRecovered = true;
                            actualBlockConsumedIncludingStatus = featureBlockSize + 1; // 18 for block + 1 for recovered status
                            // Console.WriteLine($"DEBUG (SpecCodeParser):     RECOVERED status char '{statusCharForParsing}' from char after 18-char block. Original RawName='{rawNamePartForParsing}', Original StatusCharInBlock='{featureBlock[featureNameLength]}'");
                        }
                    }

                    string namePart = rawNamePartForParsing.Trim();
                    bool isEnabled;

                    if (IsValidStatusChar(statusCharForParsing))
                    {
                        isEnabled = (statusCharForParsing == 'o');
                        
                        int bit = _featureLineCounterInSection % 8;
                        int number = ((successfullyParsedFeatureColumn * 8) + 8) - (_featureLineCounterInSection / 8) ;

                        // Console.WriteLine($"DEBUG (SpecCodeParser):     Adding SpecFeature: Name='{namePart}', Enabled={isEnabled}, No={number}, Bit={bit} (Col {successfullyParsedFeatureColumn})");
                        specSection.SpecCodes.Add(new SpecFeature(namePart, isEnabled, number, bit));
                        featureAddedThisLine = true; 
                    }
                    else // Final statusChar is still invalid (even after potential recovery)
                    {
                        if (!string.IsNullOrWhiteSpace(rawNamePartForParsing)) 
                        {
                             Console.WriteLine($"DEBUG (SpecCodeParser): FAILED PARSE for feature block: RawName='{rawNamePartForParsing}' found, but statusChar='{statusCharForParsing}' (original from block: '{featureBlock[featureNameLength]}') is invalid. Full line: '{line}'");
                        }
                        else
                        {
                             Console.WriteLine($"DEBUG (SpecCodeParser): Invalid status char '{statusCharForParsing}' (original from block: '{featureBlock[featureNameLength]}') in an otherwise empty/whitespace block. RawName='{rawNamePartForParsing}'. Skipping this block for feature addition.");
                        }
                    }
                    
                    successfullyParsedFeatureColumn++;
                    currentIndex += actualBlockConsumedIncludingStatus; // Advance by what was actually consumed for the feature name + status

                    if (successfullyParsedFeatureColumn < 4) 
                    {
                        // Separator logic
                        if (currentIndex + columnSeparatorLength <= line.Length &&
                            line.Substring(currentIndex, columnSeparatorLength) == columnSeparator) // Prefer the standard two-space separator
                        {
                            // Console.WriteLine($"DEBUG (SpecCodeParser): Consumed separator '{columnSeparator}' at index {currentIndex}");
                            currentIndex += columnSeparatorLength;
                        }
                        else if (currentIndex + 1 <= line.Length && line[currentIndex] == ' ') // Fallback: check for a single space separator
                        {
                            // This handles cases where the separator might be a single space instead of the expected two.
                            // This was observed on certain "PLC Ax.nameX" lines in the sample.dmc.
                            // Console.WriteLine($"DEBUG (SpecCodeParser): Consumed single-space separator ' ' at index {currentIndex} (expected '{columnSeparator}'). Line: '{line}'");
                            currentIndex += 1; // Consume only one space
                        }
                        else if (currentIndex < line.Length) 
                        {
                            // If neither two spaces nor one space was found, and it's not the end of the line, then it's an error.
                            string foundChars = line.Substring(currentIndex, Math.Min(columnSeparatorLength, line.Length - currentIndex));
                            Console.WriteLine($"DEBUG (SpecCodeParser): Expected separator ('{columnSeparator}' or a single ' ') after column {successfullyParsedFeatureColumn - 1} but found '{foundChars}'. Full line: '{line}'");
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

        /// <summary>
        /// Retrieves an existing <see cref="SpecCodeSection"/> from the <see cref="SoftwareDataManagementCard"/>
        /// based on the section title, or creates and adds a new one if it doesn't exist.
        /// </summary>
        /// <param name="card">The main <see cref="SoftwareDataManagementCard"/> object.</param>
        /// <param name="currentSection">The enum indicating the type of spec code section (NC or PLC).</param>
        /// <param name="sectionTitle">The title of the section (e.g., "NC-SPEC CODE No.1").</param>
        /// <returns>The existing or newly created <see cref="SpecCodeSection"/>, or null if the section title is empty or the section type is invalid.</returns>
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

        /// <summary>
        /// Resets the internal counter for feature lines within a section.
        /// This should be called when the parser begins processing a new spec code section
        /// to ensure correct bit and number calculation for <see cref="SpecFeature"/>s.
        /// </summary>
        public void ResetLineCounter() => _featureLineCounterInSection = 0;
    }
}
