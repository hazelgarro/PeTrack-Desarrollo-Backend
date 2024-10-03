using System.ComponentModel.DataAnnotations;
using APIPetrack.Models.Pets;

namespace APIPetrack.Models.Users
{
    public class PetStoreShelter
    {

        [Required]
        [Key]
        public int AppUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; }

        [MaxLength(255)]
        public string CoverPicture { get; set; }

        [MaxLength(100)]
        public string WorkingDays { get; set; }

        [MaxLength(50)]
        public string WorkingHours { get; set; }

        public AppUser AppUser { get; set; }

        public ICollection<Pet> Pets { get; set; }
    }
}
