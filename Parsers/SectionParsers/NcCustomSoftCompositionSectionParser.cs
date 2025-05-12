using DmcBlueprint.Models;
using System.Collections.Generic;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class NcCustomSoftCompositionSectionParser
    {
        private CustomSoftwareGroup? _currentCustomSoftGroup = null;

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

        public void ResetState()
        {
            _currentCustomSoftGroup = null;
        }
    }
}