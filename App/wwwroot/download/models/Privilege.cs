using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Privilege
    {
        [Key]
        public int idPrivilege { get; set; }
        public String name { get; set; }
        public virtual ICollection<Member> Members { get; set; }
    }

}
