namespace APIPetrack.Models.Users
{
    public class RegisterUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public char UserTypeId { get; set; }
        public string ProfilePicture { get; set; }
        public string ImagePublicId { get; set; }
        public string PhoneNumber { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }

    }
}
