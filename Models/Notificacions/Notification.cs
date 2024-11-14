using APIPetrack.Models.Pets;

namespace APIPetrack.Models.Notificacions
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime NotificationDate { get; set; } = DateTime.Now;

        public int? PetId { get; set; }
        public Pet Pet { get; set; }
    }
}
