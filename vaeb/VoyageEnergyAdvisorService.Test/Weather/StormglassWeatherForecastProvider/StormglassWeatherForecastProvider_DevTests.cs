
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VoyageEnergyAdvisor.Core.CommonModels;
using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders;
using VoyageEnergyAdvisor.Core.Services.WeatherProviders.WeatherProviderModels;
using Xunit;

namespace VoyageEnergyAdvisor.Core.Tests.WeatherProviders
{
    public class StormglassWeatherForecastProvider_DevTests
    {
        /// <summary>
        /// DEV-ONLY test that calls the real Stormglass API so you can inspect responses in the debugger.
        /// Requests two different times per location (Ålesund and Bergen).
        /// </summary>
        [Fact]
        public async Task Dev_CallsStormglass_TwoLocationsTwoTimes_InspectInDebugger()
        {
            // --- Configuration with the API key you provided ---
            var apiKey = "2565a64c-d588-11f0-a148-0242ac130003-2565a73c-d588-11f0-a148-0242ac130003";

            var options = Options.Create(new StormglassWeatherProviderConfiguration
            {
                ApiKey = apiKey
            });

            // Real HttpClient hitting the public endpoint
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.stormglass.io")
            };

            var provider = new StormglassWeatherForecastProvider(httpClient, options);

            // Locations: Ålesund & Bergen
            var alesund = new GeoCoordinate(62.472, 6.154);
            var bergen  = new GeoCoordinate(60.391, 5.322);

            // Build a small request window: now and +3h for each location
            var now = DateTime.UtcNow;

            var requests = new List<WeatherRequestInstance>
            {
                // Ålesund - t0 and t0+3h
                new WeatherRequestInstance { Location = alesund, Time = now,            IsLiveMode = false },
                new WeatherRequestInstance { Location = alesund, Time = now.AddHours(3), IsLiveMode = false },

                // Bergen - t0+1h and t0+4h (intentionally different from Ålesund to vary times)
                new WeatherRequestInstance { Location = bergen,  Time = now.AddHours(1), IsLiveMode = false },
                new WeatherRequestInstance { Location = bergen,  Time = now.AddHours(4), IsLiveMode = false },
            };

            // --- Call and break for inspection ---
            var response = await provider.GetMultiPointWeatherForecast(requests);

            // Put a breakpoint here and inspect `response` in the debugger:
            // - response.Count (should typically equal number of requested points if mapping finds closest hours)
            // - For each item: Location, Time, Weather (WaveHeight, WindSpeed, etc.)
            // - StartTime, EndTime, ExpirationDateTime (based on provider’s _updatePeriod/_expirationPeriod)
            GC.KeepAlive(response);
        }
    }
}
