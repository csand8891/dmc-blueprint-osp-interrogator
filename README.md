# DmcBlueprint

DmcBlueprint is a .NET library designed to parse "Software Data Management Card" files, transforming their content into a structured, usable C# object model. This is particularly useful for applications that need to read, process, or analyze data from these specific types of configuration or information files.

## Features

*   **Structured Parsing:** Converts plain text Data Management Card files into a `SoftwareDataManagementCard` object.
*   **Section-Based Processing:** Identifies and processes distinct sections within the file, such as:
    *   Machine Data
    *   Customer Data
    *   NOTE (including Revision and Customization Information)
    *   DVD Media Version Data
    *   Software Version Information
    *   Package Software Composition
    *   NC Custom Software Composition
    *   NC-SPEC CODEs (No.1, No.2, No.3)
    *   PLC-SPEC CODEs (No.1, No.2, No.3)
*   **Detailed Information Extraction:** Parses specific details like revision entries (identifier, date, SO#, P#) from the "NOTE" section.
*   **Header Detection:** Uses regular expressions to accurately identify section headers, accommodating variations in formatting (e.g., `====[Section Name]====`).
*   **Testable Design:** Includes internal helper methods and properties to facilitate unit testing.

## Getting Started

### Prerequisites

*   .NET SDK (version compatible with the project, e.g., .NET 6.0, .NET 7.0, or .NET 8.0)

### Usage

To use the parser in your project:

1.  Add a reference to the `DmcBlueprint` library/project.
2.  Instantiate the `DataManagementCardParser`.
3.  Call the `Parse` method with the path to your Data Management Card file.

```csharp
using DmcBlueprint.Parsers;
using DmcBlueprint.Models;
using System;
using System.IO; // Added for FileNotFoundException

// ...

public class ExampleUsage
{
    public void ProcessDmcFile(string filePath)
    {
        var parser = new DataManagementCardParser();
        try
        {
            SoftwareDataManagementCard cardData = parser.Parse(filePath);

            // Now you can access the parsed data, for example:
            if (cardData.RevisionAndCustomization != null)
            {
                foreach (var revision in cardData.RevisionAndCustomization.RevisionEntries)
                {
                    Console.WriteLine($"Revision ID: {revision.Identifier}, Date: {revision.SoftwareProductionDate?.ToShortDateString()}");
                }
            }

            // Access other properties of cardData as needed...
            // e.g., cardData.MachineDetails, cardData.DistributorAndCustomerDetails, etc.
            // (Assuming these properties are defined in SoftwareDataManagementCard and populated by the parser)
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: File not found at {filePath}. {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while parsing the file: {ex.Message}");
        }
    }
}
```
## Project Structure

*   **`DmcBlueprint/`**
    *   **`Parsers/`**
        *   `DataManagementCardParser.cs`: The core class responsible for parsing the input files.
    *   **`Models/`**
        *   `SoftwareDataManagementCard.cs`: The main model representing the entire parsed card.
        *   `RevisionEntry.cs`: Model for individual revision entries.
        *   *(Other model files representing different sections and data points within the card, e.g., `MachineIdentifier.cs`, `DistributorAndCustomerInfo.cs`, etc.)*
*   **`DmcBlueprint.Tests/`**
    *   `DataManagementCardParserTests.cs`: Contains xUnit tests for the `DataManagementCardParser`.

## How it Works

The `DataManagementCardParser` reads the input file line by line.

1.  It first trims each line and skips empty lines.
2.  The `IsSectionHeader()` method, using a regular expression (`^=+\s*\[(?<name>[^\]]+)\]\s*=+$`), checks if the line is a section header.
3.  If it's a section header, `UpdateCurrentSection()` is called. This method extracts the section name from the header (again, using the regex to get the content within the brackets) and updates an internal state variable (`_currentSection`) to track the current context.
4.  If the line is not a section header, it's treated as a data line. A `switch` statement based on `_currentSection` directs the line to the appropriate parsing logic for that section.
    *   For example, lines in the `[NOTE]` section are checked by `IsCommentLine()` to see if they are revision entries, which are then parsed by `ParseRevisionEntryFromLine()`.
5.  The parsed data is used to populate an instance of the `SoftwareDataManagementCard` model and its associated sub-models.

## Development & Testing

This project uses xUnit for unit testing. Tests for the `DataManagementCardParser` can be found in the `DmcBlueprint.Tests` project. These tests cover:

*   Identification of comment lines.
*   Correctly updating the current section based on header lines.
*   Parsing of revision entry lines.
*   Validation of section header formats.

To run the tests, navigate to the test project directory and use the .NET CLI:

```bash
cd DmcBlueprint.Tests
dotnet test
```
## Future Enhancements / TODOs

The parser currently has a solid foundation for identifying sections and parsing revision entries. Future work includes:

*   Implementing the parsing logic for data lines within each identified section (marked with `// TODO:` comments in `DataManagementCardParser.cs`). This involves:
    *   Defining appropriate properties in the `SoftwareDataManagementCard` model and its sub-models.
    *   Creating specific parsing methods for lines like `< Type of OSP >`, `< Name >`, hex codes in SPEC CODE sections, file paths in custom software composition, etc.
*   Adding more comprehensive error handling and logging for parsing failures within data lines.
*   Expanding unit test coverage as new parsing logic is added.
