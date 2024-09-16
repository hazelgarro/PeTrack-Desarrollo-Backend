using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Users
{
    public class Veterinarian
    {

        [Required]
        [Key]
        public int AppUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CompleteName { get; set; }

        [Required]
        [MaxLength(100)]
        public string ClinicName { get; set; }

        [Required]
        [MaxLength(255)]
        public string CoverPicture { get; set; }

        [MaxLength(100)]
        public string WorkingDays { get; set; }

        [MaxLength(50)]
        public string WorkingHours { get; set; }

    }

}
