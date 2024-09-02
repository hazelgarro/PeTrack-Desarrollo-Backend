﻿using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models
{
    public class PetStoreShelter
    {

        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(255)]
        public string Password { get; set; }

        public class LoginPetStoreShelter
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