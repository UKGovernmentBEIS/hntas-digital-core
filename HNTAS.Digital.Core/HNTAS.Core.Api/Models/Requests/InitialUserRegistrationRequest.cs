using HNTAS.Core.Api.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HNTAS.Core.Api.Models.Requests
{
    public class InitialUserRegistrationRequest
    {
        [Required(ErrorMessage = "OneLogin Id is required.")]
        public string OneLoginId { get; set; } = null!;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string EmailId { get; set; } = null!;

        /// <summary>
        /// The initial status of the user account.
        /// </summary>
        [Required(ErrorMessage = "User status is required.")]
        public UserStatus Status { get; set; }
    }
}
