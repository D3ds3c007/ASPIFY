using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Require antiforgery by default for unsafe methods (protects V-09 CSRF)
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddResponseCaching();

// SECURITY FIX V-10: Disable compression for HTTPS to mitigate BREACH
// Only enable if you add per-request random token mitigation
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false; // FIXED: was true -> BREACH oracle
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Rate limiting - mitigates V-16 DoS
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 20;
    });
    options.RejectionStatusCode = 429;
});

// TODO V-03: Enable Authentication. Uncomment when Identity/JWT is configured
// builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//     .AddCookie(options => {
//         options.LoginPath = "/Account/Login";
//         options.ExpireTimeSpan = TimeSpan.FromHours(1);
//         options.SlidingExpiration = true;
//         options.Cookie.HttpOnly = true;
//         options.Cookie.SameSite = SameSiteMode.Strict;
//         options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//     });
// builder.Services.AddAuthorization(options => {
//     options.FallbackPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser().Build();
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // includeSubDomains, preload via header
}
else
{
    // Even in Development, show generic error page, not stacktrace
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();

// SECURITY FIX V-11: Restrict static files - deny direct access to /download
// Generated files should be served via authenticated controller, not static middleware
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Deny browsing of generated code
        if (ctx.Context.Request.Path.StartsWithSegments("/download"))
        {
            ctx.Context.Response.StatusCode = 404;
            ctx.Context.Response.ContentLength = 0;
            ctx.Context.Response.Body = Stream.Null;
        }
        // Mitigate MIME-sniff XSS V-12
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    }
});

app.UseResponseCompression();
app.UseResponseCaching();

app.UseRateLimiter();

// SECURITY FIX V-12: Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' https://www.googletagmanager.com https://cdn.jsdelivr.net 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "object-src 'none'; base-uri 'self'; frame-ancestors 'none'");
    // Remove global cache header (V-10) - use per-endpoint ResponseCache instead
    // Do NOT set Public = true globally
    await next();
});

app.UseRouting();

// TODO V-03: Enable when AddAuthentication is configured
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .RequireRateLimiting("fixed");

app.Run();
