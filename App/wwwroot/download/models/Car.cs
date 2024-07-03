using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Car
    {
        [Key]
        public int idCar { get; set; }

        [Required(ErrorMessage = "BrandName is required and cannot be empty")]
        public String BrandName { get; set; }

    }

}
