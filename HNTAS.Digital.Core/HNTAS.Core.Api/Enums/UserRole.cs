using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>
        /// Represents a responsible party role.
        /// </summary>
        [Description("Responsible Party")]
        ResponsibleParty = 1,

        /// <summary>
        /// Represents a Network Manager role.
        /// </summary>
        [Description("Network Manager")]
        NetworkManager = 2,

        /// <summary>
        /// Represents a Designated Duty Holder user role.
        /// </summary>
        [Description("Designated Duty Holder")]
        DesignatedDutyHolder = 3,

        /// <summary>
        /// Represents a contributor user role.
        /// </summary>
        [Description("Contributor")]
        Contributor = 4,

        /// <summary>
        /// Represents a assessor user role.
        /// </summary>
        [Description("Assessor")]
        Assessor = 5,

        /// <summary>
        /// Represents a certifier user role.
        /// </summary>
        [Description("Certifier")]
        Certifier = 6
    }
}
