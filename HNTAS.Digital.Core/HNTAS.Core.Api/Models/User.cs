using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HNTAS.Core.Api.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("onelogin_id")]
        public string OneLoginId { get; set; }

        [BsonElement("org_details")]
        public OrgDetails? OrgDetails { get; set; }

        [BsonElement("email_id")]
        public string EmailId { get; set; }

        [BsonElement("hn_ids")]
        public List<string>? HnIds { get; set; }

        [BsonElement("roles")]
        [BsonRepresentation(BsonType.String)] // Store enum names as strings in DB
        public List<UserRole>? Roles { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)] // Store enum name as string in DB
        public UserStatus? Status { get; set; }

        [BsonElement("invitations")]
        public List<Invitation>? Invitations { get; set; }
    }
}
