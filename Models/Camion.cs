using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models
{
    public class Camion
    {
        [Key]
        public int CamionId { get; set; }

        [Required]
        [StringLength(50)]
        public string CamionMatricule { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DriverName { get; set; }

        [StringLength(50)]
        public string? DriverPhone { get; set; }

        [StringLength(50)]
        public string? CamionType { get; set; } // 'Refrigerated', 'Standard', etc.

        public int? SocietyTranspId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("SocietyTranspId")]
        public virtual SocietyTransp? SocietyTransp { get; set; }
    }
}
