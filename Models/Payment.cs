using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        [StringLength(15)]
        public string Payment_ID { get; set; }

        [Required]
        [StringLength(15)]
        public string Reservation_ID { get; set; }

        [Required]
        [Column(TypeName = "decimal(9,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Payment_Method { get; set; } = "Cash";

        [Required]
        [StringLength(15)]
        public string Payment_Status { get; set; } = "Completed";

        [StringLength(50)]
        public string? Reference_Number { get; set; }

        public DateTime Payment_Date { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("Reservation_ID")]
        public Reservation? Reservation { get; set; }
    }
}