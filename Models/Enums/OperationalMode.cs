using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationalMode
{
    Transit,
    DP,
    Port,
    Anchor,
    Maneuvering
}
