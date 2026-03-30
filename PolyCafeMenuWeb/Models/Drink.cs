using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class Drink
    {
        [Key]
        public int DrinkID { get; set; }

        [Required]
        [StringLength(100)]
        public string DrinkName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        [ForeignKey("Category")]
        public int CategoryID { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<DrinkVariant> Variants { get; set; } = new List<DrinkVariant>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
