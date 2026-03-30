using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PolyCafeMenuWeb.Models
{
    public class PaymentMethod
    {
        [Key]
        public int MethodID { get; set; }

        [Required]
        [StringLength(50)]
        public string MethodName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string MethodType { get; set; } = string.Empty; // Cash, Card, E-Wallet

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
