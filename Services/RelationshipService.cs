using System.Xml.Serialization;
using ASPIFY_MVC.DTO;

namespace ASPIFY_MVC.Services;
public class RelationshipService
{

    public static void AddRelationship(string filename, string sourceEntityName, string targetEntityName, String relationType)
    {
        EntityCollection entityCollection = XMLService.Deserialize(filename);
        Entity? sourceEntity = entityCollection.Entities.Find(e => e.Name == sourceEntityName);
        Entity? targetEntity = entityCollection.Entities.Find(e => e.Name == targetEntityName);
        EntityCollection specificEntities = new EntityCollection
        {
            Entities = entityCollection.Entities.Where(e => e.Name.Contains(e.Name)).ToList()
        };
        Relationship relationship = new Relationship();

        foreach(var entity in entityCollection.Entities)
        {
            var existingEntity = specificEntities.Entities.FirstOrDefault(e => e.Name == entity.Name);
            Console.WriteLine("Found Entity : " + entity.Name );
            if(existingEntity != null && existingEntity.Name == sourceEntityName || existingEntity.Name == targetEntityName)
            {
                if(existingEntity.Name == sourceEntityName)
                {
                    Console.WriteLine("Source Entity Found"+ sourceEntityName + targetEntityName);
                    relationship.targetEntity = targetEntityName;
                    relationship.type = relationType;
                    relationship.relationName = sourceEntityName + targetEntityName;
                    existingEntity.Relationships.Add(relationship);
                }else{
                    relationship = new Relationship();
                    if(relationType == "OneToMany")
                        relationType = "ManyToOne";
                    Console.WriteLine("Target Entity Found"+ targetEntityName + sourceEntityName);
                    relationship.targetEntity = sourceEntityName;
                    relationship.type = relationType;
                    relationship.relationName = sourceEntityName + targetEntityName;
                    existingEntity.Relationships.Add(relationship);
                }   
            }
        }

        XmlSerializer serializer = new XmlSerializer((typeof(EntityCollection)));
        using (FileStream fs = new FileStream(filename, FileMode.Create))
        {
            serializer.Serialize(fs, specificEntities);
        }
    }

    public static List<Relationship> getAllExistingRelationships(string filename)
    {
        EntityCollection entityCollection = XMLService.Deserialize(filename);
        List<Relationship> relationships = new List<Relationship>();
        foreach(var entity in entityCollection.Entities)
        {
            foreach(var relationship in entity.Relationships)
            {
                    if(relationship.relationName != null)
                        relationships.Add(relationship);
            }
        }
        //It remove the reverse relationships
        relationships = relationships.GroupBy(r => r.relationName).Select(r => r.First()).ToList();

        return relationships;
    }

    //function to remove relationship by name
    public static void RemoveRelationship(string filename, string relationName)
    {
        EntityCollection entityCollection = XMLService.Deserialize(filename);
        foreach(var entity in entityCollection.Entities)
        {
            entity.Relationships.RemoveAll(r => r.relationName == relationName);
        }
        XmlSerializer serializer = new XmlSerializer((typeof(EntityCollection)));
        using (FileStream fs = new FileStream(filename, FileMode.Create))
        {
            serializer.Serialize(fs, entityCollection);
        }
    }
    
}