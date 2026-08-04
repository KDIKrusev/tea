namespace KSailCalc.Api.Models.Domain;

/// <summary>
/// Battery-driven adjustment to a mode's Level 1 loads: the spinning reserve the battery could
/// NOT cover, split by which side of the plant carries it. Passing a non-null adjustment also
/// switches the default baseline to the "third highest" rule (decision D1).
/// PropulsionPeakShavingKw is the battery's covered ± band on thrust loads — with PTI configured
/// it must fit through the remaining PTI capacity (Excel "Insufficient PTI" gate, Increment C).
/// </summary>
public record BatteryL1Adjustment(
    double PropulsionReserveKw, double HotelReserveKw, double PropulsionPeakShavingKw = 0);
