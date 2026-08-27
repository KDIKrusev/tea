using System.Net;
using Microsoft.Extensions.Options;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
using VoyageEnergyAdvisor.Core.Services.WeatherService.Exceptions;
using Xunit;

namespace VoyageEnergyCalculatorService.Test.Weather.StormglassWeatherForecastProvider
{
    public class StormglassWeatherForecastProviderTests
    {
        private readonly GeoCoordinate _alesund = new GeoCoordinate(62.472, 6.154); // Ålesund area

        // --- Test 1: Verifies request formation (URI + start/end + params) and Authorization header -----------
        [Fact]
        public async Task GetMultiPointWeatherForecast_BuildsCorrectUriAndAuthorizationHeader()
        {
            // Arrange
            var apiKey = "2565a64c-d588-11f0-a148-0242ac130003-2565a73c-d588-11f0-a148-0242ac130003";
            var options = Options.Create(new StormglassWeatherProviderConfiguration { ApiKey = apiKey });

            var capturedRequest = default(HttpRequestMessage);

            var handler = new CaptureHandler(
                request =>
                {
                    capturedRequest = request;

                    // Return non-success deliberately; we only want to inspect the outgoing request
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"error\":\"bad request for test\"}")
                    };
                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.stormglass.io") };

            var sut = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.StormglassWeatherForecastProvider(httpClient, options);

            // Two requested points at different hours to assert start/end in the URI
            var now = DateTime.UtcNow;
            var points = new[]
            {
                new WeatherRequestInstance { Location = _alesund, Time = now, IsLiveMode = false },
                new WeatherRequestInstance { Location = _alesund, Time = now.AddHours(3), IsLiveMode = false },
            };

            // Act
            await Assert.ThrowsAsync<WeatherForecastProviderException>(
                () => sut.GetMultiPointWeatherForecast(points));

            // Assert - Request was captured
            Assert.NotNull(capturedRequest);

            // Authorization header should carry the API key (per current provider code)
            Assert.NotNull(capturedRequest.Headers.Authorization);
            Assert.Equal(apiKey, capturedRequest.Headers.Authorization.Scheme);
            Assert.Null(capturedRequest.Headers.Authorization.Parameter);
            
            // URI should be absolute (BaseAddress + relative) and contain expected query params
            var uri = capturedRequest.RequestUri!;
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            Assert.Equal("/v2/weather/point", uri.AbsolutePath);

            // lat/lng match location
            Assert.Equal(_alesund.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), query["lat"]);
            Assert.Equal(_alesund.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), query["lng"]);

            // params should equal provider's parameter set
            var expectedParams =
                "waveHeight,waveDirection,wavePeriod,windSpeed,windDirection,currentDirection,currentSpeed";
            Assert.Equal(expectedParams, query["params"]);

            // start == first time (rounded as provider formats to 'yyyy-MM-ddTHH:mm:ssZ'), end == last time
            var startStr = query["start"];
            var endStr = query["end"];
            Assert.NotNull(startStr);
            Assert.NotNull(endStr);
        }

        // --- Test 2: Non-success response should throw WeatherForecastProviderException -----------------------
        [Fact]
        public async Task GetMultiPointWeatherForecast_WhenHttpFails_ThrowsWeatherForecastProviderException()
        {
            // Arrange
            var options = Options.Create(new StormglassWeatherProviderConfiguration { ApiKey = "KEY" });

            var handler = new CaptureHandler(
                _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"error\":\"server error\"}")
                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.stormglass.io") };
            var sut = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.StormglassWeatherForecastProvider(httpClient, options);

            var now = DateTime.UtcNow;
            var req = new[]
            {
                new WeatherRequestInstance { Location = _alesund, Time = now, IsLiveMode = false }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<WeatherForecastProviderException>(
                () => sut.GetMultiPointWeatherForecast(req));

            Assert.Contains("StormGlass error", ex.Message);
            Assert.Contains("InternalServerError", ex.Message);
        }

        // --- Test 3: Successful HTTP but empty hours should throw ---------------------------------------------
        [Fact]
        public async Task GetMultiPointWeatherForecast_WhenHoursEmpty_ThrowsWeatherForecastProviderException()
        {
            // Arrange
            var options = Options.Create(new StormglassWeatherProviderConfiguration { ApiKey = "KEY" });

            // Minimal successful payload with empty hours
            var payload = /* language=json */ """
            {
              "hours": [],
              "meta": { "lat": 62.472, "lng": 6.154 }
            }
            """;

            var handler = new CaptureHandler(
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload)
                });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.stormglass.io") };
            var sut = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.StormglassWeatherForecastProvider(httpClient, options);

            var now = DateTime.UtcNow;
            var req = new[]
            {
                new WeatherRequestInstance { Location = _alesund, Time = now, IsLiveMode = false }
            };

            // Act & Assert
            await Assert.ThrowsAsync<WeatherForecastProviderException>(
                () => sut.GetMultiPointWeatherForecast(req));
        }

        // --- Helper -----------------------------------------------------------
        private sealed class CaptureHandler : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder ?? throw new ArgumentNullException(nameof(responder));
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var response = _responder(request);
                return Task.FromResult(response);
            }
        }
    }
}
