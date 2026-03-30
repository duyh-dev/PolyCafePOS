using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class Topping
    {
        [Key]
        public int ToppingID { get; set; }

        [Required]
        [StringLength(100)]
        public string ToppingName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraPrice { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<OrderDetailTopping> OrderDetailToppings { get; set; } = new List<OrderDetailTopping>();
    }
}
