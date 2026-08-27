namespace VoyageEnergyAdvisor.Core.Services.ProgressService
{
    public interface IProgressService
    {
        Task UpdateProgress(double percent, string description);
        Task UpdateProgressDynamic(int currentItem, int totalItems, double startPercent, double endPercent, string description);
    }
}
