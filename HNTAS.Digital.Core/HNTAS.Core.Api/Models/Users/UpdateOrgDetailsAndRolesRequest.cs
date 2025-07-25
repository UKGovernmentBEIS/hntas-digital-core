using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Users
{
    public class UpdateOrgDetailsAndRolesRequest
    {
        [Required(ErrorMessage = "Organization details are required.")]
        public OrgDetails OrgDetails { get; set; } = null!;

        [Required(ErrorMessage = "User role is required.")]
        public UserRole Role { get; set; }
    }
}
