using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class HouseType
    {
        [Key]
        public int idHouseType { get; set; }
        public String name { get; set; }
        public int duration { get; set; }
        public String description { get; set; }
        public double surface { get; set; }
        public virtual ICollection<DevisWork> DevisWorks { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
    }

}
