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
}
