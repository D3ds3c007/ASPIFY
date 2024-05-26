using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace ASPIFY_MVC.DTO;

public class Property
{
    // Properties
    [XmlAttribute("name")]
    public string Name { get; set; }
    [XmlAttribute("type")]
    public string Type { get; set; }
    [XmlAttribute("isPk")]
    public bool IsPk { get; set; }
    
}