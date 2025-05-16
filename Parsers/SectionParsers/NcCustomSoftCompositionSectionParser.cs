using DmcBlueprint.Models;
using System.Collections.Generic;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ NC Custom Soft composition ]" section of a DMC file.
    /// This section lists groups of custom software files, typically identified by a group name
    /// enclosed in square brackets (e.g., "[ PBU-DAT ]"), followed by a list of file paths.
    /// </summary>
    internal class NcCustomSoftCompositionSectionParser
    {
        private CustomSoftwareGroup? _currentCustomSoftGroup = null;

        /// <summary>
        /// Parses a single line from the "[ NC Custom Soft composition ]" section.
        /// It identifies custom software group names and their associated file paths,
        /// populating the provided list of <see cref="CustomSoftwareGroup"/> objects.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="customSoftwareList">The list of <see cref="CustomSoftwareGroup"/> objects to populate.</param>
        public void ParseLine(string line, List<CustomSoftwareGroup> customSoftwareList)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                string groupName = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
                _currentCustomSoftGroup = new CustomSoftwareGroup { GroupName = groupName };
                customSoftwareList.Add(_currentCustomSoftGroup);
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentCustomSoftGroup != null)
            {
                _currentCustomSoftGroup.FilePaths.Add(valueCandidate);
            }
        }

        /// <summary>
        /// Resets the internal state of the parser.
        /// This should be called when starting to parse a new DMC file or before re-parsing,
        /// to ensure that any previously stored custom software group is cleared.
        /// </summary>
        public void ResetState()
        {
            _currentCustomSoftGroup = null;
        }
    }
}