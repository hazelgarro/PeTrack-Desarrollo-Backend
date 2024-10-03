using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Pets
{
    public class RegisterPetRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string Species { get; set; }

        [Required]
        public string Breed { get; set; }

        [Required]
        public string Gender { get; set; }

        public string Weight { get; set; }

        public string Location { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        public string OwnerType { get; set; }  // 'PetOwner' o 'PetStoreShelter'

        public string HealthIssues { get; set; }

        public string PetPicture { get; set; }
    }
}
