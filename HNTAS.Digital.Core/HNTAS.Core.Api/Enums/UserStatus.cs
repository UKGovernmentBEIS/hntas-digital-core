using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserStatus
    {
        Active = 1,
        InActive = 2,
        InvitationSent = 3,
        InvitationAccepted = 4,
        InvitationDeclined = 5
    }
}
