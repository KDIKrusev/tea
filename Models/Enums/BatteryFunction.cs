using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models.Enums;

/// <summary>
/// How the battery supports a load demand (Excel "Load Demands" sheet, column C).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BatteryFunction
{
    /// <summary>Battery held ready to cover the full requirement (100% coverage), e.g. DP class redundancy.</summary>
    Reserve,

    /// <summary>Battery absorbs/supplies part of the load variation band around the average.</summary>
    PeakShaving
}

/// <summary>
/// Load demand types the battery budget is allocated over, in the Excel priority order.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BatteryLoadType
{
    // NOTE: when adding a load type, extend BatteryLoadTypeExtensions.IsThrustSide below —
    // it decides which plant side (propulsion vs hotel) carries the load's reserve/band.
    /// <summary>DP class redundancy requirement (RESERVE row).</summary>
    DpReserve,

    /// <summary>DP thruster load demand variation.</summary>
    DpDemand,

    /// <summary>Mission heavy-consumer load (e.g. crane operation).</summary>
    Mission,

    /// <summary>Propeller/propulsion load variation (environmental).</summary>
    Propulsion,

    /// <summary>Hotel load variation (consumer start/stop).</summary>
    Hotel
}

/// <summary>
/// Single source of truth for which plant side a battery load belongs to:
/// thrust-side loads route reserve to the propulsion demand and their covered band through PTI;
/// the rest are electrical (hotel side) and offset the L3 DRC variation.
/// </summary>
public static class BatteryLoadTypeExtensions
{
    public static bool IsThrustSide(this BatteryLoadType load)
        => load is BatteryLoadType.Propulsion or BatteryLoadType.DpReserve or BatteryLoadType.DpDemand;
}
