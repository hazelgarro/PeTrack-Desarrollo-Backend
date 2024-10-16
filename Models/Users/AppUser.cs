using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Users
{
    public class AppUser
    {
        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password { get; set; }

        [Required]
        public char UserTypeId { get; set; }


        [MaxLength(255)]
        public string ProfilePicture { get; set; }

        [MaxLength(255)]
        public string ImagePublicId { get; set; }

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public class LoginUser
        {
            [Required(ErrorMessage = "Blank email is not allowed")]
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; }

            [Required(ErrorMessage = "You must enter your password")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public PetOwner PetOwner { get; set; }
        public PetStoreShelter PetStoreShelter { get; set; }
    }
}
