using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Models.Obstacle
{
    /// <summary>
    /// ViewModel for registrering av nye obstacles
    /// </summary>
    public class RegisterObstacleViewModel
    {
        // REMOVED: ObstacleName - Generated automatically by the controller

        [Range(0.1, 10000, ErrorMessage = "Height must be between 0.1 and 10000 meters")]
        [Display(Name = "Height (meters)")]
        public decimal? ObstacleHeight { get; set; }

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? ObstacleDescription { get; set; }

        [Required(ErrorMessage = "Please mark the location on the map")]
        [Display(Name = "Location")]
        public string ObstacleGeometry { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Obstacle Type")]
        public string? ObstacleType { get; set; }
    }
}