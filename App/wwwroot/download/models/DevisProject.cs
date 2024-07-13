using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class DevisProject
    {
        [Key]
        public int idDevisProject { get; set; }
        public double pu { get; set; }
        public double quantity { get; set; }
        public double total { get; set; }
        public int ProjectId { get; set; }

        //Navigation Properties
        public virtual Project Project { get; set; }
        public int WorkId { get; set; }

        //Navigation Properties
        public virtual Work Work { get; set; }
    }

}
