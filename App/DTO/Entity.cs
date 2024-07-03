using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ASPIFY_MVC.DTO;

public class Entity
{
    // Properties
    [XmlAttribute("Name")]
    public string Name { get; set; }
    public List<Property> Properties { get; set; }
    [XmlElement("Relationships")]
    public List<Relationship> Relationships = new List<Relationship>();

}