using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Helpers
{
    public class UserNetworkHelper
    {
        private static readonly HashSet<UserRole> RolesGrantingFullAccess = new()
        {
            UserRole.ResponsibleParty,
            UserRole.NetworkManager
        };

        public static List<HeatNetworkUserResponse> GetAuthorizedNetworks(UserDetailsResult userDetails)
        {
            if (userDetails == null) return new List<HeatNetworkUserResponse>();

            // 1. Specific Mappings (Highest Priority)
            // If they are specifically assigned to networks, return those.
            if (userDetails.HnRoleMappings?.Count > 0)
            {
                return userDetails.HnRoleMappings
                    .Select(m => m.HeatNetwork)
                    .Where(hn => hn != null)
                    .ToList();
            }

            // 2. Full Access Check (RP or Network Manager)
            bool hasFullAccess = userDetails.Roles?.Any(r => RolesGrantingFullAccess.Contains(r)) ?? false;

            if (hasFullAccess && userDetails.Organisation?.HeatNetworks != null)
            {
                return userDetails.Organisation.HeatNetworks.ToList();
            }

            // 3. Fallback: No access or no networks found
            return new List<HeatNetworkUserResponse>();
        }
    }
}
