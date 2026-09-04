using System.Diagnostics.CodeAnalysis;

namespace HNTAS.Core.Api.Models.Users
{
    [ExcludeFromCodeCoverage]
    public class UserNetworkDetailsResponse
    {
        public string HnId { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? AdditionalDescription { get; set; }

        public string OrganisationName { get; set; } = null!;

        public string OrgId { get; set; } = null!;
    }
}
