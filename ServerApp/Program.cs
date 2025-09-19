using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Serilog.Context;
using ServerApp.Models;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog(); // Hook Serilog into ASP.NET Core

// ✅ Register CORS services
builder.Services.AddCors();

builder.Services.AddMemoryCache();

//Enable Swagger for API Discovery
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Health Check Endpoint
builder.Services.AddHealthChecks();

// Read origins from config
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

var token = builder.Configuration["Auth:ApiToken"];

// ✅ Use CORS middleware AFTER registration
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalOnly", policy =>
        policy.WithOrigins(allowedOrigins ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

 app.UseCors("LocalOnly");

//Inject Request ID into Log Context
app.Use(async (context, next) =>
{
    var requestId = context.TraceIdentifier;
    using (LogContext.PushProperty("RequestId", requestId))
    {
        await next();
    }
});

//Global Exception 
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            Log.Error(exception, "Unhandled exception occurred");

            var errorResponse = new
            {
                Message = "An unexpected error occurred.",
                RequestId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
    });
});

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

    if (context.Request.Path.StartsWithSegments("/api") && authHeader != $"Bearer {token}")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: Invalid or missing token.");
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    var elapsedMs = stopwatch.ElapsedMilliseconds;
    Log.Information("Request {Method} {Path} completed in {Elapsed}ms",
        context.Request.Method,
        context.Request.Path,
        elapsedMs);
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapGet("/api/productlist", (
    int? page,
    int? pageSize,
    string? search,
    IMemoryCache cache,
    ILogger<Program> logger) =>
{
    const string cacheKey = "productlist:all";

    Product[] allProducts;

    if (!cache.TryGetValue(cacheKey, out allProducts) || allProducts is null)
    {
        logger.LogInformation("Cache miss — loading full product list");

        allProducts = ProductRepository.LoadProducts();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

        cache.Set(cacheKey, allProducts, cacheOptions);
    }
    else
    {
        logger.LogInformation("Cache hit — using cached product list");
    }

    // Optional filtering
    if (!string.IsNullOrWhiteSpace(search))
    {
        allProducts = allProducts
            .Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    // Pagination
    int currentPage = page ?? 1;
    int size = pageSize ?? 10;
    var paged = allProducts
        .Skip((currentPage - 1) * size)
        .Take(size)
        .ToArray();

    logger.LogInformation("Returned {Count} products (Page {Page}, Size {Size})", paged.Length, currentPage, size);

    return Results.Ok(paged);
});

app.Run();

