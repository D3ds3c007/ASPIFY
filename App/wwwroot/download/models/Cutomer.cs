using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Cutomer
    {
        [Key]
        public int idCustomer { get; set; }
        public String identifiant { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
    }

}
