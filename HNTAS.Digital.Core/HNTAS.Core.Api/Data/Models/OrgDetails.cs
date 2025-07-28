using HNTAS.Core.Api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace HNTAS.Core.Api.Data.Models
{
    public class OrgDetails
    {
        [BsonElement("org_id")]
        public string? OrgId { get; set; }

        [Required(ErrorMessage = "Organization Type is required.")]
        [BsonElement("org_type")]
        public string OrgType { get; set; } = null!;

        [Required(ErrorMessage = "Companies House Number is required.")]
        [BsonElement("companies_house_number")]
        public string CompaniesHouseNumber { get; set; } = null!;

        [Required(ErrorMessage = "Organization Name is required.")]
        [BsonElement("org_name")]
        public string OrgName { get; set; } = null!;

        [Required(ErrorMessage = "First Name is required.")]
        [BsonElement("first_name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        [BsonElement("last_name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Select a preferred contact number type.")]
        [BsonElement("preferred_contact_type")] 
        [BsonRepresentation(BsonType.String)]
        public PreferredContactType PreferredContactType { get; set; } // Keep non-nullable if it's always required

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Landline number is not in a valid format.")]
        [MaxLength(20, ErrorMessage = "Landline number cannot exceed 20 characters.")]
        [BsonElement("landline_number")] 
        public string? LandlineNumber { get; set; }

        [RegularExpression(@"^\d*$", ErrorMessage = "Extension must be numeric.")]
        [MaxLength(10, ErrorMessage = "Extension cannot exceed 10 characters.")]
        [BsonElement("contact_number_extension")]
        public string? ContactNumberExtension { get; set; }

        [RegularExpression(@"^\+?\d{1,3}[\s-]?\(?\d{1,4}\)?[\s-]?\d{1,4}[\s-]?\d{1,4}[\s-]?\d{1,9}$", ErrorMessage = "Mobile number is not in a valid format.")]
        [MaxLength(13, ErrorMessage = "Mobile number cannot exceed 13 characters.")]
        [BsonElement("mobile_number")]
        public string? MobileNumber { get; set; }

        [Required(ErrorMessage = "Enter your job title.")]
        [MaxLength(100, ErrorMessage = "Job title cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z ]+$", ErrorMessage = "Job title can only contain letters and spaces.")]
        [BsonElement("job_title")]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Organization Registered Address is required.")]
        [BsonElement("org_registered_address")]
        public OrgRegisteredAddress OrgRegisteredAddress { get; set; } = null!;
    }
}
