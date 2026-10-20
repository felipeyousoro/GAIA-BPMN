using System.Text.Json;
using BpmnProperties.Application;
using BpmnProperties.Domain;
using BpmnProperties.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using BpmnProperties.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Database=bpmn_properties;Username=bpmn_properties;Password=change-me-for-local-use"));
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    builder.Configuration["Storage:AccessKey"] ?? "minio",
    builder.Configuration["Storage:SecretKey"] ?? "change-me-for-local-use",
    new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:ServiceUrl"] ?? "http://localhost:9000",
        ForcePathStyle = true
    }));
builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();

var authority = builder.Configuration["Authentication:Authority"];
if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithName("Health");
app.MapPost("/api/v1/elements/{elementId:guid}/attachments", async (Guid elementId, IFormFile file, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    if (file.Length is <= 0 or > 26_214_400) return Results.BadRequest("File must be between 1 byte and 25 MB.");
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    var key = $"elements/{elementId}/{Guid.NewGuid():N}";
    await using var stream = file.OpenReadStream();
    await storage.PutAsync(key, stream, file.ContentType ?? "application/octet-stream", cancellationToken);
    return Results.Ok(new { key, file.FileName, file.Length, contentType = file.ContentType });
}).DisableAntiforgery();

var processes = app.MapGroup("/api/v1/processes");
processes.MapGet("/", async (AppDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.Processes.AsNoTracking().OrderBy(x => x.Name)
        .Select(x => new ProcessResponse(x.Id, x.ExternalKey, x.Name)).ToListAsync(cancellationToken)));
processes.MapPost("/", async (CreateProcessRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var process = new Process(request.ExternalKey, request.Name);
    db.Processes.Add(process);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/processes/{process.Id}", new ProcessResponse(process.Id, process.ExternalKey, process.Name));
});
processes.MapGet("/{processId:guid}/elements", async (Guid processId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Processes.AnyAsync(x => x.Id == processId, cancellationToken)) return Results.NotFound();
    var result = await db.Elements.AsNoTracking().Where(x => x.ProcessId == processId).OrderBy(x => x.ExternalElementId)
        .Select(x => new ElementResponse(x.Id, x.ProcessId, x.ExternalElementId, x.ElementType, x.Name)).ToListAsync(cancellationToken);
    return Results.Ok(result);
});
processes.MapPost("/{processId:guid}/elements", async (Guid processId, CreateElementRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Processes.AnyAsync(x => x.Id == processId, cancellationToken)) return Results.NotFound();
    var element = new BpmnElement(processId, request.ExternalElementId, request.ElementType, request.Name);
    db.Elements.Add(element);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/elements/{element.Id}", new ElementResponse(element.Id, element.ProcessId, element.ExternalElementId, element.ElementType, element.Name));
});

var definitions = app.MapGroup("/api/v1/property-definitions");
definitions.MapGet("/", async (Guid? processId, AppDbContext db, CancellationToken cancellationToken) =>
{
    var query = db.PropertyDefinitions.AsNoTracking().Where(x => x.IsActive);
    if (processId.HasValue) query = query.Where(x => x.ProcessId == null || x.ProcessId == processId);
    return Results.Ok(await query.OrderBy(x => x.DisplayOrder).Select(x => new PropertyDefinitionResponse(
        x.Id, x.ProcessId, x.Key, x.DisplayName, x.Description, x.DataType, x.IsRequired, x.IsActive,
        x.DisplayOrder, x.DefaultValueJson == null ? null : JsonSerializer.Deserialize<JsonElement>(x.DefaultValueJson))).ToListAsync(cancellationToken));
});
definitions.MapPost("/", async (CreatePropertyDefinitionRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var definition = new PropertyDefinition(request.ProcessId, request.Key, request.DisplayName, request.DataType);
    definition.Configure(request.Description, request.IsRequired, request.DisplayOrder, request.DefaultValue?.GetRawText());
    db.PropertyDefinitions.Add(definition);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/property-definitions/{definition.Id}",
        new PropertyDefinitionResponse(definition.Id, definition.ProcessId, definition.Key, definition.DisplayName, definition.Description,
            definition.DataType, definition.IsRequired, definition.IsActive, definition.DisplayOrder, request.DefaultValue));
});

var elements = app.MapGroup("/api/v1/elements");
elements.MapGet("/{elementId:guid}/properties", async (Guid elementId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    var values = await db.PropertyValues.AsNoTracking().Where(x => x.ElementId == elementId)
        .Select(x => new { x.PropertyDefinitionId, x.ValueJson, x.UpdatedAt }).ToListAsync(cancellationToken);
    return Results.Ok(values.Select(x => new PropertyValueResponse(x.PropertyDefinitionId, JsonSerializer.Deserialize<JsonElement>(x.ValueJson), x.UpdatedAt)));
});
elements.MapPut("/{elementId:guid}/properties/{propertyId:guid}", async (Guid elementId, Guid propertyId, SetPropertyValueRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    var definition = await db.PropertyDefinitions.SingleOrDefaultAsync(x => x.Id == propertyId && x.IsActive, cancellationToken);
    if (definition is null) return Results.NotFound();
    var validationError = PropertyValueValidation.Validate(definition, request.Value);
    if (validationError is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["value"] = [validationError] });
    var value = await db.PropertyValues.SingleOrDefaultAsync(x => x.ElementId == elementId && x.PropertyDefinitionId == propertyId, cancellationToken);
    if (value is null) db.PropertyValues.Add(new PropertyValue(elementId, propertyId, request.Value.GetRawText()));
    else value.Update(request.Value.GetRawText());
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.Run();

public partial class Program { }
