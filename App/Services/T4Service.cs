using System;
using System.Collections.Generic;
using System.IO;
using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Templates;


namespace ASPIFY_MVC.Services;

public class T4Service
{
    private static readonly HashSet<string> AllowedTemplatePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "Templates/EntityTemplate.tt",
        "Templates/ContextTemplate.tt"
    };

    public static void renderEntityTemplate(string templatePath, Entity entity)
    {
        // V-02 FIX: validate template path whitelist
        if (!AllowedTemplatePaths.Contains(templatePath))
            throw new ArgumentException("Invalid template path");

        // V-02 FIX: validate entity before any file write
        var (valid, error) = ValidationService.ValidateEntity(entity);
        if (!valid) throw new ArgumentException($"Invalid entity: {error}");

        Console.WriteLine("Rendering Template");

        // Check template exists and resolve full path
        var fullTemplatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), templatePath));
        var templatesBase = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Templates"));
        if (!fullTemplatePath.StartsWith(templatesBase, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Template path outside allowed directory");
        if (!File.Exists(fullTemplatePath))
            throw new FileNotFoundException("Template not found", templatePath);

        // Read with size limit
        var fi = new FileInfo(fullTemplatePath);
        if (fi.Length > 100_000) throw new InvalidOperationException("Template too large");
        string templateContent = File.ReadAllText(fullTemplatePath);
        Console.WriteLine("Engine Created");
        Console.WriteLine("Template Processed");

        EntityTemplate m = new EntityTemplate(entity);
        String classContent = m.TransformText();

        // V-02 FIX: safe output path - whitelist entity.Name already validated
        // Use canonical base path check to prevent traversal
        var baseOutput = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download/models"));
        Directory.CreateDirectory(baseOutput);
        var safeFileName = entity.Name + ".cs"; // entity.Name validated as safe
        string outputPath = Path.GetFullPath(Path.Combine(baseOutput, safeFileName));
        if (!outputPath.StartsWith(baseOutput, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid output path");
        // Restrict extension
        if (!outputPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid extension");

        File.WriteAllText(outputPath, classContent);
        Console.WriteLine($"Wrote entity {entity.Name} to {outputPath}");
    }

    public static void renderContextTemplate(string templatePath, EntityCollection entityCollection)
    {
        if (!AllowedTemplatePaths.Contains(templatePath))
            throw new ArgumentException("Invalid template path");

        Console.WriteLine("Rendering Template");
        var fullTemplatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), templatePath));
        var templatesBase = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Templates"));
        if (!fullTemplatePath.StartsWith(templatesBase, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Template path outside allowed directory");
        if (!File.Exists(fullTemplatePath))
            throw new FileNotFoundException("Template not found", templatePath);

        string templateContent = File.ReadAllText(fullTemplatePath);
        Console.WriteLine("Engine Created");
        Console.WriteLine("Template Processed");

        // Validate all entities before generating context
        foreach(var e in entityCollection.Entities)
        {
            var (valid, err) = ValidationService.ValidateEntity(e);
            if (!valid) throw new ArgumentException($"Invalid entity in collection '{e.Name}': {err}");
        }

        ContextTemplate m = new ContextTemplate(entityCollection);
        String classContent = m.TransformText();

        var baseOutput = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download/Data"));
        Directory.CreateDirectory(baseOutput);
        string outputPath = Path.GetFullPath(Path.Combine(baseOutput, "evalcontext.cs"));
        if (!outputPath.StartsWith(baseOutput, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid output path");

        File.WriteAllText(outputPath, classContent);
        Console.WriteLine($"Wrote context to {outputPath}");
    }
}
