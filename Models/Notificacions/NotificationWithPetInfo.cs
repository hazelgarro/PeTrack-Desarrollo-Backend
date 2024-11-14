namespace APIPetrack.Models.Notificacions
{
    public class NotificationWithPetInfo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime NotificationDate { get; set; }

        public int? PetId { get; set; } 
        public string PetPicture { get; set; }
    }
}
