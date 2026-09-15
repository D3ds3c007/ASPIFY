using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
namespace ASPIFY_MVC.DTO;

public class XMLService
{
    // V-17 FIX: lock for read-modify-write race condition
    private static readonly object _xmlLock = new object();

    public static void Serialize(Object obj)
    {
        lock (_xmlLock)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            using(var writer = new StringWriter())
            {
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                var xml = string.Empty;
              
                //Write xml to file
                string fileName = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "entity.xml"));

                if(File.Exists(fileName))
                {
                    // V-16 size check
                    var fi = new FileInfo(fileName);
                    if (fi.Length > 2_000_000) throw new InvalidOperationException("XML file too large");

                    EntityCollection entityCollection = (EntityCollection) obj;
                    if (entityCollection.Entities.Count == 0) throw new InvalidOperationException("No entities to serialize");
                    Entity e = entityCollection.Entities[0];
                    serializer = new XmlSerializer(e.GetType());
                    var xmlSettings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true,
                        CheckCharacters = true
                    };
                    serializer.Serialize(writer, e, namespaces);
                    xml = writer.ToString();
                    xml = xml.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "");
                    if (xml.Length > 0 && xml[0] == '\n') xml = xml.Substring(1);
                    else if (xml.Length > 0 && xml.StartsWith("\r\n")) xml = xml.Substring(2);
                    writer.Close();
                    AppendToNode(fileName, "ArrayOfEntity", xml);
                    return;
                }
                else
                {
                    serializer.Serialize(writer, obj, namespaces);
                    xml = writer.ToString();
                }
                using(var streamWriter = new StreamWriter(fileName, false))
                {
                    streamWriter.Write(xml);
                }
                Console.WriteLine("XML written to file: " + fileName);
            }
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
        // V-06 FIX: whitelist target node
        var allowedNodes = new HashSet<string>{"ArrayOfEntity"};
        if (!allowedNodes.Contains(targetNodeName))
            throw new ArgumentException("Invalid target node");

        // V-05 FIX: secure XmlReader settings
        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = 2_000_000
        };

        var xmlDoc = new XmlDocument { XmlResolver = null };
        using (var reader = XmlReader.Create(fileName, readerSettings))
        {
            xmlDoc.Load(reader);
        }

        var root = xmlDoc.DocumentElement;
        // Use safe lookup, not XPath with user input concatenation for attribute
        var targetNode = root.SelectSingleNode($"//{targetNodeName}"); // targetNodeName is whitelisted above, safe
        if (targetNode != null)
        {
            var newXmlDoc = new XmlDocument { XmlResolver = null };
            // Validate xmlContent size before parsing
            if (xmlContent.Length > 500_000) throw new InvalidOperationException("XML content too large");
            using (var sr = new StringReader(xmlContent))
            using (var r = XmlReader.Create(sr, readerSettings))
            {
                newXmlDoc.Load(r);
            }

            var importedNode = xmlDoc.ImportNode(newXmlDoc.DocumentElement, true);
            targetNode.AppendChild(importedNode);

            var xmlSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };
            using (var writer = XmlWriter.Create(fileName, xmlSettings))
            {
                xmlDoc.Save(writer);
            }
        }
    }

    //function to deserialize xml to EntityCollection object - V-05 FIX
    public static EntityCollection Deserialize(string fileName)
    {
        // Resolve full path and check size
        fileName = Path.GetFullPath(fileName);
        if (!File.Exists(fileName))
            return new EntityCollection(); // return empty rather than throw

        var fi = new FileInfo(fileName);
        if (fi.Length > 2_000_000) throw new InvalidOperationException("XML file too large");
        if (fi.Length == 0) return new EntityCollection();

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, // block XXE/Billion Laughs
            XmlResolver = null,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = 2_000_000,
            MaxArrayLength = 1024 * 102
        };

        var serializer = new XmlSerializer(typeof(EntityCollection));
        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = XmlReader.Create(fs, settings))
        {
            var result = serializer.Deserialize(reader) as EntityCollection;
            return result ?? new EntityCollection();
        }
    }

    //function to remove node with specific attribute value - V-06 FIX XPath injection
    public static void RemoveNode(string fileName, string targetNodeName, string attributeName, string attributeValue)
    {
        lock (_xmlLock)
        {
            // V-06 FIX: strict whitelist, no XPath string concat with user value
            var allowedNodes = new HashSet<string>{"Entity", "Relationships"};
            var allowedAttrs = new HashSet<string>{"Name", "name", "type", "targetEntity"};
            if (!allowedNodes.Contains(targetNodeName))
                throw new ArgumentException("Invalid target node");
            if (!allowedAttrs.Contains(attributeName))
                throw new ArgumentException("Invalid attribute");
            if (string.IsNullOrWhiteSpace(attributeValue) || attributeValue.Length > 100)
                throw new ArgumentException("Invalid attribute value");
            // Allow only safe characters for value (covers entity names & relation names)
            if (!System.Text.RegularExpressions.Regex.IsMatch(attributeValue, @"^[A-Za-z0-9_]{1,100}$") && targetNodeName == "Entity")
            {
                // For Entity Name, strict
                if (!ASPIFY_MVC.Services.ValidationService.IsSafeFileName(attributeValue))
                    throw new ArgumentException("Invalid attribute value");
            }

            fileName = Path.GetFullPath(fileName);
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 1024,
                MaxCharactersInDocument = 2_000_000
            };
            var xmlDoc = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(fileName, readerSettings))
                xmlDoc.Load(reader);

            var root = xmlDoc.DocumentElement;
            // FIXED: iterate and compare via DOM, not XPath with embedded value
            // Select all nodes of that type, then filter in C#
            var allNodes = root.SelectNodes($"//{targetNodeName}");
            if (allNodes != null)
            {
                var toRemove = new List<XmlNode>();
                foreach(XmlNode node in allNodes)
                {
                    var attr = node.Attributes?[attributeName];
                    if (attr != null && attr.Value == attributeValue)
                        toRemove.Add(node);
                }
                if (toRemove.Count > 0)
                {
                    Console.WriteLine($"Removing {toRemove.Count} node(s) for {targetNodeName}[@{attributeName}='{attributeValue}']");
                    foreach(XmlNode node in toRemove)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                    var xmlSettings = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = true,
                        Indent = true
                    };
                    using (var writer = XmlWriter.Create(fileName, xmlSettings))
                    {
                        xmlDoc.Save(writer);
                    }
                }
            }
        }
    }
}
