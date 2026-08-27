namespace VoyageEnergyAdvisor.Core.Services.ProgressService
{
    using Microsoft.AspNetCore.SignalR;

    public class ProgressService : IProgressService
    {
        private readonly IHubContext<ProgressHub> _hubContext;

        public ProgressService(IHubContext<ProgressHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task UpdateProgress(double percent, string description)
        {
            await _hubContext.Clients.All.SendAsync("UpdateProgress", new { Progress = percent, Description = description });
        }

        public async Task UpdateProgressDynamic(int currentItem, int totalItems, double startPercent, double endPercent, string description)
        {
            if (totalItems == 0)
                return;

            double localProgress = currentItem / (double)totalItems;
            double globalProgress = startPercent + localProgress * (endPercent - startPercent);

            await UpdateProgress(globalProgress, description);
        }
    }
}
