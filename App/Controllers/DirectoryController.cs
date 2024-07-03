using ASPIFY_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ASPIFY_MVC.Controllers
{
    [Route("[controller]")]
    public class DirectoryController : Controller
    {
         
        [HttpGet("list")]
        public IActionResult List(string path = "")
        {
            string outputPath = string.IsNullOrEmpty(path) ? Path.Combine("wwwroot/download/") : Path.Combine("wwwroot/download/", path);
            if (!Directory.Exists(outputPath))
            {
                return NotFound("Directory not found");
            }

            var directoryNodes = new List<object>();

            var directories = Directory.GetDirectories(outputPath);
            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                Console.WriteLine(directoryName);
                var files = Directory.GetFiles(directory).ToList();
                var directoryNode = new
                {
                    text = directoryName,
                    icon = "jstree-folder",
                    children = files.Select(file => new
                    {
                        text = Path.GetFileName(file),
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

        

        [HttpGet("explore")]
        public IActionResult Explore(string path){
            return View();
        }

        [HttpGet("/directory/content/{file}")]
        public IActionResult Content(string file="")
        {
            string outputPath1 = Path.Combine("wwwroot/download/models", file);
            string outputPath2 = Path.Combine("wwwroot/download/data/", file);
            if(System.IO.File.Exists(outputPath1))
            {
                Console.WriteLine("File found");
                string content = System.IO.File.ReadAllText(outputPath1);
                return Ok(content);
            }
            else if(System.IO.File.Exists(outputPath2))
            {
                Console.WriteLine("File found2");

                string content = System.IO.File.ReadAllText(outputPath2);
                return Ok(content);
            }
           
            return NotFound("File not found");
        }

    }


}