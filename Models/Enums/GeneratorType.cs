using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GeneratorType
{
    SG,
    AE
}

public static class GeneratorTypeExtensions
{
    public static EngineCategory ToEngineCategory(this GeneratorType type) => type switch
    {
        GeneratorType.SG => EngineCategory.Main,
        GeneratorType.AE => EngineCategory.Auxiliary,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static int GetEngineTypeId(this GeneratorType type, CalculatorInput input) => type switch
    {
        GeneratorType.SG => input.MainEngineTypeId,
        GeneratorType.AE => input.AuxEngineTypeId,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
