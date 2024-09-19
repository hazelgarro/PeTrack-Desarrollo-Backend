using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models
{
    public class RegisterPetRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Species { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        public string OwnerType { get; set; }  // 'PetOwner' o 'PetStoreShelter'

        public string HealthIssues { get; set; }

        public string PetPicture { get; set; }
    }
}
