using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
namespace ASPIFY_MVC.DTO;

public class XMLService
{

    public static void Serialize(Object obj)
    {
        XmlSerializer serializer = new XmlSerializer(obj.GetType());
       using(var writer = new StringWriter())
       {
              var namespaces = new XmlSerializerNamespaces();
              namespaces.Add(string.Empty, string.Empty);
              var xml = string.Empty;
              
              //Write xml to file
              string fileName = "entity.xml";

              if(File.Exists(fileName))
              {
                    EntityCollection entityCollection = (EntityCollection) obj;
                    Entity e = entityCollection.Entities[0];
                    serializer = new XmlSerializer(e.GetType());
                    var xmlSettings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true, // Omit the XML declaration
                        Indent = true

                    };
                    serializer.Serialize(writer, e, namespaces);
                    xml = writer.ToString();
                    //Remove the xml decalartion in xml
                    xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                    //Remove blank line at the first line
                    xml = xml.Substring(1);
                    writer.Close();
                    AppendToNode(fileName, "ArrayOfEntity", xml);
                    return;

              }else{
                serializer.Serialize(writer, obj, namespaces);
                xml = writer.ToString();

              }
              using(var streamWriter = new StreamWriter(fileName, true))
              {
                     streamWriter.Write(xml);
              }
              Console.WriteLine("XML written to file: " + fileName);
       }
    }

    static string SerializeToXml(List<Entity> entities, XmlSerializer serializer, XmlWriterSettings xmlSettings)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                {
                    serializer.Serialize(xmlWriter, entities);
                }
                return stringWriter.ToString();
            }
        }

    static void AppendToNode(string fileName, string targetNodeName, string xmlContent)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(File.ReadAllText(fileName)); // Load XML content from the file

        var root = xmlDoc.DocumentElement;
        var targetNode = root.SelectSingleNode($"//{targetNodeName}");
        if (targetNode != null)
        {
            // Create a new XML document for the content to be appended
            var newXmlDoc = new XmlDocument();
            newXmlDoc.LoadXml(xmlContent);

            // Import the new XML content into the existing document
            var importedNode = xmlDoc.ImportNode(newXmlDoc.DocumentElement, true);
            targetNode.AppendChild(importedNode);

            // Save the modified XML content back to the file with the specified settings
            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true, // Omit the XML declaration
                Indent = true // Indent the XML content
            };

            using (var writer = XmlWriter.Create(fileName, xmlSettings))
            {
                xmlDoc.Save(writer);
            }
        }
    }

    //function to deserialize xml to EntityCollection object
    public static EntityCollection Deserialize(string fileName)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(EntityCollection));
        using (var reader = new StreamReader(fileName))
        {
            return (EntityCollection)serializer.Deserialize(reader);
        }
    }

    //function to update relationship to entity 
    

    //function to remove node with specific attribute value
    public static void RemoveNode(string fileName, string targetNodeName, string attributeName, string attributeValue)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(File.ReadAllText(fileName)); // Load XML content from the file

        var root = xmlDoc.DocumentElement;
        var targetNode = root.SelectNodes($"//{targetNodeName}[@{attributeName}='{attributeValue}']");
    
        
        if (targetNode != null)
        {
            Console.WriteLine("Node Found");
            foreach(XmlNode node in targetNode)
            {
                node.ParentNode.RemoveChild(node);
            }
            // Save the modified XML content back to the file with the specified settings
            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true, // Omit the XML declaration
                Indent = true // Indent the XML content
            };

            using (var writer = XmlWriter.Create(fileName, xmlSettings))
            {
                xmlDoc.Save(writer);
            }
        }
    }
 
}