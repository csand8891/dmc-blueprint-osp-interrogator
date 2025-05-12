using Xunit;
using DmcBlueprint.Models;
using DmcBlueprint.Parsers.SectionParsers;
using System.Collections.Generic; // Required for List<string> if you pass lines directly

namespace DmcBlueprint.Tests.Parsers.SectionParsers
{
    public class CustomerDataSectionParserTests
    {
        [Fact]
        public void ParseLine_WithDistributorAndCustomerData_PopulatesCorrectly()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();

            // Act
            // Simulate lines as they would be passed from DataManagementCardParser
            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("Distributor A", customerInfo);
            parser.ParseLine("<Address>", customerInfo);
            parser.ParseLine("123 Distributor St", customerInfo);
            parser.ParseLine("Distro City, DS 12345", customerInfo);
            parser.ParseLine("<Phone>", customerInfo);
            parser.ParseLine("555-0100", customerInfo);

            parser.ParseLine("<Customer>", customerInfo); // Switch to EndCustomer

            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("End User B", customerInfo);
            parser.ParseLine("<Address>", customerInfo);
            parser.ParseLine("456 End User Ave", customerInfo);
            parser.ParseLine("User Town, UT 67890", customerInfo);
            parser.ParseLine("<Phone>", customerInfo);
            parser.ParseLine("555-0200", customerInfo);

            // Assert
            Assert.Equal("Distributor A", customerInfo.Distributor.Name);
            Assert.Equal("123 Distributor St" + System.Environment.NewLine + "Distro City, DS 12345", customerInfo.Distributor.Address);
            Assert.Equal("555-0100", customerInfo.Distributor.Phone);

            Assert.Equal("End User B", customerInfo.EndCustomer.Name);
            Assert.Equal("456 End User Ave" + System.Environment.NewLine + "User Town, UT 67890", customerInfo.EndCustomer.Address);
            Assert.Equal("555-0200", customerInfo.EndCustomer.Phone);
        }

        [Fact]
        public void ParseLine_OnlyDistributorData_PopulatesDistributorCorrectly()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();

            // Act
            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("Distributor Only", customerInfo);
            parser.ParseLine("<Phone>", customerInfo);
            parser.ParseLine("555-0300", customerInfo);

            // Assert
            Assert.Equal("Distributor Only", customerInfo.Distributor.Name);
            Assert.Null(customerInfo.Distributor.Address); // Address was not provided
            Assert.Equal("555-0300", customerInfo.Distributor.Phone);

            Assert.Null(customerInfo.EndCustomer.Name); // EndCustomer should remain empty
            Assert.Null(customerInfo.EndCustomer.Address);
            Assert.Null(customerInfo.EndCustomer.Phone);
        }

        [Fact]
        public void ParseLine_OnlyEndCustomerData_PopulatesEndCustomerCorrectly()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();

            // Act
            parser.ParseLine("<Customer>", customerInfo); // Switch to EndCustomer first
            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("Customer Only Inc.", customerInfo);
            parser.ParseLine("<Address>", customerInfo);
            parser.ParseLine("789 Customer Rd", customerInfo);

            // Assert
            Assert.Null(customerInfo.Distributor.Name); // Distributor should remain empty

            Assert.Equal("Customer Only Inc.", customerInfo.EndCustomer.Name);
            Assert.Equal("789 Customer Rd", customerInfo.EndCustomer.Address);
            Assert.Null(customerInfo.EndCustomer.Phone); // Phone was not provided
        }

        [Fact]
        public void ParseLine_MultiLineAddress_AppendsCorrectly()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();

            // Act
            parser.ParseLine("<Address>", customerInfo);
            parser.ParseLine("Line 1 of Address", customerInfo);
            parser.ParseLine("Line 2 of Address", customerInfo);
            parser.ParseLine("Line 3, City, State ZIP", customerInfo);

            // Assert
            string expectedAddress = "Line 1 of Address" + System.Environment.NewLine +
                                     "Line 2 of Address" + System.Environment.NewLine +
                                     "Line 3, City, State ZIP";
            Assert.Equal(expectedAddress, customerInfo.Distributor.Address);
        }

        [Fact]
        public void ParseLine_UnknownKey_IsIgnoredAndLogsWarning()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();
            // We can't directly test Console.WriteLine easily without more setup.
            // For now, we'll just ensure it doesn't crash and known fields are unaffected.

            // Act
            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("Known Distributor", customerInfo);
            parser.ParseLine("<UnknownKey>", customerInfo);
            parser.ParseLine("Some Unknown Value", customerInfo);
            parser.ParseLine("<Phone>", customerInfo);
            parser.ParseLine("555-0400", customerInfo);

            // Assert
            Assert.Equal("Known Distributor", customerInfo.Distributor.Name);
            Assert.Equal("555-0400", customerInfo.Distributor.Phone);
            Assert.Null(customerInfo.Distributor.Address); // Should not be affected by unknown key
        }

        [Fact]
        public void ResetState_SwitchesBackToDistributorAndClearsKey()
        {
            // Arrange
            var parser = new CustomerDataSectionParser();
            var customerInfo = new DistributorAndCustomerInfo();

            // Simulate being in EndCustomer state with a key
            parser.ParseLine("<Customer>", customerInfo);
            parser.ParseLine("<Name>", customerInfo); // Sets _currentCustomerDataKey
            parser.ParseLine("Initial Customer", customerInfo);

            // Act
            parser.ResetState();

            // Try parsing a new line, it should apply to Distributor
            parser.ParseLine("<Name>", customerInfo);
            parser.ParseLine("New Distributor After Reset", customerInfo);

            // Assert
            // Check that "Initial Customer" was applied to EndCustomer before reset
            Assert.Equal("Initial Customer", customerInfo.EndCustomer.Name);
            // Check that "New Distributor After Reset" was applied to Distributor after reset
            Assert.Equal("New Distributor After Reset", customerInfo.Distributor.Name);
        }
    }
}