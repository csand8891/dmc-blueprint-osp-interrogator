using DmcBlueprint.Models;
using System;
using System.Globalization;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class NoteSectionParser
    {
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

        private bool IsCommentLine(string line)
        {
            // Line is already trimmed
            return !string.IsNullOrWhiteSpace(line) &&
                   !line.StartsWith("<") &&
                   !line.StartsWith("[") &&
                   !line.StartsWith("=");
        }

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