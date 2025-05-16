using DmcBlueprint.Models;
using System;
using System.Globalization;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ NOTE ]" section of a DMC file.
    /// This section can contain various free-form notes, but this parser specifically
    /// looks for lines that represent revision entries, typically containing an identifier,
    /// a date, and optionally Sales Order (SO#) and Project (P#) numbers.
    /// </summary>
    internal class NoteSectionParser
    {
        /// <summary>
        /// Parses a single line from the "[ NOTE ]" section. If the line is identified as a comment/revision entry,
        /// it attempts to parse it into a <see cref="RevisionEntry"/> and adds it to the <see cref="SoftwareDataManagementCard"/>.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="card">The <see cref="SoftwareDataManagementCard"/> object to populate with parsed revision data.</param>
        public void ParseLine(string line, SoftwareDataManagementCard card)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            if (IsCommentLine(line))
            {
                var revisionEntry = ParseRevisionEntryFromLine(line);
                if (revisionEntry != null)
                {
                    card.RevisionAndCustomization ??= new RevisionAndCustomizationInfo();
                    card.RevisionAndCustomization.RevisionEntries.Add(revisionEntry);
                }
            }
            // else: Handle other types of lines within the NOTE section if necessary
        }

        /// <summary>
        /// Determines if a given line from the NOTE section is a comment line (potentially a revision entry)
        /// rather than a key-value pair or a separator.
        /// </summary>
        /// <param name="line">The line to check, expected to be pre-trimmed.</param>
        /// <returns>True if the line is considered a comment/revision entry; otherwise, false.</returns>
        private bool IsCommentLine(string line)
        {
            // Line is already trimmed
            return !string.IsNullOrWhiteSpace(line) &&
                   !line.StartsWith("<") &&
                   !line.StartsWith("[") &&
                   !line.StartsWith("=");
        }

        /// <summary>
        /// Attempts to parse a <see cref="RevisionEntry"/> from a comment line.
        /// It expects a specific format where parts are space-separated, including an identifier,
        /// a date (MM/dd/yy), and optional SO# and P# tags.
        /// </summary>
        /// <param name="line">The comment line to parse.</param>
        /// <returns>A <see cref="RevisionEntry"/> object if parsing is successful, or null if essential parts cannot be parsed.</returns>
        private RevisionEntry? ParseRevisionEntryFromLine(string line)
        {
            var entry = new RevisionEntry { FullCommentLine = line };
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1)
            {
                entry.Identifier = parts[0];
            }
            else
            {
                Console.WriteLine($"Warning: Could not parse identifier from comment line: {line}");
                return null;
            }

            if (parts.Length >= 2)
            {
                if (DateTime.TryParseExact(parts[1], "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                {
                    entry.SoftwareProductionDate = dateTimeValue;
                }
                else
                {
                    Console.WriteLine($"Warning: Could not parse date '{parts[1]}' as DateTime from comment line: {line}");
                }
            }

            string? salesOrderNum = null;
            string? projectNum = null;

            for (int i = 2; i < parts.Length; i++)
            {
                if (salesOrderNum == null)
                {
                    if (parts[i] == "SO#" && i + 1 < parts.Length && !parts[i + 1].StartsWith("P#"))
                    {
                        salesOrderNum = "SO#" + parts[i + 1]; i++;
                    }
                    else if (parts[i].StartsWith("SO#") && parts[i].Length > 3) salesOrderNum = parts[i];
                }

                if (projectNum == null)
                {
                    if (parts[i] == "P#" && i + 1 < parts.Length && !parts[i + 1].StartsWith("SO#"))
                    {
                        projectNum = "P#" + parts[i + 1]; i++;
                    }
                    else if (parts[i].StartsWith("P#") && parts[i].Length > 2) projectNum = parts[i];
                }
            }
            entry.SalesOrderNumber = salesOrderNum;
            entry.ProjectNumber = projectNum;
            return entry;
        }
    }
}