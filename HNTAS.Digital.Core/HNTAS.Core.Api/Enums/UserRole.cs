using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>
        /// Represents a regulatory contact role.
        /// </summary>
        [Description("Regulatory Contact")]
        RegulatoryContact = 1,

        /// <summary>
        /// Represents a third-party user role.
        /// </summary>
        [Description("Third Party")]
        ThirdParty = 2,

        /// <summary>
        /// Represents a contributor user role.
        /// </summary>
        [Description("Contributor")]
        Contributor = 3,
    }
}
