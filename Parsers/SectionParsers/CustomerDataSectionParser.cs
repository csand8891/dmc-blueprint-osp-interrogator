using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    /// <summary>
    /// Parses the "[ Customer Data ]" section of a DMC file.
    /// This section typically contains contact information for a distributor and an end customer.
    /// The parser differentiates between these two entities and populates the <see cref="DistributorAndCustomerInfo"/> model.
    /// </summary>
    internal class CustomerDataSectionParser
    {
        /// <summary>
        /// Defines the type of customer entity currently being parsed (Distributor or EndCustomer).
        /// </summary>
        private enum CustomerEntityType
        {
            Distributor,
            EndCustomer
        }

        private CustomerEntityType _currentCustomerEntityType = CustomerEntityType.Distributor;
        private string? _currentCustomerDataKey = null;

        /// <summary>
        /// Parses a single line from the "[ Customer Data ]" section of a DMC file.
        /// It identifies keys (e.g., "&lt;Name&gt;", "&lt;Address&gt;") and their corresponding values,
        /// assigning them to the appropriate contact (Distributor or EndCustomer) in the <see cref="DistributorAndCustomerInfo"/> object.
        /// </summary>
        /// <param name="line">The line of text to parse. This line is expected to be pre-trimmed.</param>
        /// <param name="customerInfo">The <see cref="DistributorAndCustomerInfo"/> object to populate with parsed data.</param>
        public void ParseLine(string line, DistributorAndCustomerInfo customerInfo)
        {
            // Note: 'line' is already trimmed by the main Parse loop.
            string valueCandidate = line; // Already trimmed

            if (line.StartsWith("<") && line.EndsWith(">"))
            {
                // This is a key line
                string key = line.Substring(1, line.Length - 2).Trim();
                if (key == "Customer")
                {
                    _currentCustomerEntityType = CustomerEntityType.EndCustomer;
                    _currentCustomerDataKey = null; // Reset key when switching entity
                }
                else
                {
                    _currentCustomerDataKey = key;
                }
            }
            else if (!string.IsNullOrWhiteSpace(valueCandidate) && _currentCustomerDataKey != null)
            {
                // This is a value line for the current key and entity
                ContactEntry currentContact = _currentCustomerEntityType == CustomerEntityType.Distributor
                    ? customerInfo.Distributor
                    : customerInfo.EndCustomer;

                switch (_currentCustomerDataKey)
                {
                    case "Name":
                        currentContact.Name = valueCandidate;
                        break;
                    case "Address":
                        // For multi-line addresses, append if already started
                        if (string.IsNullOrEmpty(currentContact.Address))
                            currentContact.Address = valueCandidate;
                        else
                            currentContact.Address += Environment.NewLine + valueCandidate;
                        break;
                    case "Phone":
                        currentContact.Phone = valueCandidate;
                        break;
                    default:
                        Console.WriteLine($"Warning: Encountered value '{valueCandidate}' for unhandled key '{_currentCustomerDataKey}' in CustomerData section for entity '{_currentCustomerEntityType}'.");
                        break;
                }
            }
        }
        /// <summary>
        /// Resets the internal state of the parser.
        /// This should be called when starting to parse a new DMC file or before re-parsing,
        /// to ensure the parser defaults to the <see cref="CustomerEntityType.Distributor"/> and clears the current key.
        /// </summary>
        public void ResetState()
        {
            _currentCustomerEntityType = CustomerEntityType.Distributor;
            _currentCustomerDataKey = null;
        }
    }
}