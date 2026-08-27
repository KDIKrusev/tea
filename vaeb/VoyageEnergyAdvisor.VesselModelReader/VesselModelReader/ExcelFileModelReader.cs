using ClosedXML.Excel;
using VoyageEnergyAdvisor.Core.CommonModels;

namespace VesselModelReader
{
    public static class ExcelFileModelReader
    {
        public static IEnumerable<MatrixCell> ReadModel(string filePath, string workSheetName, string tableUpperLeftCorner, int rowCount, int columnCount)
        {
            var modelItems = new List<MatrixCell>();

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(workSheetName);
            var upperLeftCornerCell = worksheet.Cell(tableUpperLeftCorner);
            var startColumn = upperLeftCornerCell.WorksheetColumn().ColumnNumber();
            var startRow = upperLeftCornerCell.WorksheetRow().RowNumber();

            var xAxisValues = new List<double>();
            for (int col = startColumn + 1; col < startColumn + columnCount + 1; col++)
            {
                var cell = worksheet.Cell(startRow, col);
                if (double.TryParse(cell.GetValue<string>(), out double angle))
                {
                    xAxisValues.Add(angle);
                }
                else
                {
                    xAxisValues.Add(0);
                }
            }

            for (int row = startRow + 1; row < startRow + rowCount + 1; row++)
            {
                var yAxis = worksheet.Cell(row, startColumn);
                if (double.TryParse(yAxis.GetValue<string>(), out double yAxisValue))
                {
                    for (int col = startColumn + 1; col < startColumn + columnCount + 1; col++)
                    {
                        var tableValue = worksheet.Cell(row, col);
                        if (double.TryParse(tableValue.GetValue<string>(), out double val))
                        {
                            modelItems.Add(new MatrixCell(xAxisValues[col - startColumn - 1], yAxisValue, val));
                        }
                    }
                }
            }
            return modelItems;
        }
        
        public static void ExcelWriteCell(string filePath, string workSheetName, string cellAddress, double value)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(workSheetName);

            // Write the value to the specified cell
            var cell = worksheet.Cell(cellAddress);
            cell.Value = value;

            // Save the changes back to the file
            workbook.Save();
        }
    }
}