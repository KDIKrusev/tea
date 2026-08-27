using ClosedXML.Excel;
using VesselModelReader;
using VoyageEnergyAdvisor.Core.CommonModels;
using Xunit;

namespace VoyageEnergyAdvisorService.Test
{
    public class ExcelFileModelReaderTests
    {
        [Fact]
        public void ReadModel_ShouldReturnCorrectModelItems()
        {
            // Arrange
            var filePath = "test.xlsx";
            var tableUpperLeftCorner = "A1";
            var rowCount = 2;
            var columnCount = 3;
            var workSheetName = "testSheet";

            // Create a test workbook
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(workSheetName);
                worksheet.Cell("A1").Value = "Y\\X";
                worksheet.Cell("B1").Value = "1";
                worksheet.Cell("C1").Value = "2";
                worksheet.Cell("D1").Value = "3";
                worksheet.Cell("A2").Value = "1";
                worksheet.Cell("B2").Value = "1.1";
                worksheet.Cell("C2").Value = "1.2";
                worksheet.Cell("D2").Value = "1.3";
                worksheet.Cell("A3").Value = "2";
                worksheet.Cell("B3").Value = "2.1";
                worksheet.Cell("C3").Value = "2.2";
                worksheet.Cell("D3").Value = "2.3";
                workbook.SaveAs(filePath);
            }

            // Act
            var result = ExcelFileModelReader.ReadModel(filePath, workSheetName, tableUpperLeftCorner, rowCount, columnCount).ToList();

            // Assert
            Assert.Equal(6, result.Count);
            Assert.Contains(result, item => item.XAxisValue == 1 && item.YAxisValue == 1 && item.Value == 1.1);
            Assert.Contains(result, item => item.XAxisValue == 2 && item.YAxisValue == 2 && item.Value == 2.2);
        }

        [Fact]
        public void GetClosestValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var modelItems = new List<MatrixCell>
            {
                new MatrixCell(1, 1, 1.1),
                new MatrixCell(1, 2, 1.2),
                new MatrixCell(2, 1, 2.1),
                new MatrixCell(2, 2, 2.2)
            };

            // Act
            var result = modelItems.GetClosestValue(1.8, 1.7);

            // Assert
            Assert.Equal(2.2, result);
        }
    }
}