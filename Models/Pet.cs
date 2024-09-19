using APIPetrack.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models
{
    public class Pet
    {
        [Required]
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [MaxLength(50)]
        public string Species { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        public string OwnerTypeId { get; set; }  // 'PetOwner' o 'PetStoreShelter'

        public string HealthIssues { get; set; }

        [MaxLength(255)]
        public string PetPicture { get; set; }


        public PetOwner? PetOwner { get; set; }
        public PetStoreShelter? PetStoreShelter { get; set; }
    }
}
