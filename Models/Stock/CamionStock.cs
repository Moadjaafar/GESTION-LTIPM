using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GESTION_LTIPN.Models.Stock
{
    [Table("Camions")]
    public class CamionStock
    {
        [Key]
        public int IdCamion { get; set; }

        [Required]
        [StringLength(50)]
        public string NumeroCamion { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Chauffeur { get; set; }

        [StringLength(50)]
        public string? TypeCamion { get; set; }

        public bool Actif { get; set; } = true;

        public DateTime? DateCreation { get; set; }

        // Navigation properties
        public virtual ICollection<DepartCamion> Departs { get; set; } = new List<DepartCamion>();
    }
}
