namespace VoyageEnergyAdvisor.Core.Services.AisProviders.AisProviderModels
{
    using System.Text.Json;

    public class AisStreamMessageContent
    {
        public AisStreamPositionReport? PositionReport { get; set; }
        public AisStreamShipStaticData? ShipStaticData { get; set; }
    }

    public class AisStreamMessage
    {
        public string MessageType { get; set; } = string.Empty;
        public AisStreamMetaData? MetaData { get; set; }
        public JsonElement Message { get; set; }
    }

    public class AisStreamMetaData
    {
        public long MMSI { get; set; }

        public JsonElement MMSI_String { get; set; }

        public string ShipName { get; set; } = string.Empty;
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string time_utc { get; set; } = string.Empty;
    }

    public class AisStreamPositionReport
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? RateOfTurn { get; set; }
        public double Sog { get; set; }
        public bool PositionAccuracy { get; set; }
        public double Cog { get; set; }
        public double? TrueHeading { get; set; }
        public int Timestamp { get; set; }
        public int NavigationalStatus { get; set; }
        public long UserID { get; set; }
        public bool Valid { get; set; }
    }

    public class AisStreamShipStaticData
    {
        public int AisVersion { get; set; }
        public string CallSign { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public AisStreamDimension? Dimension { get; set; }
        public bool Dte { get; set; }
        public AisStreamEta? Eta { get; set; }
        public int FixType { get; set; }
        public long ImoNumber { get; set; }
        public double MaximumStaticDraught { get; set; }
        public int MessageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RepeatIndicator { get; set; }
        public bool Spare { get; set; }
        public int Type { get; set; }
        public long UserID { get; set; }
        public bool Valid { get; set; }
    }

    public class AisStreamDimension
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
    }

    public class AisStreamEta
    {
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Month { get; set; }
    }

}
