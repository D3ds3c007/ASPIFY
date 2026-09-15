using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ASPIFY_MVC.Controllers
{
    [Route("[controller]")]
    // [Authorize] // V-03 FIX: uncomment when auth enabled
    public class RelationshipController : Controller
    {
        private readonly ILogger<RelationshipController> _logger;
        private readonly IWebHostEnvironment _env;

        public RelationshipController(ILogger<RelationshipController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpGet("relations")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Relations()
        {
            try
            {
                string xmlPath = GetSafeXmlPath();
                EntityCollection entityCollection = XMLService.Deserialize(xmlPath);
                List<Relationship> relationships = RelationshipService.getAllExistingRelationships(xmlPath);
                ViewBag.Entities = entityCollection.Entities;
                ViewBag.Relationships = relationships;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load relations");
                ViewBag.Entities = new List<Entity>();
                ViewBag.Relationships = new List<Relationship>();
                return View();
            }
        }

        [HttpPost("add-relationship")]
        [RequestSizeLimit(10_000)]
        public IActionResult AddRelationship([FromBody] RelationshipDTO relationshipDTO)
        {   
            try{
                // V-02 FIX: validate DTO
                if (relationshipDTO == null)
                    return BadRequest("Invalid payload");
                if (!ValidationService.IsSafeFileName(relationshipDTO.SourceEntity))
                    return BadRequest("Invalid source entity");
                if (!ValidationService.IsSafeFileName(relationshipDTO.TargetEntity))
                    return BadRequest("Invalid target entity");

                var allowedTypes = new HashSet<string>{"OneToOne","OneToMany","ManyToOne","ManyToMany"};
                if (!allowedTypes.Contains(relationshipDTO.Type))
                    return BadRequest("Invalid relationship type");

                if (relationshipDTO.SourceEntity == relationshipDTO.TargetEntity)
                    return BadRequest("Source and target must differ");

                string xmlPath = GetSafeXmlPath();
                // Verify both entities exist
                var ec = XMLService.Deserialize(xmlPath);
                if (!ec.Entities.Any(e => e.Name == relationshipDTO.SourceEntity))
                    return BadRequest($"Source entity '{relationshipDTO.SourceEntity}' not found");
                if (!ec.Entities.Any(e => e.Name == relationshipDTO.TargetEntity))
                    return BadRequest($"Target entity '{relationshipDTO.TargetEntity}' not found");

                RelationshipService.AddRelationship(xmlPath, relationshipDTO.SourceEntity, relationshipDTO.TargetEntity, relationshipDTO.Type);
                GeneratorController.GenerateCode();
                _logger.LogInformation("Relationship {Src}->{Tgt} ({Type}) added", relationshipDTO.SourceEntity, relationshipDTO.TargetEntity, relationshipDTO.Type);
                return Ok("Relationship Added Successfully!");
            }catch(Exception ex)
            {
                _logger.LogError(ex, "AddRelationship failed");
                return BadRequest("Unable to add relationship");
            }
        }

        // V-09 FIX: use POST/DELETE, not GET for destructive
        [HttpPost("remove-relationship/{relationName}")]
        [HttpDelete("remove-relationship/{relationName}")]
        public IActionResult RemoveRelationship(string relationName)
        {
            try{
                if (string.IsNullOrWhiteSpace(relationName) || relationName.Length > 120 || relationName.Contains("..") || relationName.Contains('/') || relationName.Contains('\\'))
                    return BadRequest("Invalid relation name");
                // Allow only alphanumeric + underscore, but relationName is concatenated Source+Target, so check combined
                if (!System.Text.RegularExpressions.Regex.IsMatch(relationName, @"^[A-Za-z0-9_]{3,120}$"))
                    return BadRequest("Invalid relation name");

                string xmlPath = GetSafeXmlPath();
                // V-06 FIX: RemoveNode now uses safe comparison, not XPath concat - pass validated name
                XMLService.RemoveNode(xmlPath, "Relationships", "name", relationName);
                GeneratorController.GenerateCode();
                _logger.LogInformation("Relationship {Name} removed", relationName);
                return RedirectToAction("relations");
            }catch(Exception ex)
            {
                _logger.LogError(ex, "RemoveRelationship failed");
                return BadRequest("Unable to remove relationship");
            }
        }

        private string GetSafeXmlPath() => Path.GetFullPath(Path.Combine(_env.ContentRootPath, "entity.xml"));
    }
}
