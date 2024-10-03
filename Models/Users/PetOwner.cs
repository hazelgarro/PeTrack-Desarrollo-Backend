using System.ComponentModel.DataAnnotations;
using APIPetrack.Models.Pets;

namespace APIPetrack.Models.Users
{
    public class PetOwner
    {
        [Required]
        [Key]
        public int AppUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CompleteName { get; set; }

        public AppUser AppUser { get; set; }

        public ICollection<Pet> Pets { get; set; }
    }
}
