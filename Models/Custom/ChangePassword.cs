using System.ComponentModel.DataAnnotations;

namespace APIPetrack.Models.Custom
{
    public class ChangePassword
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }
}
