namespace APIPetrack.Models.Adoptions
{
    public class CreateAdoptionRequest
    {
        public int PetId { get; set; }  // ID de la mascota
        public int NewOwnerId { get; set; }  // ID del nuevo dueño (PetOwner solicitando)
    }
}
