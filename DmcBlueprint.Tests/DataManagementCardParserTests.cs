using Xunit;
using DmcBlueprint.Parsers;
using DmcBlueprint.Models;
using System;
using System.Globalization;

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
    }
}