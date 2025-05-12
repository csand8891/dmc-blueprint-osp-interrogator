using DmcBlueprint.Models;
using System.Collections.Generic;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class PackageSoftCompositionSectionParser
    {
        private string? _currentPackageName = null;

        public void ParseLine(string line, List<SoftwarePackage> packageList)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (valueCandidate.StartsWith("[") && valueCandidate.EndsWith("]"))
            {
                _currentPackageName = valueCandidate.Substring(1, valueCandidate.Length - 2).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentPackageName != null)
            {
                packageList.Add(new SoftwarePackage { PackageName = _currentPackageName, Identifier = valueCandidate });
                _currentPackageName = null; // Reset after capturing the identifier
            }
        }

        public void ResetState()
        {
            _currentPackageName = null;
        }
    }
}