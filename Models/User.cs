using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // 'Admin', 'Validator', 'Booking_Agent'

        public int? SocietyId { get; set; }

        [StringLength(100)]
        public string? TypeVoyage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("SocietyId")]
        public virtual Society? Society { get; set; }
    }
}
