using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class OrderDetailTopping
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("OrderDetail")]
        public int OrderDetailID { get; set; }

        [ForeignKey("Topping")]
        public int ToppingID { get; set; }

        [Required]
        [StringLength(100)]
        public string ToppingNameSnapshot { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceSnapshot { get; set; }

        public int Quantity { get; set; }

        // Navigation properties
        public OrderDetail? OrderDetail { get; set; }
        public Topping? Topping { get; set; }
    }
}
