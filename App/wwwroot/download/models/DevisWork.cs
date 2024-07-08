using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class DevisWork
    {
        [Key]
        public int idDevisWork { get; set; }
        public double quantity { get; set; }
        public int WorkId { get; set; }

        //Navigation Properties
        public virtual Work Work { get; set; }
        public int HouseTypeId { get; set; }

        //Navigation Properties
        public virtual HouseType HouseType { get; set; }
    }

}
