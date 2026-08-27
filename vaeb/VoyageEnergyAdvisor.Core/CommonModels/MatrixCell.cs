namespace VoyageEnergyAdvisor.Core.CommonModels
{
    public record MatrixCell(double XAxisValue, double YAxisValue, double Value);

    public static class MatrixHelper
    {
        public static double GetClosestValue(this IEnumerable<MatrixCell> modelItems, double xValue, double yValue)
        {
            // Find the closest x value (column)
            var excelModelItems = modelItems.ToList();
            var closestXDistance = excelModelItems.Min(item => Math.Abs(item.XAxisValue - xValue));
            var closestXItems = excelModelItems
                .Where(item => Math.Abs(item.XAxisValue - xValue) == closestXDistance)
                .ToList();

            if (!closestXItems.Any())
            {
                throw new InvalidOperationException("No matching x value found in the model.");
            }

            // Find the closest y value (row) within the closest x values (columns)
            var closestYItem = closestXItems
                .OrderBy(item => Math.Abs(item.YAxisValue - yValue))
                .FirstOrDefault();

            if (closestYItem == null)
            {
                throw new InvalidOperationException("No matching y value found in the model.");
            }

            return closestYItem.Value;
        }
    }
}