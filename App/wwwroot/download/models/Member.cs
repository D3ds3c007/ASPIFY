using System.ComponentModel.DataAnnotations;

namespace Evaluation2.Models
{
    public class Member
    {
        [Key]
        public int idMember { get; set; }
        public String identifiant { get; set; }
        public String name { get; set; }
        public String pwd { get; set; }
        public int PrivilegeId { get; set; }

        //Navigation Properties
        public virtual Privilege Privilege { get; set; }
    }

}
