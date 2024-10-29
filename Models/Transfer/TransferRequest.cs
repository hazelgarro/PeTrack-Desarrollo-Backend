using APIPetrack.Models.Pets;
using APIPetrack.Models.Users;

namespace APIPetrack.Models.Transfer
{
    public class TransferRequest
    {
        public int Id { get; set; }                
        public int PetId { get; set; }               
        public int CurrentOwnerId { get; set; }     
        public int NewOwnerId { get; set; }         
        public string Status { get; set; }           
        public DateTime RequestDate { get; set; }   

        public Pet Pet { get; set; }
        public AppUser CurrentOwner { get; set; }
        public AppUser NewOwner { get; set; }
    }
}
