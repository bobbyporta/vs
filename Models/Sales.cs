using System;
using System.ComponentModel.DataAnnotations;

namespace PUBReservationSystem.Models
{
    public class Sale
    {
        [Key]
        public int Sale_ID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Sale_Date { get; set; }
        // ... add any other properties your sale needs
    }
}