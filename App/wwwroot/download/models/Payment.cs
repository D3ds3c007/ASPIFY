using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Payment
    {
        [Key]
        public int idPayment { get; set; }
        public double amount { get; set; }
        public DateTime payementDate { get; set; }
        public String refPayment { get; set; }
        public String refDevis { get; set; }
        public int CutomerId { get; set; }

        //Navigation Properties
        public virtual Cutomer Cutomer { get; set; }
        public int ProjectId { get; set; }

        //Navigation Properties
        public virtual Project Project { get; set; }
    }

}
