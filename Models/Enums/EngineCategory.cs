using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EngineCategory
{
    Main,
    Auxiliary
}
