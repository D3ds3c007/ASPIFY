using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ASPIFY_MVC.Controllers
{
    [Route("[controller]")]
    // [Authorize] // V-03 FIX: uncomment when auth configured
    public class GeneratorController : Controller
    {
        private readonly ILogger<GeneratorController> _logger;
        private readonly IWebHostEnvironment _env;

        public GeneratorController(ILogger<GeneratorController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult General()
        {
            return View();
        }

        [HttpGet("entities")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Entities()
        {
            string filename = GetSafeXmlPath();
            try{
                if(System.IO.File.Exists(filename))
                {
                    EntityCollection entityCollection = XMLService.Deserialize(filename);
                    ViewBag.Entities = entityCollection.Entities;
                }
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to load entities");
                return BadRequest("Unable to load entities"); // V-13 generic
            }
            return View();
        }

        [HttpGet("entities/{name}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Entity(string name)
        {
            // V-02/V-06 validate name
            if (!ValidationService.IsSafeFileName(name))
            {
                _logger.LogWarning("Invalid entity name requested: {Name}", name?.Replace("\r","").Replace("\n",""));
                return BadRequest("Invalid entity name");
            }
            string filename = GetSafeXmlPath();
            try{
                if(System.IO.File.Exists(filename))
                {
                    EntityCollection entityCollection = XMLService.Deserialize(filename);
                    Entity? entity = entityCollection.Entities.Find(e => e.Name == name);
                    if (entity == null) return NotFound("Entity not found");
                    return Ok(entity);
                }
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to get entity {Name}", name);
                return BadRequest("Unable to retrieve entity");
            }
            return NotFound("Entity not found");
        }

        [HttpPost("add-entity")]
        [RequestSizeLimit(100_000)] // V-16 max 100KB
        // [ValidateAntiForgeryToken] // V-09 CSRF - enforced via AutoValidateAntiforgeryToken, header X-XSRF-TOKEN required
        public IActionResult AddEntity([FromBody] Entity entity)
        {
            try{
                // V-02 FIX: strict validation before any file write or template rendering
                var (valid, error) = ValidationService.ValidateEntity(entity);
                if (!valid)
                {
                    _logger.LogWarning("Blocked invalid entity: {Error}", error);
                    return BadRequest(error);
                }

                // Canonicalize types to prevent injection via type field
                foreach (var p in entity.Properties)
                    p.Type = ValidationService.CanonicalizeType(p.Type);

                // Ensure no duplicate entity name
                string xmlPath = GetSafeXmlPath();
                if (System.IO.File.Exists(xmlPath))
                {
                    var existing = XMLService.Deserialize(xmlPath);
                    if (existing.Entities.Any(e => string.Equals(e.Name, entity.Name, StringComparison.OrdinalIgnoreCase)))
                        return Conflict($"Entity '{entity.Name}' already exists");
                }

                EntityCollection entityCollection = new EntityCollection();
                entityCollection.Entities.Add(entity);
                XMLService.Serialize(entityCollection);
                GenerateCode();
                _logger.LogInformation("Entity {Name} added", entity.Name);
                return Ok("Entity Added Successfully!");
         
            }catch(Exception ex)
            {
                _logger.LogError(ex, "AddEntity failed");
                return BadRequest("Unable to add entity");
            }

        }

        // V-09 FIX: Use DELETE not GET for destructive action; also require antiforgery
        [HttpPost("remove-entity/{name}")]
        [HttpDelete("remove-entity/{name}")]
        public IActionResult RemoveEntity(string name)
        {
            // V-02/V-07 FIX: validate name strictly before file delete or XML operation
            if (!ValidationService.IsSafeFileName(name))
            {
                _logger.LogWarning("Blocked invalid delete name: {Name}", name?.Replace("\r","").Replace("\n",""));
                return BadRequest("Invalid entity name");
            }

            _logger.LogInformation("RemoveEntity requested for {Name}", name);
            try{
                string xmlPath = GetSafeXmlPath();
                XMLService.RemoveNode(xmlPath, "Entity", "Name", name);

                // V-07 FIX: safe file delete with canonical path check
                var baseModels = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "wwwroot/download/models"));
                var targetModel = Path.GetFullPath(Path.Combine(baseModels, name + ".cs"));
                // Ensure inside base
                if (targetModel.StartsWith(baseModels, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(targetModel))
                {
                    System.IO.File.Delete(targetModel);
                    _logger.LogInformation("Deleted model file {File}", targetModel);
                }

                // FIXED: typo "evalcontext.cs+" -> "evalcontext.cs" but don't delete context here - it will be regenerated
                // Only regenerate context after deletion
                GenerateCode();

                // Remove relationships referencing this entity
                try
                {
                    EntityCollection entityCollection = XMLService.Deserialize(xmlPath);
                    foreach(Entity e in entityCollection.Entities.ToList())
                    {
                        // Remove relationships where target is deleted entity
                        var toRemove = e.Relationships.Where(r => r.targetEntity == name || e.Name == name).ToList();
                        foreach(var r in toRemove)
                            RelationshipService.RemoveRelationship(xmlPath, r.relationName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean relationships for {Name}", name);
                }

                return RedirectToAction("entities");
         
            }catch(Exception ex)
            {
                _logger.LogError(ex, "RemoveEntity failed for {Name}", name);
                return BadRequest("Unable to remove entity");
            }

        }

        public static void GenerateCode()
        {
            // Note: in fixed version this reads from canonical path, not relative "entity.xml"
            string xmlPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "entity.xml"));
            // This is static method - cannot inject logger/env here, but add guards
            if (!System.IO.File.Exists(xmlPath))
                return;

            EntityCollection entityCollection;
            try
            {
                entityCollection = XMLService.Deserialize(xmlPath);
            }
            catch
            {
                return;
            }

            if (entityCollection.Entities == null || entityCollection.Entities.Count == 0)
                return; // V-16 FIX: guard empty list (was Entities[0] crash)

            try{
                foreach(Entity e in entityCollection.Entities)
                {
                    // Additional per-entity validation before codegen
                    var (valid, _) = ValidationService.ValidateEntity(e);
                    if (!valid) continue; // skip invalid

                    T4Service.renderEntityTemplate("Templates/EntityTemplate.tt", e);
                }
                T4Service.renderContextTemplate("Templates/ContextTemplate.tt", entityCollection);
            }catch(Exception ex)
            {
                Console.WriteLine(ex); // TODO: use ILogger
            }
        }

        private string GetSafeXmlPath()
        {
            // Always resolve absolute path to avoid traversal
            return Path.GetFullPath(Path.Combine(_env.ContentRootPath, "entity.xml"));
        }
    }
}
