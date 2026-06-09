using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Reservation")]
    public class Reservation
    {
        [Key]
        [StringLength(15)]
        public string Reservation_ID { get; set; }

        [Required]
        [StringLength(12)]
        public string Trip_ID { get; set; }

        [Required]
        public int User_ID { get; set; }

        [StringLength(15)]
        public string? Group_ID { get; set; }

        public bool Is_Group_Booking { get; set; } = false;

        [StringLength(100)]
        public string? Contact_Person { get; set; }

        [Required]
        [StringLength(100)]
        public string Passenger_Name { get; set; }

        [StringLength(30)]
        public string? Contact_Number { get; set; }

        [Required]
        [StringLength(40)]
        public string Passenger_Type { get; set; } = "Regular";

        [Column(TypeName = "decimal(5,2)")]
        public decimal Discount_Percentage { get; set; } = 0;

        [StringLength(100)]
        public string? ID_Number { get; set; }

        [Required]
        public DateTime Reservation_Date { get; set; }

        [Required]
        public int Seat_Number { get; set; }

        public int Number_of_Seats { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(9,2)")]
        public decimal Base_Fare { get; set; }

        [Column(TypeName = "decimal(9,2)")]
        public decimal Discount_Applied { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(9,2)")]
        public decimal Total_Amount { get; set; }

        [Required]
        [StringLength(15)]
        public string Status { get; set; } = "Confirmed";

        public DateTime Created_At { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("Trip_ID")]
        public Trip? Trip { get; set; }

        [ForeignKey("User_ID")]
        public Users? CreatedBy { get; set; }

        public Payment? Payment { get; set; }
    }
}