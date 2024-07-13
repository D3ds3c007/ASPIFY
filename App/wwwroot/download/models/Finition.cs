using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Finition
    {
        [Key]
        public String idFinition { get; set; }
        public String name { get; set; }
        public int coeff { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
    }

}
