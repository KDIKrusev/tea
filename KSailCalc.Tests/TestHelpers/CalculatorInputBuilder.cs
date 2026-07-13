using KSailCalc.Api.Models;

namespace KSailCalc.Tests.TestHelpers;

public class CalculatorInputBuilder
{
    private readonly CalculatorInput _input;

    private CalculatorInputBuilder()
    {
        _input = new CalculatorInput
        {
            MeCapacityPerEngine = 5000,
            MeCount = 2,
            SgCapacityPerEngine = 1000,
            AeCapacityPerEngine = 800,
            AeCount = 3,
            MainEngineTypeId = 1,
            AuxEngineTypeId = 1,
            PropulsionPower = 4000,
            SeaMargin = 15,
            VesselSpeedKnots = 14,
            TransitHours = 5694,
            TransitHotelPowerKW = 800,
            DPHours = null,
            DPHotelPowerKW = null,
            RequiredDPPowerKW = null,
            SailInstalled = false,
            SailEnabled = false,
            BatteryCapacity = 0,
            FuelPrice = 600,
            TrueWindSpeed = null,
            WindAngleRelVessel = null
        };
    }

    public static CalculatorInputBuilder Default() => new();
    public CalculatorInput Build() => _input;

    public CalculatorInputBuilder WithMainEngines(double capacity, int count) { _input.MeCapacityPerEngine = capacity; _input.MeCount = count; return this; }
    public CalculatorInputBuilder WithAuxiliaryEngines(double capacity, int count) { _input.AeCapacityPerEngine = capacity; _input.AeCount = count; return this; }
    public CalculatorInputBuilder WithShaftGenerators(double capacity) { _input.SgCapacityPerEngine = capacity; return this; }
    public CalculatorInputBuilder WithPropulsionPower(double power) { _input.PropulsionPower = power; return this; }
    public CalculatorInputBuilder WithSeaMargin(double margin) { _input.SeaMargin = margin; return this; }
    public CalculatorInputBuilder WithFuelPrice(double price) { _input.FuelPrice = price; return this; }

    public CalculatorInputBuilder WithTransitMode(double hours, double hotel) { _input.TransitHours = hours; _input.TransitHotelPowerKW = hotel; return this; }

    public CalculatorInputBuilder WithDPMode(double hours, double hotel, double thrust)
    {
        _input.DpEnabled = true; _input.DPHours = hours; _input.DPHotelPowerKW = hotel; _input.RequiredDPPowerKW = thrust; return this;
    }
    public CalculatorInputBuilder WithoutDPMode() { _input.DpEnabled = false; _input.DPHours = null; _input.DPHotelPowerKW = null; _input.RequiredDPPowerKW = null; return this; }

    public CalculatorInputBuilder WithSailEnabled(double trueWindSpeed, double windAngle)
    {
        _input.SailInstalled = true; _input.SailEnabled = true;
        _input.TrueWindSpeed = trueWindSpeed; _input.WindAngleRelVessel = windAngle;
        return this;
    }
    public CalculatorInputBuilder WithSailDisabled() { _input.SailEnabled = false; _input.TrueWindSpeed = null; _input.WindAngleRelVessel = null; return this; }
    public CalculatorInputBuilder WithVesselTypeName(string name) { _input.VesselTypeName = name; return this; }
}