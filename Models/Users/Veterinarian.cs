using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Users
{
    public class Veterinarian
    {

        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(255)]
        public string Password { get; set; }

        [Required]
        [MaxLength(100)]
        public string ClinicName { get; set; }

        public class LoginVeterinarian
        {
            [Required(ErrorMessage = "Blank email is not allowed")]
            [DataType(DataType.EmailAddress)]
            public string Email { get; set; }

            [Required(ErrorMessage = "You must enter your password")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }


    }

}
