using Newtonsoft.Json;
using VoyageEnergyAdvisor.Core.Services.SailContributionService.Models;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using VoyageEnergyAdvisor.Core.Services.CalmWaterResistanceService.Models;
using VoyageEnergyAdvisor.Core.Services.CurrentResistanceService.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Helpers.WaveResistanceHelper;
using VoyageEnergyAdvisor.Core.Services.WindResistanceService.Models;

namespace VesselModelReader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: VesselModelReader <inputFilePath>");
                return;
            }
            string inputFilePath = args[0];
            var outputDirectory = Path.GetDirectoryName(inputFilePath);
            
            if (outputDirectory != null)
            {
                var calmWaterResistanceServiceConfiguration = GetCalmWaterResistanceServiceConfiguration(inputFilePath);
                WriteOutput(calmWaterResistanceServiceConfiguration, outputDirectory);

                var windResistanceServiceConfiguration = GetWindResistanceServiceConfiguration(inputFilePath);
                WriteOutput(windResistanceServiceConfiguration, outputDirectory);

                var sailContributionServiceConfiguration = GetSailContributionConfiguration(inputFilePath);
                WriteOutput(sailContributionServiceConfiguration, outputDirectory);

                var currentResistanceServiceConfiguration = GetCurrentResistanceServiceConfiguration(inputFilePath);
                WriteOutput(currentResistanceServiceConfiguration, outputDirectory);
            }
        }

        static CalmWaterResistanceServiceConfiguration GetCalmWaterResistanceServiceConfiguration(string inputFilePath)
        {
            string workSheetName = "Vessel"; // TODO clarify
            string upperLeftCornerSailContributions = "BK38";
            int rowCountCalmWaterResistance = 576;
            int columnCountCalmWaterResistance = 1;
            
            var calmWaterResistance = ExcelFileModelReader.ReadModel(inputFilePath, workSheetName, upperLeftCornerSailContributions, rowCountCalmWaterResistance, columnCountCalmWaterResistance);
            
            return new CalmWaterResistanceServiceConfiguration()
            {
                CalmWaterResistanceItems = calmWaterResistance.Select(e => new CalmWaterResistanceServiceConfigurationItem(e.YAxisValue * 0.514444444 , e.Value)).ToList() // Note: Here we are converting form n to m/s
            };
        }
        
        static WindResistanceServiceConfiguration GetWindResistanceServiceConfiguration(string inputFilePath)
        {
            string workSheetName = "WM_AW_onlySHIP";
            string upperLeftCornerWindResistanceConfiguration = "D4";
            int rowCountWindResistanceConfiguration = 26;
            int columnCountWindResistanceConfiguration = (355 / 5) + 1;
            var windResistances = ExcelFileModelReader.ReadModel(inputFilePath, workSheetName, upperLeftCornerWindResistanceConfiguration, rowCountWindResistanceConfiguration, columnCountWindResistanceConfiguration);
            
            return new WindResistanceServiceConfiguration()
            {
                WindResistanceItems = windResistances.Select(e => new WindResistanceServiceConfigurationItem(e.XAxisValue, e.YAxisValue, -e.Value)).ToList()  // - as sheet contains contribution
            };
        }
        
        static CurrentResistanceServiceConfiguration GetCurrentResistanceServiceConfiguration(string inputFilePath)
        {
            string workSheetName = "MX_CURRENT-SOG"; // TODO clarify
            string upperLeftCornerCurrentResistanceConfiguration = "Z93";
            int rowCountCurrentResistanceConfiguration = 26;
            int columnCountCurrentResistanceConfiguration = (355 / 5) + 1;
            var currentResistances = ExcelFileModelReader.ReadModel(inputFilePath, workSheetName, upperLeftCornerCurrentResistanceConfiguration, rowCountCurrentResistanceConfiguration, columnCountCurrentResistanceConfiguration);
            
            return new CurrentResistanceServiceConfiguration()
            {
                // TODO only have one speed point - 10.5 knots, so we can hardcode it
                CurrentResistanceItems = currentResistances.Select(e => new CurrentResistanceServiceConfigurationItem(10.5 * 0.5144444448, e.XAxisValue, e.YAxisValue,e.Value)).ToList()
            };
        }
        
        static SailContributionServiceConfiguration GetSailContributionConfiguration(string inputFilePath)
        {
            string workSheetName = "WM_AW_onlySAILS";
            string upperLeftCornerSailContributions = "D4";
            int rowCountSailContributions = 26;
            int columnCountSailContributions = (355 / 5) + 1;
            
            string upperLeftCornerSailPowers = "D62";
            int rowCountSailPowers = 26;
            int columnCountSailPowers = (355 / 5) + 1;

            var sailContributions = ExcelFileModelReader.ReadModel(inputFilePath, workSheetName, upperLeftCornerSailContributions, rowCountSailContributions, columnCountSailContributions);
            var sailActivePowers = ExcelFileModelReader.ReadModel(inputFilePath, workSheetName, upperLeftCornerSailPowers, rowCountSailPowers, columnCountSailPowers);

            return new SailContributionServiceConfiguration()
            {
                SailContributions = sailContributions.Select(e => new SailContributionItem(e.XAxisValue, e.YAxisValue, e.Value)).ToList(),
                SailActivePowers = sailActivePowers.Select(e => new SailActivePowerItem(e.XAxisValue, e.YAxisValue, e.Value)).ToList()
            };
        }

        static void WriteOutput<T>(T configuration, string outputDirectory)
        {
            WriteJsonOutput(configuration, outputDirectory);
            // WriteXmlOutput(configuration, baseFilePath + ".xml");
        }

        static void WriteJsonOutput<T>(T configuration, string outputDirectory)
        {
            var typeName = typeof(T).Name;
            if (configuration != null)
            {
                var wrappedObject = new Dictionary<string, object>
                {
                    { typeName, configuration }
                };

                var json = JsonConvert.SerializeObject(wrappedObject, Formatting.Indented);
                var outputFilePath = Path.Combine(outputDirectory, typeName + ".json");
                File.WriteAllText(outputFilePath, json);
                Console.WriteLine($"Configuration converted and saved to {outputFilePath}");
            }
        }

        // static void WriteXmlOutput<T>(T configuration, string outputFilePath)
        // {
        //     var serializer = new XmlSerializer(typeof(SailContributionServiceConfiguration));
        //     using (var writer = new StreamWriter(outputFilePath))
        //     {
        //         serializer.Serialize(writer, configuration);
        //     }
        //     Console.WriteLine($"Configuration converted and saved to {outputFilePath}");
        // }
    }
}