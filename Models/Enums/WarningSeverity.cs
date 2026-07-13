using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace KSailCalc.Api.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WarningSeverity
{
    [EnumMember(Value = "warning")]
    Warning,

    [EnumMember(Value = "error")]
    Error
}
