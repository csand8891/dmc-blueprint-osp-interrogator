using DmcBlueprint.Models;
using System;

namespace DmcBlueprint.Parsers.SectionParsers
{
    internal class CustomerDataSectionParser
    {
        private enum CustomerEntityType
        {
            Distributor,
            EndCustomer
        }

        private CustomerEntityType _currentCustomerEntityType = CustomerEntityType.Distributor;
        private string? _currentCustomerDataKey = null;

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
        public void ResetState()
        {
            _currentCustomerEntityType = CustomerEntityType.Distributor;
            _currentCustomerDataKey = null;
        }
    }
}