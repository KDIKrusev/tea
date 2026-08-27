namespace VoyageEnergyAdvisor.Core.Services.WeatherProviders
{
    public interface ICancellationTokenService
    {
        CancellationToken Token { get; }
        void SetToken(CancellationToken token);
    }

    public class CancellationTokenService : ICancellationTokenService
    {
        private CancellationToken _token = CancellationToken.None;

        public CancellationToken Token => _token;

        public void SetToken(CancellationToken token)
        {
            _token = token;
        }
    }
}
