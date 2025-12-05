using System.ComponentModel.DataAnnotations;

namespace FirstWebApplication.Entities
{
    /// <summary>
    /// Historikk-tabell for status-endringer
    /// Lagrer ALLE statusendringer for et obstacle
    /// </summary>
    public class ObstacleStatus
    {
        [Key]
        public long Id { get; set; }

        // FK til Obstacle
        [Required]
        public long ObstacleId { get; set; }
        public Obstacle? Obstacle { get; set; }

        // FK til StatusType (Registered, Pending, Approved, Rejected)
        [Required]
        public int StatusTypeId { get; set; }
        public StatusType? StatusType { get; set; }

        [Required]
        [StringLength(450)]
        public string ChangedByUserId { get; set; } = string.Empty;
        public ApplicationUser? ChangedByUser { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Comments { get; set; }

        public bool IsActive { get; set; } = true;
    }
}