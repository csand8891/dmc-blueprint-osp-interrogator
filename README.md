# DmcBlueprint

DmcBlueprint is a robust .NET library that simplifies the parsing of "Software Data Management Card" files.
It efficiently transforms the textual data from these specialized documents into a well-structured and intuitive C# object model.
This enables developers to easily access, process, and integrate this information into their .NET applications, streamlining data analysis and management tasks.

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


## Project Structure

*   **`DmcBlueprint/`**: The main library project.
    *   **`Parsers/`**
        *   `DataManagementCardParser.cs`: The primary orchestrator that reads the DMC file and delegates section-specific parsing.
        *   **`SectionParsers/`**: Contains specialized parsers for individual sections of the DMC file.
            *   `MachineDataSectionParser.cs`: Parses the "[Machine Data]" section.
            *   `CustomerDataSectionParser.cs`: Parses the "[Customer Data]" section.
            *   `NoteSectionParser.cs`: Parses the "[NOTE]" section, including revision history.
            *   `SpecCodeSectionParser.cs`: Parses various "[SPEC CODE]" sections.
            *   *(Other section-specific parser files, e.g., `DvdMediaVersionDataSectionParser.cs`, `PackageSoftCompositionSectionParser.cs`, etc.)*
    *   **`Models/`**
        *   `SoftwareDataManagementCard.cs`: The main model representing the entire parsed card.
        *   `MachineIdentifier.cs`: Model for machine-specific data.
        *   `DistributorAndCustomerInfo.cs`: Model for distributor and customer contact details.
        *   `RevisionEntry.cs`: Model for individual revision entries.
        *   `DvdMediaVersionData.cs`: Model for DVD media version information.
        *   *(Other model files representing different data structures, e.g., `SoftwarePackage.cs`, `SpecCodeSection.cs`, `ContactEntry.cs`, etc.)*
*   **`DmcBlueprint.Tests/`**: Contains unit tests for the DmcBlueprint library.
    *   `DataManagementCardParserTests.cs`: Specifically, xUnit tests for the `DataManagementCardParser`.

## How it Works

The `DataManagementCardParser` processes the input file line by line, maintaining context about the current section to apply appropriate parsing rules:

1.  **Line Preprocessing:** Each line read from the file undergoes initial normalization (e.g., specific Unicode characters like Roman numeral 'Ⅱ' and ideographic spaces are converted to standard equivalents). The resulting line is then trimmed of leading/trailing whitespace.
2.  **Empty Line Handling:** Trimmed lines that are empty are generally skipped. However, for certain sections like `SPEC CODE`s, even lines that appear empty (or become empty after trimming) are passed to their respective section parsers, as blank lines can be significant in these contexts.
3.  **Section Header Identification:** The `IsSectionHeader()` method, utilizing a regular expression (`^=+\s*\[(?<name>[^\]]+)\]\s*=+$`), determines if the trimmed line is a section header.
4.  **Context Update:** If a line is identified as a section header, `UpdateCurrentSection()` is invoked. This method extracts the section name (e.g., "Machine Data", "NOTE") from the header and updates an internal state variable (`_currentSection`) to reflect the new parsing context.
5.  **Data Line Delegation:** If the line is not a section header, it's treated as a data line.
    *   For `SPEC CODE` sections, the line (sometimes the original, untrimmed line if it meets specific length criteria, otherwise the trimmed line) is passed to a specialized `SpecCodeSectionParser`.
    *   For all other sections, the trimmed data line is directed by a `switch` statement (based on `_currentSection`) to a dedicated parser instance (e.g., `MachineDataSectionParser`, `NoteSectionParser`, `CustomerDataSectionParser`). These specialized parsers are responsible for interpreting the data according to that section's unique format.
    *   For instance, lines within the `[NOTE]` section are handled by the `NoteSectionParser`, which contains the logic to identify and extract information like revision history entries.
6.  **Model Population:** The data extracted by the various section parsers is used to populate the properties of a `SoftwareDataManagementCard` object and its associated sub-models (like `MachineIdentifier`, `RevisionEntry`, etc.), creating a structured representation of the file's content.

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

While DmcBlueprint has a solid foundation for identifying file sections and parsing revision history, the following enhancements are planned to make it a comprehensive parsing solution:

* **Complete Data Extraction for All Sections:**
    * **Implement Parsers for Section-Specific Data:** Develop and integrate parsing logic for all data lines within each identified section (e.g., Machine Data, Customer Data, DVD Media Version, Software Version, Package/Custom Software Composition, SPEC CODEs). This is the highest priority and will involve:
        * Defining and refining corresponding properties in `SoftwareDataManagementCard.cs` and its associated sub-models (like `MachineIdentifier.cs`, `DistributorAndCustomerInfo.cs`, etc.) to accurately store the extracted information.
        * Creating robust parsing methods to handle various data formats, including:
            * Simple key-value pairs (e.g., `< Type of OSP >: Value`, `< Name >: Value`).
            * Lists of items or file paths (e.g., in software composition sections).
            * Structured data like hexadecimal codes in `SPEC CODE` sections.
        * Parsing any remaining information within the `[NOTE]` section, such as "Customization Information."
    * **Populate Main Model:** Ensure all parsed data correctly populates the `SoftwareDataManagementCard` object model.

* **Advanced Error Handling and Validation:**
    * **Granular Error Reporting:** Implement more specific error detection and reporting for malformed or unexpected data encountered *within* data lines of each section, rather than general file-level errors.
    * **Data Validation:** Introduce validation logic for critical data points (e.g., date formats, expected numeric values, specific string patterns) to ensure data integrity.
    * **Resilient Parsing (Optional):** Explore options for the parser to continue processing and report multiple errors rather than halting on the first data line error, if appropriate for the use case.

* **Comprehensive Unit Test Coverage:**
    * **Section-Specific Tests:** Write dedicated unit tests for the parsing logic of each data field within every section as it's implemented.
    * **Edge Case and Malformed Data Testing:** Expand tests to cover various edge cases, including missing optional fields, empty sections, and common data entry errors, to ensure the parser behaves predictably.

* **Logging and Diagnostics:**
    * **Integrate Logging Framework:** Consider incorporating a standard logging library (e.g., `Microsoft.Extensions.Logging`) to provide detailed tracing of the parsing process, aiding in debugging and understanding parser behavior with complex files.

* **Documentation and Usability:**
    * **API Documentation:** Enhance XML documentation comments for all public classes and methods to improve IntelliSense and provide clearer guidance for library users.
    * **Expanded Usage Examples:** Add more diverse examples in the README or separate documentation showing how to access and utilize data from various sections of the parsed `SoftwareDataManagementCard`.

* **Support for Format Variations (Long-term):**
    * Investigate if different versions or minor variations of the "Software Data Management Card" format exist and plan for flexible parsing or version detection if necessary.
