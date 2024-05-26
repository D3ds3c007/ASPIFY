using System;
using System.Collections.Generic;
using System.IO;
using ASPIFY_MVC.DTO;
using ASPIFY_MVC.Templates;


namespace ASPIFY_MVC.Services;

public class T4Service
{

    public static void renderEntityTemplate(string templatePath, Entity entity)
    {
        //Load 
        Console.WriteLine("Rendering Template");
        string templateContent = File.ReadAllText(templatePath);
        //var engine = new Engine();
        Console.WriteLine("Engine Created");
        //string output = engine.ProcessTemplate(templateContent, host);
        Console.WriteLine("Template Processed");

        EntityTemplate m = new EntityTemplate(entity);
        String classContent = m.TransformText();


        //Write content to wwwroot
        string outputPath = Path.Combine("wwwroot/download/models/", entity.Name + ".cs");
        File.WriteAllText(outputPath, classContent);
    }

    public static void renderContextTemplate(string templatePath, EntityCollection entityCollection)
    {
        //Load 
        Console.WriteLine("Rendering Template");
        string templateContent = File.ReadAllText(templatePath);
        //var engine = new Engine();
        Console.WriteLine("Engine Created");
        //string output = engine.ProcessTemplate(templateContent, host);
        Console.WriteLine("Template Processed");

        ContextTemplate m = new ContextTemplate(entityCollection);
        String classContent = m.TransformText();

        string outputPath = Path.Combine("wwwroot/download/Data/evalcontext.cs");
        File.WriteAllText(outputPath, classContent);
    }
}