using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Users")]
    public class Users
    {
        [Key]
        public int User_ID { get; set; }

        [Required]
        [StringLength(15)]
        public string Employee_ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Password_Hash { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        public bool Is_Active { get; set; } = true;
        public DateTime? Last_Login { get; set; }
        public int Login_Attempts { get; set; } = 0;
        public bool Account_Locked { get; set; } = false;
        public DateTime? Locked_At { get; set; }
        public DateTime Created_At { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("Employee_ID")]
        public Employee? Employee { get; set; }

        public ICollection<Reservation>? Reservations { get; set; }
        public ICollection<AuditLog>? AuditLogs { get; set; }
    }
}