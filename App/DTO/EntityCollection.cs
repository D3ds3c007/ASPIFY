using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ASPIFY_MVC.DTO;

 [XmlRoot("ArrayOfEntity")]
public class EntityCollection
{
    // Properties
    [XmlElement("Entity")]
    public List<Entity> Entities  = new List<Entity>();
    
}