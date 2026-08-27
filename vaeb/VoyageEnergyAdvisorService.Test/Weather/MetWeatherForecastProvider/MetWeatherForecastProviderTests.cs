//using VoyageEnergyAdvisor.Core.CommonModels;

//namespace VoyageEnergyAdvisorService.Test.Weather.MetWeatherForecastProvider
//{
//    using System.Globalization;
//    using System.Net;
//    using Xunit;
//    using Microsoft.Extensions.Options;
//    using VoyageEnergyAdvisor.Core.Services.WeatherProvider.Models;
//    using Moq;

//    public class MetWeatherForecastProviderTests
//    {
//        private readonly HttpClient _httpClient = new(new MyHttpMessageHandlerMock());
//        private readonly IOptionsMonitor<MetWeatherForecastProviderConfiguration> _optionsMonitor;
//        //private readonly HttpClient _httpClient = new(); // Test code. Uncomment to use actual http client.
//        private static readonly List<string> PrevLocationRequestUris = new();
//        private static readonly List<string> PrevOceanRequestUris = new();

//        public MetWeatherForecastProviderTests()
//        {
//            CultureInfo.CurrentCulture = new CultureInfo("en-US");

//            var config = new MetWeatherForecastProviderConfiguration
//            {
//                Radius = 1.0, // Default radius for all tests
//                ExpirationPeriod = TimeSpan.FromHours(1) // Default expiration period for all tests
//            };

//            // Mock the IOptionsMonitor
//            var optionsMock = new Mock<IOptionsMonitor<MetWeatherForecastProviderConfiguration>>();
//            optionsMock.Setup(o => o.CurrentValue).Returns(config);

//            _optionsMonitor = optionsMock.Object;
//        }

//        private class MyHttpMessageHandlerMock : DelegatingHandler // Cumbersome solution as GetAsync is not directly overridable 
//        {
//            private readonly List<string> _locationUris;
//            private readonly List<string> _oceanUris;

//            public MyHttpMessageHandlerMock()
//            {
//                //_locationUris = locationUris;
//                //_oceanUris = oceanUris;
//            }

//            protected override Task<HttpResponseMessage> SendAsync(
//             HttpRequestMessage request,
//             CancellationToken cancellationToken)
//            {
//                if (request.RequestUri!.ToString().Contains("locationforecast"))
//                {
//                    _locationUris.Add(request.RequestUri.ToString());
//                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
//                    {
//                        Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetWeatherForecastResponse.json"))
//                    });
//                }

//                if (request.RequestUri.ToString().Contains("oceanforecast"))
//                {
//                    _oceanUris.Add(request.RequestUri.ToString());
//                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
//                    {
//                        Content = new StringContent(File.ReadAllText("../../../Weather/MetWeatherForecastProvider/MetOceanForecastResponse.json"))
//                    });
//                }

//                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
//            }
//        }

//        [Fact]
//        public async Task TestGetSingleWeatherForecast()
//        {
//            var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.MetWeatherForecastProvider(_httpClient, _optionsMonitor);

//            //var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.MetWeatherForecastProvider(httpClient);

//            foreach (var testPoint in TestPoints)
//            {
//                PrevLocationRequestUris.Clear();
//                PrevOceanRequestUris.Clear();

//                var input = testPoint.input;
//                var expectedOutput = testPoint.expectedOutput;
//                var forcasts = await metWeatherForecastProvider.GetMultiPointWeatherForecast(
//                    new List<WeatherRequestInstance>(){new WeatherRequestInstance() {Time = input.Time, Location = input.Location } });

//                var res = forcasts.First();

//                // Check URIs
//                Assert.Equal($"https://api.met.no/weatherapi/locationforecast/2.0/complete?lat={input.Location.Latitude}&lon={input.Location.Longitude}", PrevLocationRequestUris.First());
//                Assert.Equal($"https://api.met.no/weatherapi/oceanforecast/2.0/complete?lat={input.Location.Latitude}&lon={input.Location.Longitude}", PrevOceanRequestUris.First());

//                // Meta data
//                Assert.Equal(input.Time, res!.Time);
//                Assert.Equal(expectedOutput.Time, res.Time);
//                Assert.Equal(expectedOutput.Location.Latitude, res.Location.Latitude);
//                Assert.Equal(expectedOutput.Location.Longitude, res.Location.Longitude);

//                // Weather data
//                Assert.Equal(expectedOutput.Weather.WindDirection!.Value, res.Weather.WindDirection!.Value);
//                Assert.Equal(expectedOutput.Weather.WindSpeed!.Value, res.Weather.WindSpeed!.Value);
//                Assert.Equal(expectedOutput.Weather.CurrentDirection!.Value, res.Weather.CurrentDirection!.Value);
//                Assert.Equal(expectedOutput.Weather.CurrentSpeed!.Value, res.Weather.CurrentSpeed!.Value);
//                Assert.Equal(expectedOutput.Weather.WavePeakPeriod!.Value, res.Weather.WavePeakPeriod!.Value);
//                Assert.Equal(expectedOutput.Weather.WaveHeight!.Value, res.Weather.WaveHeight!.Value);
//                Assert.Equal(expectedOutput.Weather.WaveDirection!.Value, res.Weather.WaveDirection!.Value);
//            }
//        }

//        [Fact]
//        public async Task TestGetMultiPointWeatherForecast()
//        {
//            var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.MetWeatherForecastProvider(_httpClient, _optionsMonitor);
//            var GeoCoordinates = TestPoints.Select(e => new WeatherRequestInstance()
//                {Time = e.input.Time, Location = e.input.Location}).ToList();
//            var res = await metWeatherForecastProvider.GetMultiPointWeatherForecast(GeoCoordinates);

//            Assert.Equal(TestPoints.Count, res.Count);
//            foreach (var testPoint in TestPoints)
//            {
//                Assert.Contains(res, e => e.Location == testPoint.input.Location);
//                Assert.Contains(PrevLocationRequestUris, e => e == $"https://api.met.no/weatherapi/locationforecast/2.0/complete?lat={testPoint.input.Location.Latitude}&lon={testPoint.input.Location.Longitude}");
//                Assert.Contains(PrevOceanRequestUris, e => e == $"https://api.met.no/weatherapi/oceanforecast/2.0/complete?lat={testPoint.input.Location.Latitude}&lon={testPoint.input.Location.Longitude}");
//            }
//        }

//        //[Fact]
//        //public async Task TestFailOnTimestampTooEarly()
//        //{
//        //    var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.MetWeatherForecastProvider(_httpClient, null);

//        //    async Task ActionUnderTest()
//        //    {
//        //        var GeoCoordinates = TestPoints.Select(e => new WeatherRequestInstance()
//        //        {
//        //            Time = DateTimeOffset.Parse("10/31/2023 07:00:00 +00:00").DateTime,
//        //            GeoCoordinate = e.input.GeoCoordinate
//        //        }).ToList();
        
//        //        // This line should throw an exception
//        //        await metWeatherForecastProvider.GetMultiPointWeatherForecast(GeoCoordinates);
//        //    }

//        //    var exception = await Assert.ThrowsAsync<WeatherForecastProviderException>(ActionUnderTest);
//        //    Assert.Equal("Failed to get weather forecast. Weather forecast can only be provided for the period 10/31/2023 9:00:00 AM to 11/10/2023 6:00:00 AM.", exception.Message);
//        //}



//        [Fact]
//        public async Task TestFailOnTimestampTooLate()
//        {
//            var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProviders.MetWeatherForecastProvider(_httpClient, null);

//            //[Fact]
//            //public async Task TestFailOnTimestampTooLate()
//            //{
//            //    var metWeatherForecastProvider = new VoyageEnergyAdvisor.Core.Services.WeatherProvider.MetWeatherForecastProvider(_httpClient, null);

//            //    async Task ActionUnderTest()
//            //    {
//            //        var GeoCoordinates = TestPoints.Select(e => new TimestampedGeoCoordinate()
//            //        {
//            //            Timestamp = DateTimeOffset.Parse("11/10/2023 08:00:00 +00:00").DateTime,
//            //            Location = e.input.Location
//            //        }).ToList();
//            async Task ActionUnderTest()
//            {
//                var GeoCoordinates = TestPoints.Select(e => new WeatherRequestInstance()
//                {
//                    Time = DateTimeOffset.Parse("11/10/2023 08:00:00 +00:00").DateTime,
//                    Location = e.input.Location
//                }).ToList();

//                // This line should throw an exception
//                await metWeatherForecastProvider.GetMultiPointWeatherForecast(GeoCoordinates);
//            }
//        }

//        //        // This line should throw an exception
//        //        await metWeatherForecastProvider.GetMultiPointWeatherForecast(GeoCoordinates);
//        //    }

//        //    var exception = await Assert.ThrowsAsync<WeatherForecastProviderException>(ActionUnderTest);
//        //    Assert.Equal("Failed to get weather forecast. Weather forecast can only be provided for the period 10/31/2023 9:00:00 AM to 11/10/2023 6:00:00 AM.", exception.Message);
//        //}

//        private static readonly List<(WeatherRequestInstance input, WeatherResponseInstance expectedOutput)> TestPoints = new()
//        {
//            (new WeatherRequestInstance()
//                {
//                    Location = new GeoCoordinate {Latitude = 60, Longitude = 5},
//                    Time = DateTimeOffset.Parse("2023-11-01T04:20:02Z").DateTime
//                },
//                new WeatherResponseInstance
//                {
//                    Location = new GeoCoordinate {Latitude = 60, Longitude = 5},
//                    Time = DateTimeOffset.Parse("2023-11-01T04:20:02Z").DateTime,
//                    Weather = new WeatherData()
//                    {
//                        WindDirection = 142.3 - 180,
//                        WindSpeed = 8.9,
//                        CurrentSpeed = 0.2,
//                        CurrentDirection = 339.2,
//                        WaveDirection = 184.1 - 180,
//                        WaveHeight = 0.9,
//                        WavePeakPeriod = 0.000001,
//                    }
//                }
//            ),
//            (
//                new WeatherRequestInstance
//                {
//                    Location = new GeoCoordinate {Latitude = 60, Longitude = 5},
//                    Time = DateTimeOffset.Parse("2023-11-01T03:31:02Z").DateTime
//                },
//                new WeatherResponseInstance
//                {
//                    //UpdatedAt = DateTimeOffset.Parse("2023-10-31T08:39:13Z").DateTime,
//                    Location = new GeoCoordinate {Latitude = 60, Longitude = 5},
//                    Time = DateTimeOffset.Parse("2023-11-01T03:31:02Z").DateTime,
//                    Weather = new WeatherData()
//                    {
//                        WindDirection = 142.3 - 180,
//                        WindSpeed = 8.9,
//                        CurrentSpeed = 0.2,
//                        CurrentDirection = 339.2,
//                        WaveDirection = 184.1 - 180,
//                        WaveHeight = 0.9,
//                        WavePeakPeriod = 0.000001,
//                    }
//                }
//            ),
//            (
//                new WeatherRequestInstance
//                {
//                    Location = new GeoCoordinate {Latitude = 65, Longitude = 6},
//                    Time = DateTimeOffset.Parse("2023-11-07T08:05:00Z").DateTime
//                },
//                new WeatherResponseInstance
//                {
//                    //UpdatedAt = DateTimeOffset.Parse("2023-10-31T08:39:13Z").DateTime,
//                    Location = new GeoCoordinate {Latitude = 65, Longitude = 6},
//                    Time = DateTimeOffset.Parse("2023-11-07T08:05:00Z").DateTime,
//                    Weather = new WeatherData()
//                    {
//                        WindDirection = 150.2 - 180,
//                        WindSpeed = 4.8,
//                        CurrentSpeed = 0,
//                        CurrentDirection = 315.2,
//                        WaveDirection = 316.7 - 180,
//                        WaveHeight = 0.8,
//                        WavePeakPeriod = 0.000001,
//                    }
//                }
//            )
//        };

//    }
//}