namespace VoyageEnergyAdvisor.Data.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using System.Text;
    using System.Xml.Linq;
    using VoyageEnergyAdvisor.Data.Entities;

    public static class RouteSeedExtensions
    {
        public static async Task SeedRoutes(this IServiceProvider serviceProvider, string defaultResourcesPath)
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var rtzPath = Path.Combine(defaultResourcesPath, "RtzData");
            if (!Directory.Exists(rtzPath))
            {
                Console.WriteLine($"⚠️ RTZ directory {rtzPath} does not exist.");
                return;
            }

            var rtzFiles = Directory.GetFiles(rtzPath, "*.rtz");
            foreach (var filePath in rtzFiles)
            {
                try
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string utf8Xml = Encoding.UTF8.GetString(fileBytes).Trim('\uFEFF');
                    XDocument xmlDoc = XDocument.Parse(utf8Xml);

                    string utf16Xml;
                    using (var writer = new Utf8StringWriter())
                    {
                        xmlDoc.Save(writer);
                        utf16Xml = writer.ToString();
                    }

                    var fileName = Path.GetFileName(filePath);

                    if (!await dbContext.Routes.AnyAsync(r => r.RouteName == fileName))
                    {
                        var route = new Route
                        {
                            RouteName = fileName,
                            RouteXml = utf16Xml
                        };

                        await dbContext.Routes.AddAsync(route);
                        Console.WriteLine($"✅ Added RTZ route {fileName} from {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to process {filePath}: {ex.Message}");
                }
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.Unicode; // UTF-16
    }
}
