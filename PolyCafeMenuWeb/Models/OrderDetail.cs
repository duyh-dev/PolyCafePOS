using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }

        [ForeignKey("Order")]
        public int OrderID { get; set; }

        [ForeignKey("Drink")]
        public int DrinkID { get; set; }

        [ForeignKey("DrinkVariant")]
        public int? VariantID { get; set; }

        [Required]
        [StringLength(100)]
        public string DrinkNameSnapshot { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SizeSnapshot { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceSnapshot { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ToppingTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [StringLength(255)]
        public string? ImageSnapshot { get; set; }

        // Navigation properties
        public Order? Order { get; set; }
        public Drink? Drink { get; set; }
        public DrinkVariant? DrinkVariant { get; set; }
        public ICollection<OrderDetailTopping> OrderDetailToppings { get; set; } = new List<OrderDetailTopping>();
    }
}
