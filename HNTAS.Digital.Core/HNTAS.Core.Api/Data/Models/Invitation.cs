using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Data.Models
{
    public class Invitation
    {
        [Required(ErrorMessage = "Invitation Id is required.")]
        [BsonElement("id")]
        public string Id { get; set; } = null!;

        [Required(ErrorMessage = "Invitation First Name is required.")]
        [BsonElement("first_name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Invitation Last Name is required.")]
        [BsonElement("last_name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Permissions list is required.")]
        [BsonElement("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        [Required(ErrorMessage = "Invited Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Invited Email format.")]
        [BsonElement("invited_email")]
        public string InvitedEmail { get; set; } = null!;

        [Required(ErrorMessage = "Invited At date/time is required.")]
        [BsonElement("invited_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime InvitedAt { get; set; }

        [Required(ErrorMessage = "Invitation Status is required.")]
        [BsonElement("status")]
        public string Status { get; set; } = null!;
    }
}
