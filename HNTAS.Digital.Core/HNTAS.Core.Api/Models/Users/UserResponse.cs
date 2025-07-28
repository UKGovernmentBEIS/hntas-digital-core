namespace HNTAS.Core.Api.Models.Users
{
    public class UserResponse
    {
        public string Id { get; set; } = null!;
        public string EmailAddress { get; set; } = null!;
        public string? FullName { get; set; }
        public Organisation? Organisation { get; set; }
        public List<string>? Roles { get; set; }
        public string Status { get; set; } = null!;
    }
}
