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
            Assert.Equal(line, result.FullCommentLine);
            // If RevisionEntry had a ProjectNumber property:
            // Assert.Equal("P#1080911", result.ProjectNumber);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_LineWithMultipleSpaces_ParsesCorrectly()
        {
            // Arrange
            string line = "2    04/15/24    SO#12345    Extra   Spaces";
            var expectedDate = DateTime.ParseExact("04/15/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("2", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Equal("SO#12345", result.SalesOrderNumber);
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
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_InvalidDateFormat_DateIsNotSet()
        {
            // Arrange
            string line = "ID003 2023-01-01 SO#FAIL"; // Invalid date format

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID003", result.Identifier);
            Assert.Equal(default(DateTime), result.SoftwareProductionDate); // Date parsing should fail
            Assert.Equal("SO#FAIL", result.SalesOrderNumber);
            Assert.Equal(line, result.FullCommentLine);
        }
        
        [Fact]
        public void ParseRevisionEntryFromLine_MissingDateButHasSO_ParsesIdentifierAndSO()
        {
            // Arrange
            string line = "ID004 NODATEHERE SO#123";

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID004", result.Identifier);
            Assert.Equal(default(DateTime), result.SoftwareProductionDate); // "NODATEHERE" is not a date
            Assert.Equal("SO#123", result.SalesOrderNumber);
            Assert.Equal(line, result.FullCommentLine);
        }

        [Fact]
        public void ParseRevisionEntryFromLine_NoSalesOrderNumber_SalesOrderIsNull()
        {
            // Arrange
            string line = "ID005 01/01/24 NoSO";
            var expectedDate = DateTime.ParseExact("01/01/24", "MM/dd/yy", CultureInfo.InvariantCulture);

            // Act
            var result = _parser.ParseRevisionEntryFromLine(line);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ID005", result.Identifier);
            Assert.Equal(expectedDate, result.SoftwareProductionDate);
            Assert.Null(result.SalesOrderNumber);
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
        public void ParseRevisionEntryFromLine_SalesOrderNumberAtDifferentPosition_ParsesCorrectly()
        {
            // Arrange
            // Current FirstOrDefault logic will find "SO#" regardless of its position after the first two elements
            string line = "ID006 02/02/24 Some random text SO#PROPER";
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
