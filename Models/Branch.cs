using System.ComponentModel.DataAnnotations;

namespace PUBReservationSystem.Models
{
    public class Branch
    {
        [Key]
        public int Branch_ID { get; set; }
        public string Branch_Name { get; set; }
        public string Location { get; set; }
        public bool Is_Active { get; set; }
        public bool Is_Archived { get; set; } = false;
    }
}