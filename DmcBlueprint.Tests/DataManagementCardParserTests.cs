using Xunit;
using DmcBlueprint.Parsers;
using DmcBlueprint.Models; // Required for RevisionEntry
using System;
using System.Globalization; // Required for DateTime comparisons

namespace DmcBlueprint.Tests
{
    public class DataManagementCardParserTests
    {
        private readonly DataManagementCardParser _parser;

        // xUnit creates a new instance of the test class for each test method.
        // So, the constructor is a good place for setup that applies to each test.
        public DataManagementCardParserTests()
        {
            _parser = new DataManagementCardParser();
        }

        // --- Tests for IsCommentLine ---

        [Theory] // Use [Theory] for parameterized tests
        [InlineData("This is a comment", true)]
        [InlineData("  Another comment with spaces  ", true)]
        [InlineData("<NotAComment", false)]
        [InlineData("[NotACommentEither", false)]
        [InlineData("=DefinitelyNotAComment", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Valid comment < inside", true)]
        public void IsCommentLine_VariousInputs_ReturnsExpectedResult(string line, bool expected)
        {
            // Act
            var result = _parser.IsCommentLine(line);

            // Assert
            Assert.Equal(expected, result);
        }

        // --- Tests for UpdateCurrentSection ---
        [Theory]
        [InlineData("===========================[ Machine Data ]===================================", DataManagementCardParser.CurrentSection.MachineData)]
        [InlineData("====[ Customer Data ]====", DataManagementCardParser.CurrentSection.CustomerData)]
        [InlineData("[ NOTE ]", DataManagementCardParser.CurrentSection.Note)]
        [InlineData("===========================[ DVD Media Version Data ]==========================", DataManagementCardParser.CurrentSection.DvdMediaVersionData)]
        [InlineData("===========================[ Soft Version Excepted OSP System CD ]============", DataManagementCardParser.CurrentSection.SoftVersionExceptedOspSystemCd)]
        [InlineData("============================[ Package Soft composition ]======================", DataManagementCardParser.CurrentSection.PackageSoftComposition)]
        [InlineData("============================[ NC Custom Soft composition ]=====================", DataManagementCardParser.CurrentSection.NcCustomSoftComposition)]
        [InlineData("===========================[ NC-SPEC CODE No.1 ]==============================", DataManagementCardParser.CurrentSection.NcSpecCode1)]
        [InlineData("===========================[ NC-SPEC CODE No.2 ]==============================", DataManagementCardParser.CurrentSection.NcSpecCode2)]
        [InlineData("===========================[ NC-SPEC CODE No.3 ]==============================", DataManagementCardParser.CurrentSection.NcSpecCode3)]
        [InlineData("===========================[ PLC-SPEC CODE No.1 ]=============================", DataManagementCardParser.CurrentSection.PlcSpecCode1)]
        [InlineData("===========================[ PLC-SPEC CODE No.2 ]===========================", DataManagementCardParser.CurrentSection.PlcSpecCode2)]
        [InlineData("===========================[ PLC-SPEC CODE No.3 ]===========================", DataManagementCardParser.CurrentSection.PlcSpecCode3)]
        [InlineData("  [ Machine Data ]  ", DataManagementCardParser.CurrentSection.MachineData)] // Test with extra spaces around brackets
        [InlineData("[Unknown Section]", DataManagementCardParser.CurrentSection.None)] // Test unknown section
        [InlineData("NotAHeader", DataManagementCardParser.CurrentSection.None)] // Test invalid header format
        [InlineData("Malformed [Header", DataManagementCardParser.CurrentSection.None)] // Test malformed header
        public void UpdateCurrentSection_VariousHeaders_SetsCorrectSection(string headerLine, DataManagementCardParser.CurrentSection expectedSection){
            // Act
            _parser.UpdateCurrentSection(headerLine);
            var actualSection = _parser.CurrentSectionForTesting;

            // Assert
            Assert.Equal(expectedSection, actualSection);
        }
        // --- Tests for ParseRevisionEntryFromLine ---

        [Fact] // Use [Fact] for non-parameterized tests
        public void ParseRevisionEntryFromLine_ValidFullLine_ParsesCorrectly()
        {
            // Arrange
            // Note: RevisionEntry.cs does not have ProjectNumber.
            // If P# is meant to be parsed into a ProjectNumber property on RevisionEntry,
            // that property needs to be added to the RevisionEntry model.
            string line = "1  03/07/23  SO#2170654  Some other info P#1080911";
            var expectedDate = DateTime.ParseExact("03/07/23", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Equal("SO#2170654", result.SalesOrderNumber);
            Assert.Equal("P#1080911", result.ProjectNumber); // Assuming ProjectNumber is now in RevisionEntry
            Assert.Equal(line, result.FullCommentLine);
            // If RevisionEntry had a ProjectNumber property:
            // Assert.Equal("P#1080911", result.ProjectNumber);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_LineWithMultipleSpaces_ParsesCorrectly()
        {
            // Arrange
            string line = "2    04/15/24    SO#12345    Extra   Spaces P#67890";
            var expectedDate = DateTime.ParseExact("04/15/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Equal("SO#12345", result.SalesOrderNumber);
            Assert.Equal("P#67890", result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }
        
        [Fact]
        public void ParseRevisionEntryFromLine_OnlyIdentifier_ParsesIdentifier()
        {
            // Arrange
            string line = "ID_ONLY";

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID_ONLY", result.Identifier);
            Assert.Equal(default(DateTime), result.SoftwareProductionDate); // Expect default if not parsable
            Assert.Null(result.SalesOrderNumber);
            Assert.Null(result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_IdentifierAndDate_ParsesCorrectly()
        {
            // Arrange
            string line = "ID002 12/25/22";
            var expectedDate = DateTime.ParseExact("12/25/22", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID002", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Null(result.SalesOrderNumber);
            Assert.Null(result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_InvalidDateFormat_DateIsNotSet()
        {
            // Arrange
            string line = "ID003 2023-01-01 SO#FAIL P#BAD"; // Invalid date format

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID003", result.Identifier);
            Assert.Equal(default(DateTime), result.SoftwareProductionDate); // Date parsing should fail
            Assert.Equal("SO#FAIL", result.SalesOrderNumber);
            Assert.Equal("P#BAD", result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }
        
        [Fact]
        public void ParseRevisionEntryFromLine_MissingDateButHasSOAndP_ParsesIdentifierAndSOAndP()
        {
            // Arrange
            string line = "ID004 NODATEHERE SO#123 P#456";

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID004", result.Identifier);
            Assert.Equal(default(DateTime), result.SoftwareProductionDate); // "NODATEHERE" is not a date
            Assert.Equal("SO#123", result.SalesOrderNumber);
            Assert.Equal("P#456", result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_NoSalesOrderNumber_SalesOrderIsNull()
        {
            // Arrange
            string line = "ID005 01/01/24 NoSO P#789";
            var expectedDate = DateTime.ParseExact("01/01/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID005", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Null(result.SalesOrderNumber);
            Assert.Equal("P#789", result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_NoProjectNumber_ProjectNumberIsNull()
        {
            // Arrange
            string line = "ID005B 01/01/24 SO#NoP";
            var expectedDate = DateTime.ParseExact("01/01/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID005B", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Equal("SO#NoP", result.SalesOrderNumber);
            Assert.Null(result.ProjectNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_EmptyLine_ReturnsNull()
        {
            // Arrange
            string line = "";

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_WhitespaceLine_ReturnsNull()
        {
            // Arrange
            string line = "   ";

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_SalesOrderAndProjectNumberAtDifferentPosition_ParsesCorrectly()
        {
            // Arrange
            // Current FirstOrDefault logic will find "SO#" regardless of its position after the first two elements
            string line = "ID006 02/02/24 Some P#ALPHA random SO#PROPER text";
            var expectedDate = DateTime.ParseExact("02/02/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID006", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Equal("SO#PROPER", result.SalesOrderNumber);
            Assert.Equal(line, result.FullCommentLine);
        }
    }
}
