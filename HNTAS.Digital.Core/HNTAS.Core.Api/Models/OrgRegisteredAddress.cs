using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Models
{
    public class OrgRegisteredAddress
    {
        [Required(ErrorMessage = "Address Line 1 is required.")]
        [BsonElement("address_line1")]
        public string AddressLine1 { get; set; } = null!; 

        public string? AddressLine2 { get; set; }

        public string? Town { get; set; }

        public string? County { get; set; }

        [Required(ErrorMessage = "Postcode is required.")]
        [BsonElement("postcode")]
        public string Postcode { get; set; } = null!; 

        public string? Country { get; set; }
    }
}
