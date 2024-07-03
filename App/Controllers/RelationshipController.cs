using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Models;
using ASPIFY_MVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ASPIFY_MVC.Controllers
{
    [Route("[controller]")]
    public class RelationshipController : Controller
    {
         [HttpGet("relations")]
        public IActionResult Relations()
        {
            EntityCollection entityCollection = XMLService.Deserialize("entity.xml");
            List<Relationship> relationships = RelationshipService.getAllExistingRelationships("entity.xml");
            ViewBag.Entities = entityCollection.Entities;
            ViewBag.Relationships = relationships;
            return View();
        }

        [HttpPost("add-relationship")]
        public IActionResult AddRelationship([FromBody] RelationshipDTO relationshipDTO)
        {   
            try{
                RelationshipService.AddRelationship("entity.xml", relationshipDTO.SourceEntity, relationshipDTO.TargetEntity, relationshipDTO.Type);
                GeneratorController.GenerateCode();
                return Ok("Relationship Added Successfully!");
            }catch(Exception e)
            {
                Console.WriteLine(e);
                return BadRequest(e.Message);
            }

        }

        [HttpGet("remove-relationship/{relationName}")]
        public IActionResult RemoveRelationship(string relationName)
        {
            try{
                XMLService.RemoveNode("entity.xml", "Relationships", "name", relationName);
                GeneratorController.GenerateCode();
                return RedirectToAction("relations");
            }catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }
      
    }
}