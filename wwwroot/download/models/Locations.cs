using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Locations
    {
        [Key]
        public int IdLocation { get; set; }
        public String Name { get; set; }
        public Point Geom { get; set; }
    }

}
