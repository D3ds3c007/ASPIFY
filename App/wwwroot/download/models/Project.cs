using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Project
    {
        [Key]
        public int idProject { get; set; }
        public String refDevis { get; set; }
        public String totalPrice { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public DateTime createDate { get; set; }
        public String location { get; set; }
        public double tauxFinition { get; set; }
        public int CutomerId { get; set; }

        //Navigation Properties
        public virtual Cutomer Cutomer { get; set; }
        public virtual ICollection<DevisProject> DevisProjects { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public int FinitionId { get; set; }

        //Navigation Properties
        public virtual Finition Finition { get; set; }
        public int HouseTypeId { get; set; }

        //Navigation Properties
        public virtual HouseType HouseType { get; set; }
    }

}
