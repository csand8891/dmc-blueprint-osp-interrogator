// See https://aka.ms/new-console-template for more information
using System;
using System.IO; // For File.Exists, Path.GetFullPath, File.WriteAllText
using System.Linq; // For .Any(), .First(), .DefaultIfEmpty()
using DmcBlueprint.Models;
using DmcBlueprint.Parsers;

class Program
{
    static void Main(string[] args)
    {
        DemonstrateDmcParsing();
    }

    static void DemonstrateDmcParsing()
    {
        Console.WriteLine("DMC Blueprint Parser Demonstration");
        Console.WriteLine("================================\n");

        // Define the output file path
        string outputFilePath = "parsing_output.txt";
        FileStream fs = new FileStream(outputFilePath, FileMode.Create); // Create or overwrite the file
        StreamWriter writer = new StreamWriter(fs) { AutoFlush = true }; // AutoFlush is convenient for seeing output as it happens

        // Save the original console output stream
        TextWriter originalConsoleOut = Console.Out;

        // Redirect console output to the file
        Console.SetOut(writer);

        Console.WriteLine($"DMC Blueprint Parser Demonstration - Outputting to {Path.GetFullPath(outputFilePath)}");
        Console.WriteLine("===================================================================");

        var parser = new DataManagementCardParser();
        
        // IMPORTANT: You can replace this with the actual path to your DMC file
        string filePath = "sample.dmc"; 

        // This line will now go to the file
        Console.WriteLine($"\n--- Initial Console Output (will be in {outputFilePath}) ---");

        Console.WriteLine($"Attempting to parse DMC file: {Path.GetFullPath(filePath)}");

        // Create a dummy sample.dmc if it doesn't exist to make it runnable
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"'{filePath}' not found. Creating a sample file for demonstration...");
            CreateSampleDmcFile(filePath);
            if (!File.Exists(filePath)) 
            {
                 Console.WriteLine($"Failed to create sample file at '{filePath}'. Please create it manually or provide a valid path.");
                 return;
            }
            Console.WriteLine($"Sample file '{filePath}' created successfully.");
        }

        try
        {
            // Function 1: Parsing the DMC file
            SoftwareDataManagementCard card = parser.Parse(filePath);
            Console.WriteLine("\nFile Parsed Successfully!");
            Console.WriteLine("------------------------------------");

            // Function 2: Displaying some of the parsed data
            DisplayParsedCardInfo(card);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Error: The file was not found at '{filePath}'. Please check the path.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An IO error occurred: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during parsing: {ex.Message}");
            Console.WriteLine("Stack Trace:");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            // Ensure the output is written to the file and close the writer
            Console.Out.Flush(); // Ensure everything is written
            Console.SetOut(originalConsoleOut); // Restore original console output
            writer.Close();
            fs.Close();

            Console.WriteLine($"\n--- End of File Output ---"); // This will go to the actual console
            Console.WriteLine($"Parsing output has been saved to: {Path.GetFullPath(outputFilePath)}");
        }
    }

    static void DisplayParsedCardInfo(SoftwareDataManagementCard card)
    {
        Console.WriteLine("\nParsed Data Highlights:");
        Console.WriteLine("-----------------------");

        if (card.MachineDetails != null)
        {
            Console.WriteLine($"  Machine OSP Type: {card.MachineDetails.OspType ?? "N/A"}");
            Console.WriteLine($"  Software Production No: {card.MachineDetails.SoftwareProductionNumber ?? "N/A"}");
            Console.WriteLine($"  Project No: {card.MachineDetails.ProjectNumber ?? "N/A"}");
            Console.WriteLine($"  Production Date: {(card.MachineDetails.SoftwareProductionDate == default(DateTime) ? "N/A" : card.MachineDetails.SoftwareProductionDate.ToShortDateString())}");
            Console.WriteLine($"  Machine Types: {string.Join(", ", card.MachineDetails.MachineType.DefaultIfEmpty("N/A"))}");
        }
        else
        {
            Console.WriteLine("  Machine Details: Not available.");
        }

        if (card.DistributorAndCustomerDetails != null)
        {
            Console.WriteLine($"  Distributor Name: {card.DistributorAndCustomerDetails.Distributor?.Name ?? "N/A"}");
            Console.WriteLine($"  End Customer Name: {card.DistributorAndCustomerDetails.EndCustomer?.Name ?? "N/A"}");
        }
        else
        {
            Console.WriteLine("  Distributor and Customer Details: Not available.");
        }
        
        if (card.RevisionAndCustomization != null)
        {
            Console.WriteLine($"  Number of Revision Entries: {card.RevisionAndCustomization.RevisionEntries.Count}");
            if (card.RevisionAndCustomization.RevisionEntries.Any())
            {
                var firstRevision = card.RevisionAndCustomization.RevisionEntries.First();
                Console.WriteLine($"    First Revision ID: {firstRevision.Identifier ?? "N/A"}, Date: {(firstRevision.SoftwareProductionDate == default(DateTime) ? "N/A" : firstRevision.SoftwareProductionDate.ToShortDateString())}, SO#: {firstRevision.SalesOrderNumber ?? "N/A"}, P#: {firstRevision.ProjectNumber ?? "N/A"}");
            }
        }
        else
        {
            Console.WriteLine("  Revision and Customization data: Not available.");
        }

        Console.WriteLine($"  NC Spec Codes Sections: {card.NcSpecCodes.Count}");
        if (card.NcSpecCodes.Any())
        {
            var firstNcSection = card.NcSpecCodes.First();
            Console.WriteLine($"    First NC Section: '{firstNcSection.SectionTitle ?? "N/A"}' has {firstNcSection.SpecCodes.Count} features.");
            if (firstNcSection.SpecCodes.Any())
            {
                var firstFeature = firstNcSection.SpecCodes.First();
                Console.WriteLine($"      First Feature: No.{firstFeature.Number} Bit.{firstFeature.Bit} - {firstFeature.Name} (Enabled: {firstFeature.IsEnabled})");
            }
            if (firstNcSection.HexCodes.Any())
            {
                Console.WriteLine($"      First Hex Code Line: {firstNcSection.HexCodes.First()}");
            }
        }

        Console.WriteLine($"  PLC Spec Codes Sections: {card.PlcSpecCodes.Count}");
        if (card.PlcSpecCodes.Any())
        {
            var firstPlcSection = card.PlcSpecCodes.First();
            Console.WriteLine($"    First PLC Section: '{firstPlcSection.SectionTitle ?? "N/A"}' has {firstPlcSection.SpecCodes.Count} features.");
             if (firstPlcSection.SpecCodes.Any())
            {
                var firstFeature = firstPlcSection.SpecCodes.First();
                Console.WriteLine($"      First Feature: No.{firstFeature.Number} Bit.{firstFeature.Bit} - {firstFeature.Name} (Enabled: {firstFeature.IsEnabled})");
            }
            if (firstPlcSection.HexCodes.Any())
            {
                Console.WriteLine($"      First Hex Code Line: {firstPlcSection.HexCodes.First()}");
            }
        }

        Console.WriteLine($"  Software Packages: {card.SoftwarePackageComposition.Count}");
        if (card.SoftwarePackageComposition.Any())
        {
            var firstPackage = card.SoftwarePackageComposition.First();
            Console.WriteLine($"    First Package: {firstPackage.PackageName ?? "N/A"} - {firstPackage.Identifier ?? "N/A"}");
        }
        
        Console.WriteLine($"  Custom Software Groups: {card.CustomSoftwareComposition.Count}");
        if (card.CustomSoftwareComposition.Any())
        {
            var firstCustomGroup = card.CustomSoftwareComposition.First();
            Console.WriteLine($"    First Custom Group: '{firstCustomGroup.GroupName ?? "N/A"}' has {firstCustomGroup.FilePaths.Count} files.");
            if (firstCustomGroup.FilePaths.Any())
            {
                 Console.WriteLine($"      First File Path: {firstCustomGroup.FilePaths.First()}");
            }
        }
        Console.WriteLine("------------------------------------");
    }

    static void CreateSampleDmcFile(string filePath)
    {
        // A minimal DMC file content for demonstration
        string sampleContent = @"===========================[ Machine Data ]===================================
<Type of OSP>
OSP-P300S
<Type of Machine>
MB-5000H
<Soft Production No>
#12345
<Project No>
P67890
<Software Production Date>
2023-01-15
===========================[ Customer Data ]====================================
<Name>
Okuma America
<Address>
123 Okuma Way
<Phone>
555-1234
<Customer>
<Name>
End User Corp
<Address>
456 User Drive
<Phone>
555-5678
===========================[ NOTE ]=============================================
1  01/15/23  SO#001  P#001 Initial Release
A  02/20/23  SO#002  P#002 Update Feature X
===========================[ DVD Media Version Data ]===========================
[Windows System CD Version]
01.02
[OSP System CD Version]
03.04
===========================[ Soft Version Excepted OSP System CD ]============
[Windows System Version]
10.0.19045
[Custom API Additional DVD Version]
API_V1.0
[MTconnect Version]
MTConnect_V1.5
============================[ Package Soft composition ]======================
[NC INSTALLER]
P300_NC_INSTALLER_L01-01_01.01.01.00R0001_11-1J
[OSP SUITE]
OSP_SUITE_V2.0
============================[ NC Custom Soft composition ]=====================
[LPP]
C:\OSP-P\P-MANUAL\LPP\ENG\LPP627C-ENG.CNT
C:\OSP-P\P-MANUAL\LPP\JPN\LPP627C-JPN.CNT
[HMI]
C:\OSP-P\HMI\PDSN401-LU302X.DLL
===========================[ NC-SPEC CODE No.1 ]==============================
SLANT-Y AXIS     -  ONE TOUCH IGF ADVo
Y AXIS BY CT-Z   -  TAPE DATA IN/OUT o
===========================[ PLC-SPEC CODE No.1 ]=============================
HI-G CONTROL     o  PROGRAM SELECT   -
PFC‡U            -  MG TOOL READY    -
";
        try
        {
            File.WriteAllText(filePath, sampleContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating sample file '{filePath}': {ex.Message}");
        }
    }
}
