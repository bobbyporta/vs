using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Trip")]
    public class Trip
    {
        [Key]
        [StringLength(12)]
        public string Trip_ID { get; set; }

        [StringLength(10)]
        public string? Bus_ID { get; set; }

        [StringLength(15)]
        public string? Employee_ID_Driver { get; set; }

        [StringLength(15)]
        public string? Employee_ID_Conductor { get; set; }

        [Required]
        [StringLength(100)]
        public string Origin { get; set; }

        [Required]
        [StringLength(100)]
        public string Destination { get; set; }

        [Required]
        [Column(TypeName = "decimal(9,2)")]
        public decimal Base_Fare { get; set; }

        [Column("Departure_Time")]
        public DateTime? Scheduled_Departure_Time { get; set; }

        public DateTime? Actual_Dispatch_Time { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "Scheduled";

        public DateTime? Created_At { get; set; } = DateTime.Now;

        // --- THESE WERE MISSING IN MY LAST MESSAGE ---
        public DateTime? Arrival_Time { get; set; }
        public DateTime? Cancelled_Time { get; set; }

        [ForeignKey("Bus_ID")]
        public virtual Bus? Bus { get; set; }

        [ForeignKey("Employee_ID_Driver")]
        public virtual Employee? Driver { get; set; }

        [ForeignKey("Employee_ID_Conductor")]
        public virtual Employee? Conductor { get; set; }
        // ----------------------------------------------

        [StringLength(15)]
        public string? Route_ID { get; set; }

        [ForeignKey("Route_ID")]
        public virtual BusRoute? Route { get; set; }

        public virtual ICollection<Reservation>? Reservations { get; set; }

        public int? Branch_ID { get; set; }

        public int? Destination_Branch_ID { get; set; } // The ID of the branch where the bus is going
    }
}