using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Work
    {
        [Key]
        public int idWork { get; set; }
        public String name { get; set; }
        public double pu { get; set; }
        public String unit { get; set; }
        public String code { get; set; }
        public virtual ICollection<DevisWork> DevisWorks { get; set; }
        public virtual ICollection<DevisProject> DevisProjects { get; set; }
    }

}
