using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Bus")]
    public class Bus
    {
        [Key]
        [StringLength(10)]
        public string Bus_ID { get; set; }

        [Required]
        [StringLength(10)]
        public string Plate_Number { get; set; }

        [Required]
        [StringLength(20)]
        public string Body_Bus_Number { get; set; }

        [StringLength(50)]
        public string? Bus_Name { get; set; }

        public string? Bus_Type { get; set; } // "AC" or "OR"

        [Required]
        [StringLength(20)]
        public string Bus_Condition { get; set; } = "Road Worthy";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available";

        public DateTime Created_At { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Trip>? Trips { get; set; }
        public bool IsArchived { get; set; }

        public int Branch_ID { get; set; }
    }
}