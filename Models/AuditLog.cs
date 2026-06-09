using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Audit_Log")]
    public class AuditLog
    {
        [Key]
        public int Log_ID { get; set; }

        [Required]
        public int User_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Terminal { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("User_ID")]
        public Users? User { get; set; }
    }
}