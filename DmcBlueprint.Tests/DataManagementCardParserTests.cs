using Xunit;
using DmcBlueprint.Parsers;
using DmcBlueprint.Models; // Required for RevisionEntry
using System;
using System.Globalization; // Required for DateTime comparisons
using System.Linq; // Added for assertions on lists
using System.Collections.Generic; // Added for List<SoftwarePackage>

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

        // --- Tests for ParseMachineDataLine ---

        [Fact]
        public void ParseMachineDataLine_SingleLineValue_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Type of OSP>";
            string valueLine = "  OSP-P300LA  ";
            string expectedOspType = "OSP-P300LA";

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails); // Set the current key
            _parser.ParseMachineDataLine(valueLine, machineDetails); // Parse the value

            // Assert
            Assert.Equal(expectedOspType, machineDetails.OspType);
        }

        [Fact]
        public void ParseMachineDataLine_MultiLineValue_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Type of Machine>";
            string valueLine1 = "  LB3000II  ";
            string valueLine2 = "  1SC-1000  ";
            string valueLine3 = "V12"; // No extra spaces

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails); // Set the current key
            _parser.ParseMachineDataLine(valueLine1, machineDetails);
            _parser.ParseMachineDataLine(valueLine2, machineDetails);
            _parser.ParseMachineDataLine(valueLine3, machineDetails);

            // Assert
            Assert.Equal(3, machineDetails.MachineType.Count);
            Assert.Contains("LB3000II", machineDetails.MachineType);
            Assert.Contains("1SC-1000", machineDetails.MachineType);
            Assert.Contains("V12", machineDetails.MachineType);
        }
        
        [Fact]
        public void ParseMachineDataLine_SoftwareProductionNumber_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Soft Production No>";
            string valueLine = "  #08410  ";
            string expectedProdNo = "#08410";

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(valueLine, machineDetails);

            // Assert
            Assert.Equal(expectedProdNo, machineDetails.SoftwareProductionNumber);
        }

        [Fact]
        public void ParseMachineDataLine_ProjectNumber_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Project No>";
            string valueLine = "  P251028  ";
            string expectedProjectNo = "P251028";

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(valueLine, machineDetails);

            // Assert
            Assert.Equal(expectedProjectNo, machineDetails.ProjectNumber);
        }

        [Fact]
        public void ParseMachineDataLine_SoftwareProductionDate_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Software Production Date>";
            string valueLine = "  2023-03-07  ";
            DateTime expectedDate = new DateTime(2023, 3, 7);

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(valueLine, machineDetails);

            // Assert
            Assert.Equal(expectedDate, machineDetails.SoftwareProductionDate);
        }

        [Fact]
        public void ParseMachineDataLine_InvalidSoftwareProductionDate_RetainsDefaultDate()
        {
            // Arrange
            var machineDetails = new MachineIdentifier(); // SoftwareProductionDate will be DateTime.MinValue
            string keyLine = "<Software Production Date>";
            string valueLine = "  INVALID-DATE  ";
            DateTime expectedDate = default(DateTime); // Default for DateTime

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(valueLine, machineDetails);

            // Assert
            Assert.Equal(expectedDate, machineDetails.SoftwareProductionDate);
        }

        [Fact]
        public void ParseMachineDataLine_KeyLineWithSpacesAroundBrackets_ParsesCorrectly()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "  < Type of OSP >  "; // Key with spaces
            string valueLine = "OSP-Test";
            string expectedOspType = "OSP-Test";

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(valueLine, machineDetails);

            // Assert
            Assert.Equal(expectedOspType, machineDetails.OspType);
        }

        [Fact]
        public void ParseMachineDataLine_ValueLineBeforeKey_IsIgnored()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string valueLine = "OrphanValue";

            // Act
            _parser.ParseMachineDataLine(valueLine, machineDetails); // No key set yet

            // Assert
            Assert.Null(machineDetails.OspType);
            Assert.Empty(machineDetails.MachineType);
            Assert.Null(machineDetails.SoftwareProductionNumber);
            // ... assert other properties are also in their initial state
        }

        [Fact]
        public void ParseMachineDataLine_EmptyValueLine_IsIgnored()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Type of OSP>";
            string emptyValueLine = "   "; // Whitespace only

            // Act
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine(emptyValueLine, machineDetails);

            // Assert
            Assert.Null(machineDetails.OspType); // Should not have been set by the empty value
        }
        
        [Fact]
        public void ParseMachineDataLine_UnknownKey_ValueIsIgnoredButDoesNotCrash()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Unknown Key>";
            string valueLine = "SomeValueForUnknownKey";

            // Act & Assert (no exception should be thrown)
            Exception? ex = Record.Exception(() => {
                _parser.ParseMachineDataLine(keyLine, machineDetails);
                _parser.ParseMachineDataLine(valueLine, machineDetails);
            });
            
            Assert.Null(ex);
            // Optionally, assert that known properties were not affected
            Assert.Null(machineDetails.OspType);
        }

        [Fact]
        public void ParseMachineDataLine_TypeOfMachine_ClearsListOnNewKeyEncounter()
        {
            // Arrange
            var machineDetails = new MachineIdentifier();
            string keyLine = "<Type of Machine>";
            
            // First set of values
            _parser.ParseMachineDataLine(keyLine, machineDetails);
            _parser.ParseMachineDataLine("MachineA", machineDetails);
            _parser.ParseMachineDataLine("MachineB", machineDetails);
            Assert.Equal(2, machineDetails.MachineType.Count);

            // Act: Encounter the key again and add new values
            _parser.ParseMachineDataLine(keyLine, machineDetails); // This should clear the list
            _parser.ParseMachineDataLine("MachineC", machineDetails);
            _parser.ParseMachineDataLine("MachineD", machineDetails);

            // Assert
            Assert.Equal(2, machineDetails.MachineType.Count);
            Assert.Contains("MachineC", machineDetails.MachineType);
            Assert.Contains("MachineD", machineDetails.MachineType);
            Assert.DoesNotContain("MachineA", machineDetails.MachineType);
            Assert.DoesNotContain("MachineB", machineDetails.MachineType);
        }
        // --- Tests for ParseCustomerDataLine ---
            
            [Fact]
            public void ParseCustomerDataLine_DistributorDetails_ParsesCorrectly()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();
                // Simulate entering the CustomerData section, which defaults to Distributor
                // (In a full parse, UpdateCurrentSection would set _currentCustomerEntityType)
                // For direct testing, we can assume the parser's state is already Distributor.

                // Act
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("  Distributor Name Inc.  ", customerInfo);
                _parser.ParseCustomerDataLine("<Address>", customerInfo);
                _parser.ParseCustomerDataLine("  123 Distributor St  ", customerInfo);
                _parser.ParseCustomerDataLine("<Phone>", customerInfo);
                _parser.ParseCustomerDataLine("  555-0100  ", customerInfo);

                // Assert
                Assert.Equal("Distributor Name Inc.", customerInfo.Distributor.Name);
                Assert.Equal("123 Distributor St", customerInfo.Distributor.Address);
                Assert.Equal("555-0100", customerInfo.Distributor.Phone);
                Assert.Null(customerInfo.EndCustomer.Name); // End customer should be unaffected
            }

            [Fact]
            public void ParseCustomerDataLine_EndCustomerDetails_ParsesCorrectlyAfterCustomerKey()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act
                // Distributor part (optional, but good to show sequence)
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("Distro Co", customerInfo);

                // Switch to End Customer
                _parser.ParseCustomerDataLine("<Customer>", customerInfo);

                // End Customer details
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("  End User LLC  ", customerInfo);
                _parser.ParseCustomerDataLine("<Address>", customerInfo);
                _parser.ParseCustomerDataLine("  456 User Ave  ", customerInfo);
                _parser.ParseCustomerDataLine("<Phone>", customerInfo);
                _parser.ParseCustomerDataLine("  555-0199  ", customerInfo);


                // Assert
                Assert.Equal("Distro Co", customerInfo.Distributor.Name); // Distributor info should remain
                Assert.Equal("End User LLC", customerInfo.EndCustomer.Name);
                Assert.Equal("456 User Ave", customerInfo.EndCustomer.Address);
                Assert.Equal("555-0199", customerInfo.EndCustomer.Phone);
            }

            [Fact]
            public void ParseCustomerDataLine_OnlyEndCustomerDetails_ParsesCorrectly()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act
                _parser.ParseCustomerDataLine("<Customer>", customerInfo); // Switch to End Customer immediately
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("Customer Only Inc.", customerInfo);

                // Assert
                Assert.Null(customerInfo.Distributor.Name); // Distributor should be empty
                Assert.Equal("Customer Only Inc.", customerInfo.EndCustomer.Name);
            }

            [Fact]
            public void ParseCustomerDataLine_EmptyValueLines_AreIgnored()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("   ", customerInfo); // Empty value for Distributor Name
                _parser.ParseCustomerDataLine("<Address>", customerInfo);
                _parser.ParseCustomerDataLine("Distro Address", customerInfo);

                _parser.ParseCustomerDataLine("<Customer>", customerInfo);
                _parser.ParseCustomerDataLine("<Name>", customerInfo);
                _parser.ParseCustomerDataLine("End User Name", customerInfo);
                _parser.ParseCustomerDataLine("<Address>", customerInfo);
                _parser.ParseCustomerDataLine("", customerInfo); // Empty value for End Customer Address

                // Assert
                Assert.Null(customerInfo.Distributor.Name); // Name should be null due to empty value
                Assert.Equal("Distro Address", customerInfo.Distributor.Address);
                Assert.Equal("End User Name", customerInfo.EndCustomer.Name);
                Assert.Null(customerInfo.EndCustomer.Address); // Address should be null
            }

            [Fact]
            public void ParseCustomerDataLine_ValueLineBeforeAnyKey_IsIgnored()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();
                // Simulate parser state where _currentCustomerDataKey is null
                // (This is the default state before any <Key> is encountered in the section)

                // Act
                _parser.ParseCustomerDataLine("Orphan Value Line", customerInfo);

                // Assert
                Assert.Null(customerInfo.Distributor.Name);
                Assert.Null(customerInfo.Distributor.Address);
                Assert.Null(customerInfo.Distributor.Phone);
                Assert.Null(customerInfo.EndCustomer.Name);
            }

            [Fact]
            public void ParseCustomerDataLine_UnknownKey_ValueIsIgnoredAndDoesNotCrash()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act & Assert (no exception should be thrown)
                Exception? ex = Record.Exception(() => {
                    _parser.ParseCustomerDataLine("<UnknownKey>", customerInfo);
                    _parser.ParseCustomerDataLine("Value for unknown key", customerInfo);
                });

                Assert.Null(ex);
                // Assert that known properties were not affected
                Assert.Null(customerInfo.Distributor.Name);
                Assert.Null(customerInfo.EndCustomer.Name);
            }
            
            [Fact]
            public void ParseCustomerDataLine_KeyLineWithSpacesAroundBrackets_ParsesCorrectly()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act
                _parser.ParseCustomerDataLine("  < Name >  ", customerInfo);
                _parser.ParseCustomerDataLine("Spaced Key Name", customerInfo);

                // Assert
                Assert.Equal("Spaced Key Name", customerInfo.Distributor.Name);
            }

            [Fact]
            public void ParseCustomerDataLine_CustomerKeyResetsDataKey()
            {
                // Arrange
                var customerInfo = new DistributorAndCustomerInfo();

                // Act
                _parser.ParseCustomerDataLine("<Name>", customerInfo); // Sets _currentCustomerDataKey to "Name"
                _parser.ParseCustomerDataLine("Distributor Name", customerInfo);
                
                _parser.ParseCustomerDataLine("<Customer>", customerInfo); // Should reset _currentCustomerDataKey
                
                // This line should be ignored because _currentCustomerDataKey was reset
                _parser.ParseCustomerDataLine("This Should Be Ignored", customerInfo); 
                                                                                    
                _parser.ParseCustomerDataLine("<Name>", customerInfo); // Now set key for EndCustomer
                _parser.ParseCustomerDataLine("End Customer Name", customerInfo);


                // Assert
                Assert.Equal("Distributor Name", customerInfo.Distributor.Name);
                Assert.Equal("End Customer Name", customerInfo.EndCustomer.Name);
            }

            // --- Tests for ParseDvdMediaVersionDataLine ---

        [Fact]
        public void ParseDvdMediaVersionDataLine_WindowsSystemCdVersion_ParsesCorrectly()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLine = "[Windows System CD Version]";
            string valueLine = "  01  ";
            string expectedVersion = "01";

            // Act
            _parser.ParseDvdMediaVersionDataLine(keyLine, dvdMediaData); // Set the current key
            _parser.ParseDvdMediaVersionDataLine(valueLine, dvdMediaData); // Parse the value

            // Assert
            Assert.Equal(expectedVersion, dvdMediaData.WindowsSystemCdVersion);
            Assert.Null(dvdMediaData.OspSystemCdVersion); // Other property should be unaffected
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_OspSystemCdVersion_ParsesCorrectly()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLine = "[OSP System CD Version]";
            string valueLine = "  03  ";
            string expectedVersion = "03";

            // Act
            _parser.ParseDvdMediaVersionDataLine(keyLine, dvdMediaData); // Set the current key
            _parser.ParseDvdMediaVersionDataLine(valueLine, dvdMediaData); // Parse the value

            // Assert
            Assert.Equal(expectedVersion, dvdMediaData.OspSystemCdVersion);
            Assert.Null(dvdMediaData.WindowsSystemCdVersion); // Other property should be unaffected
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_BothVersions_ParsesCorrectlyInSequence()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLineWin = "[Windows System CD Version]";
            string valueLineWin = "01";
            string keyLineOsp = "[OSP System CD Version]";
            string valueLineOsp = "03";

            // Act
            _parser.ParseDvdMediaVersionDataLine(keyLineWin, dvdMediaData);
            _parser.ParseDvdMediaVersionDataLine(valueLineWin, dvdMediaData);
            _parser.ParseDvdMediaVersionDataLine(keyLineOsp, dvdMediaData);
            _parser.ParseDvdMediaVersionDataLine(valueLineOsp, dvdMediaData);

            // Assert
            Assert.Equal("01", dvdMediaData.WindowsSystemCdVersion);
            Assert.Equal("03", dvdMediaData.OspSystemCdVersion);
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_KeyLineWithExtraSpaces_ParsesCorrectly()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLine = "  [ Windows System CD Version ]  "; // Key with spaces
            string valueLine = "02";
            string expectedVersion = "02";

            // Act
            _parser.ParseDvdMediaVersionDataLine(keyLine, dvdMediaData);
            _parser.ParseDvdMediaVersionDataLine(valueLine, dvdMediaData);

            // Assert
            Assert.Equal(expectedVersion, dvdMediaData.WindowsSystemCdVersion);
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_ValueLineBeforeKey_IsIgnored()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string valueLine = "OrphanValue07";
            // Simulate parser state where _currentDvdMediaKey is null

            // Act
            _parser.ParseDvdMediaVersionDataLine(valueLine, dvdMediaData);

            // Assert
            Assert.Null(dvdMediaData.WindowsSystemCdVersion);
            Assert.Null(dvdMediaData.OspSystemCdVersion);
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_EmptyValueLine_IsIgnored()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLine = "[Windows System CD Version]";
            string emptyValueLine = "   "; // Whitespace only

            // Act
            _parser.ParseDvdMediaVersionDataLine(keyLine, dvdMediaData);
            _parser.ParseDvdMediaVersionDataLine(emptyValueLine, dvdMediaData);

            // Assert
            Assert.Null(dvdMediaData.WindowsSystemCdVersion); // Should not have been set
        }

        [Fact]
        public void ParseDvdMediaVersionDataLine_UnknownKey_ValueIsIgnoredAndDoesNotCrash()
        {
            // Arrange
            var dvdMediaData = new DvdMediaVersionData();
            string keyLine = "[Unknown DVD Key]";
            string valueLine = "ValueForUnknownDvdKey";

            // Act & Assert (no exception should be thrown)
            Exception? ex = Record.Exception(() => {
                _parser.ParseDvdMediaVersionDataLine(keyLine, dvdMediaData);
                _parser.ParseDvdMediaVersionDataLine(valueLine, dvdMediaData);
            });
            
            Assert.Null(ex);
            // Assert that known properties were not affected
            Assert.Null(dvdMediaData.WindowsSystemCdVersion);
            Assert.Null(dvdMediaData.OspSystemCdVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_WindowsSystemVersion_ParsesCorrectly()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[Windows System Version]";
            string valueLine = "  8.0.0.E  ";
            string expectedVersion = "8.0.0.E";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Equal(expectedVersion, softVersions.WindowsSystemVersion);
            Assert.Null(softVersions.ApiDvdVersion);
            Assert.Null(softVersions.MtConnectVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_ApiDvdVersion_ParsesCorrectly()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[Custom API Additional DVD Version]";
            string valueLine = "  App_THINC_API          V1.23.2.0W-V3.3.4_P  ";
            string expectedVersion = "App_THINC_API          V1.23.2.0W-V3.3.4_P";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Equal(expectedVersion, softVersions.ApiDvdVersion);
            Assert.Null(softVersions.WindowsSystemVersion);
            Assert.Null(softVersions.MtConnectVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_MtConnectVersionWithSpecialChar_ParsesCorrectly()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[MTconnect Version@(Included in App_THINC_API DVD)]"; // Key with special char
            string valueLine = "  MTConnect              V3.3.4  ";
            string expectedVersion = "MTConnect              V3.3.4";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Equal(expectedVersion, softVersions.MtConnectVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_MtConnectVersionWithoutSpecialChar_ParsesCorrectly()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[MTconnect Version]"; // Key without special char (fallback)
            string valueLine = "  MTConnect V3.3.5  ";
            string expectedVersion = "MTConnect V3.3.5";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Equal(expectedVersion, softVersions.MtConnectVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_AllVersions_ParsesCorrectlyInSequence()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyWin = "[Windows System Version]";
            string valWin = "8.0.0.E";
            string keyApi = "[Custom API Additional DVD Version]";
            string valApi = "App_THINC_API V1.23.2.0W-V3.3.4_P";
            string keyMt = "[MTconnect Version]";
            string valMt = "MTConnect V3.3.4";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyWin, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valWin, softVersions);
            _parser.ParseSoftVersionIdentifierLine(keyApi, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valApi, softVersions);
            _parser.ParseSoftVersionIdentifierLine(keyMt, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valMt, softVersions);

            // Assert
            Assert.Equal(valWin, softVersions.WindowsSystemVersion);
            Assert.Equal(valApi, softVersions.ApiDvdVersion);
            Assert.Equal(valMt, softVersions.MtConnectVersion);
        }
        
        [Fact]
        public void ParseSoftVersionIdentifierLine_KeyLineWithExtraSpaces_ParsesCorrectly()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "  [ Windows System Version ]  "; // Key with spaces
            string valueLine = "9.0.0.F";
            string expectedVersion = "9.0.0.F";

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Equal(expectedVersion, softVersions.WindowsSystemVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_ValueLineBeforeKey_IsIgnored()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string valueLine = "OrphanVersionX.Y.Z";
            // Simulate parser state where _currentSoftVersionKey is null

            // Act
            _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);

            // Assert
            Assert.Null(softVersions.WindowsSystemVersion);
            Assert.Null(softVersions.ApiDvdVersion);
            Assert.Null(softVersions.MtConnectVersion);
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_EmptyValueLine_IsIgnored()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[Windows System Version]";
            string emptyValueLine = "   "; // Whitespace only

            // Act
            _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
            _parser.ParseSoftVersionIdentifierLine(emptyValueLine, softVersions);

            // Assert
            Assert.Null(softVersions.WindowsSystemVersion); // Should not have been set
        }

        [Fact]
        public void ParseSoftVersionIdentifierLine_UnknownKey_ValueIsIgnoredAndDoesNotCrash()
        {
            // Arrange
            var softVersions = new SoftwareVersionIdentifier();
            string keyLine = "[Unknown Software Key]";
            string valueLine = "ValueForUnknownSoftwareKey";

            // Act & Assert (no exception should be thrown)
            Exception? ex = Record.Exception(() => {
                _parser.ParseSoftVersionIdentifierLine(keyLine, softVersions);
                _parser.ParseSoftVersionIdentifierLine(valueLine, softVersions);
            });
            
            Assert.Null(ex);
            // Assert that known properties were not affected
            Assert.Null(softVersions.WindowsSystemVersion);
            Assert.Null(softVersions.ApiDvdVersion);
            Assert.Null(softVersions.MtConnectVersion);
        }

        // --- Tests for ParseNcCustomSoftCompositionLine ---

        [Fact]
        public void ParseNcCustomSoftCompositionLine_SingleGroupSingleFile_ParsesCorrectly()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string groupNameLine = "[LPP]";
            string filePathLine = "  C:\\OSP-P\\P-MANUAL\\LPP\\ENG\\LPP627C-ENG.CNT  ";
            string expectedGroupName = "LPP";
            string expectedFilePath = "C:\\OSP-P\\P-MANUAL\\LPP\\ENG\\LPP627C-ENG.CNT";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(groupNameLine, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePathLine, customSoftwareList);

            // Assert
            Assert.Single(customSoftwareList);
            var group = customSoftwareList[0];
            Assert.Equal(expectedGroupName, group.GroupName);
            Assert.Single(group.FilePaths);
            Assert.Equal(expectedFilePath, group.FilePaths[0]);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_SingleGroupMultipleFiles_ParsesCorrectly()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string groupNameLine = "[HMI]";
            string filePath1 = "C:\\OSP-P\\HMI\\PDSN401-LU302X.DLL";
            string filePath2 = "  C:\\OSP-P\\HMI\\ENG\\PDSN401-LU302XEN.DLL  ";
            string filePath3 = "C:\\OSP-P\\HMI-REG\\PSRD401-LU302X.REG";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(groupNameLine, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePath1, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePath2, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePath3, customSoftwareList);

            // Assert
            Assert.Single(customSoftwareList);
            var group = customSoftwareList[0];
            Assert.Equal("HMI", group.GroupName);
            Assert.Equal(3, group.FilePaths.Count);
            Assert.Contains(filePath1, group.FilePaths);
            Assert.Contains(filePath2.Trim(), group.FilePaths);
            Assert.Contains(filePath3, group.FilePaths);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_MultipleGroups_ParsesCorrectly()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string group1Name = "[LPP]";
            string group1File1 = "path/to/lpp/file1.txt";
            string group2Name = "[HMI]";
            string group2File1 = "path/to/hmi/fileA.dll";
            string group2File2 = "path/to/hmi/fileB.dll";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(group1Name, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(group1File1, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(group2Name, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(group2File1, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(group2File2, customSoftwareList);

            // Assert
            Assert.Equal(2, customSoftwareList.Count);

            var group1 = customSoftwareList.FirstOrDefault(g => g.GroupName == "LPP");
            Assert.NotNull(group1);
            Assert.Single(group1.FilePaths);
            Assert.Contains(group1File1, group1.FilePaths);

            var group2 = customSoftwareList.FirstOrDefault(g => g.GroupName == "HMI");
            Assert.NotNull(group2);
            Assert.Equal(2, group2.FilePaths.Count);
            Assert.Contains(group2File1, group2.FilePaths);
            Assert.Contains(group2File2, group2.FilePaths);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_GroupNameWithExtraSpaces_ParsesCorrectly()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string groupNameLine = "  [  SPACED GROUP  ]  ";
            string filePathLine = "file.path";
            string expectedGroupName = "SPACED GROUP";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(groupNameLine, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePathLine, customSoftwareList);

            // Assert
            Assert.Single(customSoftwareList);
            Assert.Equal(expectedGroupName, customSoftwareList[0].GroupName);
            Assert.Contains(filePathLine, customSoftwareList[0].FilePaths);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_FilePathLineBeforeGroupName_IsIgnored()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string filePathLine = "Orphan/File/Path.txt";
            // Simulate parser state where _currentCustomSoftGroup is null

            // Act
            _parser.ParseNcCustomSoftCompositionLine(filePathLine, customSoftwareList);

            // Assert
            Assert.Empty(customSoftwareList);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_EmptyLineBetweenGroupAndFile_IsIgnored()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string groupNameLine = "[TEST GROUP]";
            string emptyLine = "   ";
            string filePathLine = "test/file.dat";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(groupNameLine, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(emptyLine, customSoftwareList); // Should be ignored
            _parser.ParseNcCustomSoftCompositionLine(filePathLine, customSoftwareList);

            // Assert
            Assert.Single(customSoftwareList);
            var group = customSoftwareList[0];
            Assert.Equal("TEST GROUP", group.GroupName);
            Assert.Single(group.FilePaths);
            Assert.Contains(filePathLine, group.FilePaths);
        }

        [Fact]
        public void ParseNcCustomSoftCompositionLine_EmptyLinesWithinFilePaths_AreIgnored()
        {
            // Arrange
            var customSoftwareList = new List<CustomSoftwareGroup>();
            string groupNameLine = "[FILES GROUP]";
            string filePath1 = "file1.exe";
            string emptyLine1 = "";
            string filePath2 = "file2.dll";
            string emptyLine2 = "  ";

            // Act
            _parser.ParseNcCustomSoftCompositionLine(groupNameLine, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePath1, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(emptyLine1, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(filePath2, customSoftwareList);
            _parser.ParseNcCustomSoftCompositionLine(emptyLine2, customSoftwareList); // Empty line after last file path

            // Assert
            Assert.Single(customSoftwareList);
            var group = customSoftwareList[0];
            Assert.Equal("FILES GROUP", group.GroupName);
            Assert.Equal(2, group.FilePaths.Count);
            Assert.Contains(filePath1, group.FilePaths);
            Assert.Contains(filePath2, group.FilePaths);
        }

        

        // --- Tests for GetTitleForCurrentSpecSection ---
        // Note: To test this directly, GetTitleForCurrentSpecSection would need to be internal.

        [Theory]
        [InlineData("===========================[ NC-SPEC CODE No.1 ]==============================", "NC-SPEC CODE No.1")]
        [InlineData("===========================[ NC-SPEC CODE No.2 ]==============================", "NC-SPEC CODE No.2")]
        [InlineData("===========================[ NC-SPEC CODE No.3 ]==============================", "NC-SPEC CODE No.3")]
        [InlineData("===========================[ PLC-SPEC CODE No.1 ]=============================", "PLC-SPEC CODE No.1")]
        [InlineData("===========================[ PLC-SPEC CODE No.2 ]===========================", "PLC-SPEC CODE No.2")]
        [InlineData("===========================[ PLC-SPEC CODE No.3 ]===========================", "PLC-SPEC CODE No.3")]
        public void GetTitleForCurrentSpecSection_ValidSpecSections_ReturnsCorrectTitle(string sectionHeader, string expectedTitle)
        {
            // Arrange
            _parser.UpdateCurrentSection(sectionHeader); // Sets the internal _currentSection

            // Act
            // To make this directly callable, GetTitleForCurrentSpecSection needs to be internal.
            var result = _parser.GetTitleForCurrentSpecSection();

            // Assert
            Assert.Equal(expectedTitle, result);
        }

        [Fact]
        public void GetTitleForCurrentSpecSection_NonSpecSection_ReturnsEmptyString()
        {
            // Arrange
            _parser.UpdateCurrentSection("===========================[ Machine Data ]===================================");

            // Act
            var result = _parser.GetTitleForCurrentSpecSection();

            // Assert
            Assert.Equal(string.Empty, result); // Or null, depending on desired behavior for non-spec sections
        }

        // --- Tests for GetOrCreateSpecCodeSection ---

        [Fact]
        public void GetOrCreateSpecCodeSection_NcSpecCode1_CreatesNewSectionIfNotExists()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            string expectedTitle = "NC-SPEC CODE No.1";

            // Act
            var section = _parser.GetOrCreateSpecCodeSection(card);

            // Assert
            Assert.NotNull(section);
            Assert.Equal(expectedTitle, section.SectionTitle);
            Assert.Single(card.NcSpecCodes);
            Assert.Same(section, card.NcSpecCodes[0]);
            Assert.Empty(card.PlcSpecCodes);
        }

        [Fact]
        public void GetOrCreateSpecCodeSection_PlcSpecCode2_CreatesNewSectionIfNotExists()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ PLC-SPEC CODE No.2 ]===========================");
            string expectedTitle = "PLC-SPEC CODE No.2";

            // Act
            var section = _parser.GetOrCreateSpecCodeSection(card);

            // Assert
            Assert.NotNull(section);
            Assert.Equal(expectedTitle, section.SectionTitle);
            Assert.Single(card.PlcSpecCodes);
            Assert.Same(section, card.PlcSpecCodes[0]);
            Assert.Empty(card.NcSpecCodes);
        }

        [Fact]
        public void GetOrCreateSpecCodeSection_ExistingSection_ReturnsExistingSection()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            var firstCallSection = _parser.GetOrCreateSpecCodeSection(card); // Create it

            // Act
            var secondCallSection = _parser.GetOrCreateSpecCodeSection(card); // Get existing

            // Assert
            Assert.NotNull(secondCallSection);
            Assert.Single(card.NcSpecCodes);
            Assert.Same(firstCallSection, secondCallSection);
        }

        [Fact]
        public void GetOrCreateSpecCodeSection_NonSpecSection_ReturnsNull()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ Machine Data ]===================================");

            // Act
            var section = _parser.GetOrCreateSpecCodeSection(card);

            // Assert
            Assert.Null(section);
            Assert.Empty(card.NcSpecCodes);
            Assert.Empty(card.PlcSpecCodes);
        }

        // --- Tests for ParseSpecCodeLine ---

        [Fact]
        public void ParseSpecCodeLine_SingleFeatureLine_ParsesCorrectly() // Renamed for clarity
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            string featureLine = "SLANT-Y AXIS     -";

            // Act
            _parser.ParseSpecCodeLine(featureLine, card);

            // Assert
            Assert.Single(card.NcSpecCodes);
            var section = card.NcSpecCodes[0];
            Assert.Equal("NC-SPEC CODE No.1", section.SectionTitle);
            Assert.Single(section.SpecCodes); // Check that one feature was added
            Assert.Equal("SLANT-Y AXIS", section.SpecCodes[0].Name);
            Assert.False(section.SpecCodes[0].IsEnabled);
            Assert.Empty(section.HexCodes); // HexCodes should be empty
        }

        [Fact]
        public void ParseSpecCodeLine_SpecFeatureLine_On_AddsToSpecCodes()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ PLC-SPEC CODE No.1 ]=============================");
            string featureLine = "Cool Feature     o"; // Updated to new format
            string expectedName = "Cool Feature";

            // Act
            _parser.ParseSpecCodeLine(featureLine, card);

            // Assert
            Assert.Single(card.PlcSpecCodes);
            var section = card.PlcSpecCodes[0];
            Assert.Equal("PLC-SPEC CODE No.1", section.SectionTitle);
            Assert.Single(section.SpecCodes);
            Assert.Equal(expectedName, section.SpecCodes[0].Name);
            Assert.True(section.SpecCodes[0].IsEnabled);
            Assert.Empty(section.HexCodes);
        }

        [Fact]
        public void ParseSpecCodeLine_SpecFeatureLine_Off_AddsToSpecCodes()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.2 ]==============================");
            string featureLine = "Another Spec     -"; // Updated to new format
            string expectedName = "Another Spec";

            // Act
            _parser.ParseSpecCodeLine(featureLine, card);

            // Assert
            Assert.Single(card.NcSpecCodes);
            var section = card.NcSpecCodes[0]; // Should be the first one added to NcSpecCodes
            Assert.Equal("NC-SPEC CODE No.2", section.SectionTitle);
            Assert.Single(section.SpecCodes);
            Assert.Equal(expectedName, section.SpecCodes[0].Name);
            Assert.False(section.SpecCodes[0].IsEnabled);
            Assert.Empty(section.HexCodes);
        }
        
        [Fact]
        public void ParseSpecCodeLine_SpecFeatureLine_WithExtraSpaces_ParsesCorrectly()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            string featureLine = "  Spaced Out Feature   o  "; // Updated to new format, outer spaces handled by Parse, inner by entry.Trim()
            string expectedName = "Spaced Out Feature";

            // Act
            _parser.ParseSpecCodeLine(featureLine, card); // ParseSpecCodeLine itself also trims

            // Assert
            Assert.Single(card.NcSpecCodes);
            var section = card.NcSpecCodes[0];
            Assert.Single(section.SpecCodes);
            Assert.Equal(expectedName, section.SpecCodes[0].Name);
            Assert.True(section.SpecCodes[0].IsEnabled);
        }


        [Fact]
        public void ParseSpecCodeLine_UnrecognizedLine_DoesNotAddAndLogs()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            string unrecognizedLine = "This is not a valid spec line";
            // We can't easily assert Console.WriteLine output without more setup,
            // but we can check that no data was added.

            // Act
            _parser.ParseSpecCodeLine(unrecognizedLine, card);

            // Assert
            Assert.Single(card.NcSpecCodes); // Section is created
            var section = card.NcSpecCodes[0];
            Assert.Empty(section.HexCodes);
            Assert.Empty(section.SpecCodes);
        }

        [Fact]
        public void ParseSpecCodeLine_EmptyLine_IsIgnored_And_NoSectionCreatedOrModified()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ NC-SPEC CODE No.1 ]==============================");
            string emptyLine = "   ";
        
            // Act
            _parser.ParseSpecCodeLine(emptyLine, card);
        
            // Assert
            // ParseSpecCodeLine should return early for an empty/whitespace line,
            // so no section should be created or modified by this call.
            Assert.Empty(card.NcSpecCodes); 
            Assert.Empty(card.PlcSpecCodes); // Also check the other list for completeness
        }

        [Fact]
        public void ParseSpecCodeLine_MultipleLines_CorrectlyPopulatesSection()
        {
            // Arrange
            var card = new SoftwareDataManagementCard();
            _parser.UpdateCurrentSection("===========================[ PLC-SPEC CODE No.3 ]===========================");
            string line1 = "FEATURE A        -  FEATURE B        o";
            string separatorLine = "------------------  ------------------"; // Should be ignored
            string line2 = "FEATURE C        o  FEATURE D        -";

            // Act
            _parser.ParseSpecCodeLine(line1, card);
            _parser.ParseSpecCodeLine(separatorLine, card);
            _parser.ParseSpecCodeLine(line2, card);

            // Assert
            Assert.Single(card.PlcSpecCodes);
            var section = card.PlcSpecCodes[0];
            Assert.Equal("PLC-SPEC CODE No.3", section.SectionTitle);
            Assert.Empty(section.HexCodes); // HexCodes list should be empty
            Assert.Equal(4, section.SpecCodes.Count); // Expecting 4 features
            Assert.Contains(section.SpecCodes, s => s.Name == "FEATURE A" && !s.IsEnabled);
            Assert.Contains(section.SpecCodes, s => s.Name == "FEATURE B" && s.IsEnabled);
            Assert.Contains(section.SpecCodes, s => s.Name == "FEATURE C" && s.IsEnabled);
            Assert.Contains(section.SpecCodes, s => s.Name == "FEATURE D" && !s.IsEnabled);
        }

        
    }

}
