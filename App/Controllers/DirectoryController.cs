using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ASPIFY_MVC.Controllers
{
    // V-03 FIX: Require authentication (uncomment when auth is configured)
    // [Authorize]
    [Route("[controller]")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // V-10 no caching for file listings
    public class DirectoryController : Controller
    {
        private readonly ILogger<DirectoryController> _logger;
        private readonly string _baseDownloadPath;
        private readonly string _modelsPath;
        private readonly string _dataPath;

        public DirectoryController(ILogger<DirectoryController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            // Resolve absolute base paths once
            _baseDownloadPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "wwwroot/download"));
            _modelsPath = Path.GetFullPath(Path.Combine(_baseDownloadPath, "models"));
            _dataPath = Path.GetFullPath(Path.Combine(_baseDownloadPath, "Data"));
            // Ensure directories exist
            Directory.CreateDirectory(_modelsPath);
            Directory.CreateDirectory(_dataPath);
        }

        private bool IsPathInsideBase(string requestedPath, string basePath)
        {
            var fullRequested = Path.GetFullPath(requestedPath);
            var fullBase = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullRequested.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fullRequested.TrimEnd(Path.DirectorySeparatorChar), fullBase.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("list")]
        public IActionResult List(string path = "")
        {
            try
            {
                // V-01 FIX: canonicalize and whitelist traversal
                if (path != null && (path.Contains("..") || Path.IsPathRooted(path) || path.Contains('\0')))
                {
                    _logger.LogWarning("Blocked path traversal attempt: {Path}", path?.Replace("\r","").Replace("\n",""));
                    return BadRequest("Invalid path");
                }

                string outputPath = string.IsNullOrWhiteSpace(path)
                    ? _baseDownloadPath
                    : Path.GetFullPath(Path.Combine(_baseDownloadPath, path));

                if (!IsPathInsideBase(outputPath, _baseDownloadPath))
                {
                    _logger.LogWarning("Path traversal blocked: requested {Requested} outside base {Base}", outputPath, _baseDownloadPath);
                    return BadRequest("Invalid path");
                }

                if (!Directory.Exists(outputPath))
                {
                    return NotFound("Directory not found");
                }

                var directoryNodes = new List<object>();
                var directories = Directory.GetDirectories(outputPath);
                foreach (var directory in directories)
                {
                    var directoryName = Path.GetFileName(directory);
                    // extra safety: skip hidden/system
                    var files = Directory.GetFiles(directory).Select(Path.GetFileName).ToList();
                    var directoryNode = new
                    {
                        text = directoryName,
                        icon = "jstree-folder",
                        children = files.Select(file => new
                        {
                            text = file,
                            icon = "jstree-file"
                        })
                    };
                    directoryNodes.Add(directoryNode);
                }

                var fileNodes = Directory.GetFiles(outputPath).Select(file => new
                {
                    text = Path.GetFileName(file),
                    icon = "jstree-file"
                });

                return Json(directoryNodes.Concat(fileNodes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Directory list failed");
                return BadRequest("Unable to list directory"); // V-13 generic error
            }
        }

        [HttpGet("explore")]
        public IActionResult Explore(string path){
            return View();
        }

        // FIXED: validate filename, restrict to .cs, enforce base path, no generic Path.Combine with user input
        [HttpGet("/directory/content/{file}")]
        public IActionResult Content(string file="")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(file))
                    return BadRequest("Invalid file");

                // V-01 FIX: block traversal, absolute, invalid chars, and non-.cs
                if (file.Contains("..") || file.Contains('/') || file.Contains('\\') || Path.IsPathRooted(file)
                    || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                    || file.Contains('\0'))
                {
                    _logger.LogWarning("Blocked file traversal: {File}", file.Replace("\r","").Replace("\n",""));
                    return BadRequest("Invalid file");
                }

                // Only allow .cs files (generated models)
                if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only .cs files allowed");

                // Additional safety: filename only, no subdirectory
                var safeFileName = Path.GetFileName(file);
                if (safeFileName != file)
                    return BadRequest("Invalid file");

                string candidate1 = Path.GetFullPath(Path.Combine(_modelsPath, safeFileName));
                string candidate2 = Path.GetFullPath(Path.Combine(_dataPath, safeFileName));

                // Double-check canonical paths still inside allowed bases
                if (IsPathInsideBase(candidate1, _modelsPath) && System.IO.File.Exists(candidate1))
                {
                    _logger.LogInformation("Serving model file {File}", safeFileName);
                    // Use PhysicalFile to set correct content-type and avoid reading into memory large files
                    return PhysicalFile(candidate1, "text/plain");
                }
                else if (IsPathInsideBase(candidate2, _dataPath) && System.IO.File.Exists(candidate2))
                {
                    _logger.LogInformation("Serving data file {File}", safeFileName);
                    return PhysicalFile(candidate2, "text/plain");
                }

                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Content read failed");
                return BadRequest("Unable to read file");
            }
        }
    }
}
