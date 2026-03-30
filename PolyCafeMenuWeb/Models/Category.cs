using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PolyCafeMenuWeb.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<Drink> Drinks { get; set; } = new List<Drink>();
    }
}
