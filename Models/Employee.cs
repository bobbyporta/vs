using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        [StringLength(15)]
        public string Employee_ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Full_Name { get; set; }

        [Required]
        [StringLength(15)]
        public string Contact_Number { get; set; }

        [Required]
        [StringLength(10)]
        public string Gender { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Required]
        [StringLength(30)]
        public string Job_Position { get; set; }

        [Required]
        public DateTime Birthday { get; set; }

        [Required]
        public DateTime Hire_Date { get; set; }

        public bool Is_Active { get; set; } = true;

        [StringLength(50)]
        public string? Branch { get; set; }

        // NO navigation properties here

        public int? Branch_ID { get; set; }
    }
}