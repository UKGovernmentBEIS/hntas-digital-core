namespace HNTAS.Core.Api.Models.Users
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
        public string OrganisationName { get; set; }
        public List<string> Roles { get; set; } // List of friendly role descriptions
        public string Status { get; set; }
    }
}
