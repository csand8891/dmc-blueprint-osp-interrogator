using DmcBlueprint.Models;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DmcBlueprint.Parsers
{
    public class DataManagementCardParsers
    {



        private enum CurrentSection
        {
            None,
            Header,
            MachineIdentification,
            DistributorAndCustomerInfo,
            RevisionAndCustomizationInfo,
            DvdMediaVersionData,
            SoftwareVersionIdentifier,
            SoftwarePackageComposition,
            
        }
        private CurrentSection _currentSection = CurrentSection.None;
        private string _currentCustomSoftGroup = null;
    }
}