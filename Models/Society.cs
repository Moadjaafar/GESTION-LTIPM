using System.ComponentModel.DataAnnotations;

namespace GESTION_LTIPN.Models
{
    public class Society
    {
        [Key]
        public int SocietyId { get; set; }

        [Required]
        [StringLength(200)]
        public string SocietyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
