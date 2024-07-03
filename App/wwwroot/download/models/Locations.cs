using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Locations
    {
        [Key]
        public int IdLocation { get; set; }

        [Required(ErrorMessage = "Name is required and cannot be empty")]
        public String Name { get; set; }

        [Required(ErrorMessage = "Geom is required and cannot be empty")]
        public Point Geom { get; set; }

    }

}
