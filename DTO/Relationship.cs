using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ASPIFY_MVC.DTO;

public class Relationship
{
    [XmlAttribute("type")]
    public String type { get; set; }
    [XmlAttribute("targetEntity")]
    public String targetEntity { get; set; }
    [XmlAttribute("name")]
    public String relationName { get; set; }
}