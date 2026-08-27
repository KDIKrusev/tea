namespace VoyageEnergyAdvisor.Core.Services.AisProviders
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels;
    using VoyageEnergyAdvisor.Core.Services.CacheService;

    public class AisStreamBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AisStreamBackgroundService> _logger;
        private readonly ICacheService _cacheService;
        private readonly AisStreamProviderConfiguration _config;

        private ClientWebSocket? _webSocket;
        private int _reconnectAttempts = 0;

        public AisStreamBackgroundService(
            IServiceProvider serviceProvider,
            ICacheService cacheService,
            IOptions<AisStreamProviderConfiguration> configOptions,
            ILogger<AisStreamBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cacheService = cacheService;
            _config = configOptions.Value ?? throw new Exception("AisStreamProviderConfiguration not found in appsettings.json");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndListen(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket connection failed");
                    await HandleReconnection(stoppingToken);
                }
            }

            _logger.LogInformation("AISStream Background Service stopped");
        }

        public AisResponseInstance? GetCachedVesselData(string mmsi)
        {
            var cacheKey = _cacheService.GenerateCacheKey("ais_vessel", mmsi);

            if (_cacheService.TryGetCachedItem<AisResponseInstance>(cacheKey, out var data))
            {
                return data;
            }
            return null;
        }

        private async Task ConnectAndListen(CancellationToken stoppingToken)
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            var url = new Uri($"wss://stream.aisstream.io/v0/stream?token={_config.ApiKey}");
            _logger.LogInformation("Connecting to AISStream...");
            await _webSocket.ConnectAsync(url, stoppingToken);
            _reconnectAttempts = 0;
            Console.WriteLine("✅ Connected to AISStream");
            await SendSubscription(stoppingToken);
            Console.WriteLine("📡 Subscription sent!");
            Console.WriteLine("⏳ Waiting for raw messages... (press Ctrl+C to exit)\n");
            var buffer = new byte[8192];
            while (_webSocket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
              
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by server");
                    break;
                }
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessMessage(message);
            }

            // Check why we exited the while loop
            if (_webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning($"Connection lost - WebSocket state is now: {_webSocket.State}");
                Console.WriteLine($"❌ Connection closed - State: {_webSocket.State}");
            }

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Exited due to cancellation request");
                Console.WriteLine("🛑 Service shutdown requested");
            }
        }

        private async Task SendSubscription(CancellationToken stoppingToken)
        {
            var subscription = new
            {
                APIKey = _config.ApiKey,
                BoundingBoxes = new[] { _config.GlobalBoundingBox },
                FiltersShipMMSI = _config.FilterShipMMSI,
                FilterMessageTypes = _config.FilterMessageTypes
            };

            var json = JsonSerializer.Serialize(subscription);

            await _webSocket!.SendAsync(
                Encoding.UTF8.GetBytes(json),
                WebSocketMessageType.Text,
                true,
                stoppingToken);

            _logger.LogInformation($"Subscription sent for vessels: {string.Join(", ", _config.FilterShipMMSI)}");
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var aisMessage = JsonSerializer.Deserialize<AisStreamMessage>(message, options);

                if (aisMessage?.MetaData == null) return;

                var mmsi = aisMessage.MetaData.MMSI.ToString();
                var vesselName = aisMessage.MetaData.ShipName?.Trim();

                var messageContent = JsonSerializer.Deserialize<AisStreamMessageContent>(
                    aisMessage.Message.GetRawText(), options);

                if (aisMessage.MessageType == "PositionReport" && messageContent?.PositionReport != null)
                {
                    var positionReport = messageContent.PositionReport;

                    if (positionReport.Valid)
                    {
                        var aisResponse = new AisResponseInstance
                        {
                            VesselId = 0,
                            MMSI = aisMessage.MetaData.MMSI,
                            VesselName = vesselName ?? "Unknown",
                            Latitude = positionReport.Latitude,
                            Longitude = positionReport.Longitude,
                            Speed = positionReport.Sog,
                            Course = positionReport.Cog,
                            Heading = positionReport.TrueHeading,
                            Status = GetNavigationalStatusText(positionReport.NavigationalStatus),
                            PositionUpdatedAt = ParseUtcTime(aisMessage.MetaData.time_utc)
                        };

                        var cacheKey = _cacheService.GenerateCacheKey("ais_vessel", mmsi);
                        _cacheService.CacheItem(cacheKey, aisResponse, TimeSpan.FromHours(24), TimeSpan.FromMinutes(180));

                        _logger.LogDebug($"Updated position for {vesselName} (MMSI: {mmsi})");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process AISStream message");
            }
        }

        private DateTime ParseUtcTime(string timeUtc)
        {
            if (DateTime.TryParse(timeUtc, out var parsedTime))
            {
                return parsedTime.ToUniversalTime();
            }
            return DateTime.UtcNow;
        }

        private string GetNavigationalStatusText(int status)
        {
            return status switch
            {
                0 => "Under way using engine",
                1 => "At anchor",
                2 => "Not under command",
                3 => "Restricted maneuverability",
                4 => "Constrained by her draught",
                5 => "Moored",
                6 => "Aground",
                7 => "Engaged in fishing",
                8 => "Under way sailing",
                9 => "Reserved for future amendment of Navigational Status for HSC",
                10 => "Reserved for future amendment of Navigational Status for WIG",
                11 => "Power-driven vessel towing astern (regional use)",
                12 => "Power-driven vessel pushing ahead or towing alongside (regional use)",
                13 => "Reserved for future use",
                14 => "AIS-SART (Search and Rescue Transmitter)",
                15 => "Undefined (default)",
                _ => "Unknown"
            };
        }

        private async Task HandleReconnection(CancellationToken stoppingToken)
        {
            _reconnectAttempts++;

            if (_reconnectAttempts >= _config.MaxReconnectAttempts)
            {
                _logger.LogError($"Max reconnection attempts ({_config.MaxReconnectAttempts}) reached. Giving up.");
                return;
            }

            _logger.LogInformation($"Reconnecting in {_config.ReconnectDelayMs}ms " +
                                 $"(attempt {_reconnectAttempts}/{_config.MaxReconnectAttempts})");

            try
            {
                await Task.Delay(_config.ReconnectDelayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

    }
}
