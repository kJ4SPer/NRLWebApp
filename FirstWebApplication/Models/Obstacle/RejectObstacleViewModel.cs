using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Models.Obstacle
{

    // ViewModel for avvisning av obstacle

    public class RejectObstacleViewModel
    {
        [Required]
        public long ObstacleId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for rejection")]
        [StringLength(255)]
        [Display(Name = "Rejection Reason")]
        public string RejectionReason { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Additional Comments (optional)")]
        public string? Comments { get; set; }
    }
}