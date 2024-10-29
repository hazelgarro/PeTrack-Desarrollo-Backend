namespace APIPetrack.Models.Transfer
{
    public class TransferRequestDto
    {
        public int PetId { get; set; }
        public int CurrentOwnerId { get; set; }
        public string NewOwnerEmail { get; set; }
        public string Password { get; set; }
    }
}
