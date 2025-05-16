using DmcBlueprint.Models;
using System.Collections.Generic;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ Package Soft composition ]" section of a DMC file.
    /// This section lists software packages and their corresponding identifiers.
    /// Keys (package names) are expected to be enclosed in square brackets (e.g., "[ NC INSTALLER ]").
    /// </summary>
    internal class PackageSoftCompositionSectionParser
    {
        private string? _currentPackageName = null;


        /// <summary>
        /// Parses a single line from the "[ Package Soft composition ]" section.
        /// It identifies package names (keys) and their identifiers (values), adding them to the provided list.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="packageList">The list of <see cref="SoftwarePackage"/> objects to populate.</param>
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

        /// <summary>
        /// Resets the internal state of the parser.
        /// This should be called when starting to parse a new DMC file or before re-parsing,
        /// to ensure that any previously stored package name is cleared.
        /// </summary>
        public void ResetState()
        {
            _currentPackageName = null;
        }
    }
}