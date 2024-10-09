using APIPetrack.Models.Pets;
using APIPetrack.Models.Users;
using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Adoptions
{
    public class AdoptionRequest
    {
        [Key]
        public int Id { get; set; }
        public int PetId { get; set; }
        public int CurrentOwnerId { get; set; }
        public int NewOwnerId { get; set; }
        public string OwnerType { get; set; } // "O" para PetOwner, "S" para PetStoreShelter
        public string IsAccepted { get; set; } //Rejected, Accepted, Pending
        public DateTime RequestDate { get; set; }

        public bool IsDelivered { get; set; }

        public Pet Pet { get; set; } // Propiedad de navegación a la entidad Pet
        public AppUser CurrentOwner { get; set; } // Propiedad de navegación a la entidad User (dueño actual)

        public AppUser NewOwner { get; set; } // Propiedad de navegación a la entidad User (nuevo dueño)
    }
}
