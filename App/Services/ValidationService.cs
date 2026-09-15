using System.Text.RegularExpressions;
using ASPIFY_MVC.DTO;

namespace ASPIFY_MVC.Services;

/// <summary>
/// Centralized input validation for entity generation - fixes V-02 code injection + path traversal
/// </summary>
public static class ValidationService
{
    // PascalCase, start with letter, 3-50 chars, letters/digits/underscore only - blocks "<", "/", "..", spaces, injection
    private static readonly Regex EntityNameRegex = new(@"^[A-Z][A-Za-z0-9_]{2,50}$", RegexOptions.Compiled);
    private static readonly Regex PropertyNameRegex = new(@"^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "String", "string", "int", "Integer", "double", "Boolean", "bool", "DateTime", "Point"
    };

    // Map informal type names to canonical C# types
    private static readonly Dictionary<string, string> TypeMap = new(StringComparer.Ordinal)
    {
        {"String", "String"}, {"string", "string"},
        {"int", "int"}, {"Integer", "int"},
        {"double", "double"},
        {"Boolean", "Boolean"}, {"bool", "bool"},
        {"DateTime", "DateTime"},
        {"Point", "Point"}
    };

    public static (bool valid, string error) ValidateEntity(Entity entity)
    {
        if (entity == null) return (false, "Entity is null");
        if (string.IsNullOrWhiteSpace(entity.Name))
            return (false, "Entity name is required");
        if (!EntityNameRegex.IsMatch(entity.Name))
            return (false, $"Invalid entity name '{entity.Name}'. Use PascalCase: start with uppercase letter, 3-50 alphanumeric/underscore, e.g., 'Customer'");
        if (entity.Name.Contains("..") || entity.Name.Contains('/') || entity.Name.Contains('\\'))
            return (false, "Entity name contains illegal characters");

        if (entity.Properties == null || entity.Properties.Count == 0)
            return (false, "At least one property is required");
        if (entity.Properties.Count > 50)
            return (false, "Too many properties (max 50)");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in entity.Properties)
        {
            var (v, e) = ValidateProperty(p);
            if (!v) return (false, e);
            if (!seen.Add(p.Name))
                return (false, $"Duplicate property name '{p.Name}'");
        }

        // Relationships are set via separate endpoint, but if present validate targetEntity names
        if (entity.Relationships != null)
        {
            foreach (var r in entity.Relationships)
            {
                if (r == null) continue;
                if (!string.IsNullOrEmpty(r.targetEntity) && !EntityNameRegex.IsMatch(r.targetEntity))
                    return (false, $"Invalid relationship target '{r.targetEntity}'");
                if (!string.IsNullOrEmpty(r.relationName) && r.relationName.Length > 100)
                    return (false, "Relationship name too long");
            }
        }
        return (true, string.Empty);
    }

    public static (bool valid, string error) ValidateProperty(Property prop)
    {
        if (prop == null) return (false, "Property is null");
        if (string.IsNullOrWhiteSpace(prop.Name))
            return (false, "Property name is required");
        if (!PropertyNameRegex.IsMatch(prop.Name))
            return (false, $"Invalid property name '{prop.Name}'. Use alphanumeric/underscore, start with letter, max 50 chars");
        if (prop.Name.Length > 50) return (false, "Property name too long");
        if (string.IsNullOrWhiteSpace(prop.Type))
            return (false, $"Property '{prop.Name}' type is required");
        if (!AllowedTypes.Contains(prop.Type))
            return (false, $"Invalid type '{prop.Type}' for property '{prop.Name}'. Allowed: String, int, double, Boolean, DateTime, Point");
        return (true, string.Empty);
    }

    public static string CanonicalizeType(string type)
    {
        return TypeMap.TryGetValue(type, out var canon) ? canon : "String";
    }

    public static bool IsSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Contains("..") || name.Contains('/') || name.Contains('\\') || Path.IsPathRooted(name)) return false;
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        return EntityNameRegex.IsMatch(name);
    }
}
