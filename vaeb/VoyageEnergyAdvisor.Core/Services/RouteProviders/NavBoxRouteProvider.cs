using Microsoft.Extensions.Options;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Models;
using VoyageEnergyAdvisor.Core.Services.RouteProviders;
using VoyageEnergyAdvisor.Core.Services.RouteService.RouteProviders;

namespace VoyageEnergyAdvisor.Core.Services.RouteService
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Configuration.RouteConfiguration.Models;
    using VoyageEnergyAdvisor.Core.Configuration.RouteConfiguration;

    public class NavBoxRouteProvider : IRouteProvider
    {
        public RouteProviderType RouteProviderType => RouteProviderType.NavBoxRouteProvider;

        private readonly HttpClient _client;
        private readonly ILogger<NavBoxRouteProvider> _logger;

        private readonly NavBoxRouteProviderConfiguration _navBoxConfiguration;
        private const string _authURL = "auth/token";
        private const string _routesURL = "api/public/v1/routes/routes/";
        private const string _rtzFormat = "cirmrtz";
        private const string _clinetHostname = "EcoAdvisor";
        private const string _softwareName = "EcoAdvisor";
        private string _token = string.Empty;
        private DateTime? _tokenValidTo;


        public NavBoxRouteProvider(IOptions<NavBoxRouteProviderConfiguration> config, ILogger<NavBoxRouteProvider> logger)
        {
            _logger = logger;
            _navBoxConfiguration = config.Value;
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            _client = new HttpClient(httpClientHandler);
        }

        public List<string> GetRoutesList()
        {
            if (RefreshClient())
            {
                var requestResult = _client.GetAsync(_navBoxConfiguration.ApiUrl + _routesURL);
                var json = requestResult.Result.Content.ReadAsStringAsync().Result;
                try
                {
                    List<RouteInfo>? response = JsonConvert.DeserializeObject<List<RouteInfo>>(json);
                    if (response != null)
                    {
                        List<string>? result = response.Select(r => r.Id).ToList();
                        return result;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Get request for Routes List returned unexpected data format");
                    return null!;
                }
            }
            else
            {
                _logger.LogError($"Get request for Routes List failed");
                return null!;
            }

            return null!;
        }

        public Route? GetRoute(string id)
        {
            if (RefreshClient())
            {
                var requestResult = _client.GetAsync($"{_navBoxConfiguration.ApiUrl}{_routesURL}{id}?routeFormat={_rtzFormat}");
                var responseResult = requestResult.Result.Content.ReadAsStringAsync().Result;
                Route? result = null;
                try
                {
                    result = RouteXmlSerializer.DeserializeRouteFromString(responseResult);
                    return result;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Get request for Route {id} returned unexpected data format");
                    return result;
                }
            }
            else
            {
                _logger.LogError($"Get request for Route {id} failed");
                return null;
            }
        }


        private bool RefreshClient()
        {
            var token = GetValidToken();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            if (_client.DefaultRequestHeaders.Any(x => x.Key == "Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");
            }
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            return true;
        }

        private string GetValidToken()
        {
            if (_tokenValidTo.HasValue)
            {
                var remaining = _tokenValidTo.Value - DateTime.Now;
                if (remaining.TotalSeconds > 10)
                {
                    return _token;
                }
            }

            var newLine = Environment.NewLine;
            var content = new StringContent($"{{ {newLine}" +
                $"oem_token: \"{_navBoxConfiguration.OemToken}\",  {newLine}" +
                $"navbox_token: \"{_navBoxConfiguration.NavboxToken}\",  {newLine}" +
                $"client_hostname: \"{_clinetHostname}\",  {newLine}" +
                $"software_name: \"{_softwareName}\",  {newLine}" +
                $"software_version: \"1.0.0\",  {newLine}" +
                $"}}", System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            NavTorTokenResponse responseResult;
            try
            {
                response = _client.PostAsync(_navBoxConfiguration.ApiUrl + _authURL, content).Result;
                responseResult = JsonConvert.DeserializeObject<NavTorTokenResponse>(response.Content.ReadAsStringAsync().Result)!; // TODO check null forgiving operator
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting with NavBoxProvider. Please check connection configuration!");
                return string.Empty;
            }
            try
            {
                _tokenValidTo = DateTime.Now.AddSeconds(responseResult.ExpiresInSeconds);
                _token = responseResult.Token!; // TODO check null forgiving operator
                return _token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining authorization token from NavBoxProvider!");
                return string.Empty;
            }
        }

    }
}
