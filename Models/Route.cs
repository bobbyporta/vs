using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PUBReservationSystem.Models
{
    [Table("Routes")]
    public class BusRoute
    {
        [Key]
        [StringLength(15)]
        public string Route_ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Origin { get; set; }

        public bool Is_Archived { get; set; }

        [Required]
        [StringLength(200)]
        public string Destination { get; set; }

        [Required]
        [Column(TypeName = "decimal(9,2)")]
        public decimal Base_Fare { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? Distance_KM { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Estimated_Hours { get; set; }

        public bool Is_Active { get; set; } = true;
        public DateTime? Created_At { get; set; } = DateTime.Now;
        public DateTime? Updated_At { get; set; }

        public int Branch_ID { get; set; }
        [ForeignKey("Branch_ID")]
        public virtual Branch Branch { get; set; }

        
        public int? Origin_Branch_ID { get; set; }    // Note the lack of underscores
        public int? Destination_Branch_ID { get; set; }
        
    }
}