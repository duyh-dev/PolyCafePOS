using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyCafeMenuWeb.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        [ForeignKey("Order")]
        public int OrderID { get; set; }

        [ForeignKey("PaymentMethod")]
        public int MethodID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashGiven { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Completed"; // Pending, Completed, Failed

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Order? Order { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}
