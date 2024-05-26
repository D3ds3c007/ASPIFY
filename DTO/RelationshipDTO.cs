using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ASPIFY_MVC.DTO;

public class RelationshipDTO
{
    public string SourceEntity { get; set;}
    public string TargetEntity { get; set; }
    public string Type { get; set; }
}