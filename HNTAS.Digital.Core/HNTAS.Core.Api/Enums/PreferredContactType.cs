using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PreferredContactType
    {
        [Description("Landline")]
        Landline = 1,
        [Description("Mobile")]
        Mobile = 2
    }
}
