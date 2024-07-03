using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Services;
using Microsoft.AspNetCore.Mvc;
namespace ASPIFY_MVC.Controllers
{
    [Route("[controller]")]
    public class GeneratorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult General()
        {
            return View();
        }

        [HttpGet("entities")]
        public IActionResult Entities()
        {
            string filename = "entity.xml";
            try{
                if(System.IO.File.Exists(filename))
                {
                    EntityCollection entityCollection = XMLService.Deserialize(filename);
                    ViewBag.Entities = entityCollection.Entities;
                }
            }catch(Exception e)
            {
                return BadRequest(e.Message);
            }
            return View();
        }

        [HttpGet("entities/{name}")]
        public IActionResult Entity(string name)
        {
            string filename = "entity.xml";
            try{
                if(System.IO.File.Exists(filename))
                {
                    EntityCollection entityCollection = XMLService.Deserialize(filename);
                    Entity entity = entityCollection.Entities.Find(e => e.Name == name);
                    return Ok(entity);
                }
            }catch(Exception e)
            {
                return BadRequest(e.Message);
            }
            return View();
        }

        [HttpPost("add-entity")]
        public IActionResult AddEntity([FromBody] Entity entity)
        {
            try{
                EntityCollection entityCollection = new EntityCollection();
                entityCollection.Entities.Add(entity);
                XMLService.Serialize(entityCollection);
                GenerateCode();
                return Ok("Entity Added Successfully!");
         
            }catch(Exception e)
            {
                return BadRequest(e.Message);
            }

        }

        [HttpGet("remove-entity/{name}")]
        public IActionResult RemoveEntity(string name)
        {
            Console.WriteLine(name);
            try{
                XMLService.RemoveNode("entity.xml", "Entity", "Name", name);
                //Delete entity file
                System.IO.File.Delete("wwwroot/download/models/"+name+".cs");
                System.IO.File.Delete("wwwroot/download/data/evalcontext.cs+");
                GenerateCode();

                EntityCollection entityCollection = XMLService.Deserialize("entity.xml");
                foreach(Entity e in entityCollection.Entities)
                {
                    RelationshipService.RemoveRelationship("entity.xml", name+e.Name);
                }
                //redirect to add-entity
                return RedirectToAction("entities");
         
            }catch(Exception e)
            {
                return BadRequest(e.Message);
            }

        }

 
        public static void GenerateCode()
        {
            EntityCollection entityCollection = XMLService.Deserialize("entity.xml");
            Entity entity = entityCollection.Entities[0];
            try{
                foreach(Entity e in entityCollection.Entities)
                {
                    T4Service.renderEntityTemplate("Templates/EntityTemplate.tt", e);
                }
                T4Service.renderContextTemplate("Templates/ContextTemplate.tt", entityCollection);
         
            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

       
        
    }
}
