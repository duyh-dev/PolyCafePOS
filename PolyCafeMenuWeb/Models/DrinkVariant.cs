using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class DrinkVariant
    {
        [Key]
        public int VariantID { get; set; }

        [ForeignKey("Drink")]
        public int DrinkID { get; set; }

        [Required]
        [StringLength(50)]
        public string SizeName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Drink? Drink { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
