using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContributorRole
    {
        [Description("Designated duty holder")]
        DesignatedDutyHolder = 1,
        [Description("Contributor")]
        Contributor = 2,        
        [Description("Assessor")]
        Assessor = 3,
        [Description("Certifier")]
        Certifier = 4,
        [Description("Network Manager")]
        NetworkManager = 5,
        [Description("Responsible Party")]
        ResponsibleParty = 6
    }
}