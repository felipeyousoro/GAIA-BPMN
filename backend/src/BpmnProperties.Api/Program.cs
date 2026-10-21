using System.Text;
using System.Text.Json;
using BpmnProperties.Application;
using BpmnProperties.Domain;
using BpmnProperties.Infrastructure;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 250 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 250 * 1024 * 1024);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "null"];
builder.Services.AddCors(options => options.AddPolicy("desktop", policy => policy
    .SetIsOriginAllowed(origin => allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    .AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition")));
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Database=bpmn_properties;Username=bpmn_properties;******"));
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    builder.Configuration["Storage:AccessKey"] ?? "minio",
    builder.Configuration["Storage:SecretKey"] ?? "change-me-for-local-use",
    new AmazonS3Config { ServiceURL = builder.Configuration["Storage:ServiceUrl"] ?? "http://localhost:9000", ForcePathStyle = true }));
builder.Services.AddSingleton<IObjectStorage, S3ObjectStorage>();

var authority = builder.Configuration["Authentication:Authority"];
if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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
app.UseCors("desktop");
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithName("Health");

var processes = app.MapGroup("/api/v1/processes");
processes.MapGet("/", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var result = await db.Processes.AsNoTracking().OrderByDescending(x => x.UpdatedAt)
        .Select(x => new ProcessResponse(x.Id, x.ExternalKey, x.Name, x.FileName, x.UpdatedAt, x.Elements.Count)).ToListAsync(cancellationToken);
    return Results.Ok(result);
});
processes.MapPost("/import", async (HttpRequest request, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files.SingleOrDefault();
    if (file is null || file.Length <= 0 || file.Length > BpmnXml.MaxBytes || !file.FileName.EndsWith(".bpmn", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Exactly one non-empty .bpmn file up to 10 MB is required.");
    await using var uploaded = file.OpenReadStream();
    BpmnDocument parsed;
    try
    {
        parsed = BpmnXml.Parse(uploaded, file.FileName);
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(exception.Message);
    }
    var process = await db.Processes
        .Include(x => x.Elements)
        .ThenInclude(x => x.PropertyValues)
        .SingleOrDefaultAsync(x => x.ExternalKey == parsed.ExternalKey, cancellationToken);
    if (process is null)
    {
        process = new Process(parsed.ExternalKey, parsed.Name);
        db.Processes.Add(process);
    }
    else process.Rename(parsed.Name);
    var key = $"processes/{process.Id}/diagram.bpmn";
    process.AttachDiagram(key, Path.GetFileName(file.FileName));
    var definitions = await db.PropertyDefinitions.Where(x => x.ProcessId == null && (x.Key == "input" || x.Key == "output")).ToDictionaryAsync(x => x.Key, cancellationToken);
    foreach (var task in parsed.Tasks)
    {
        var element = process.Elements.SingleOrDefault(x => x.ExternalElementId == task.ExternalElementId);
        if (element is null)
        {
            element = new BpmnElement(process.Id, task.ExternalElementId, task.ElementType, task.Name);
            process.Elements.Add(element);
        }
        else element.Rename(task.Name, task.ElementType);
        UpsertValue(db, element, definitions.GetValueOrDefault("input"), task.Input);
        UpsertValue(db, element, definitions.GetValueOrDefault("output"), task.Output);
    }
    await db.SaveChangesAsync(cancellationToken);
    await using var xml = new MemoryStream(Encoding.UTF8.GetBytes(BpmnXml.Serialize(parsed.Xml)));
    await storage.PutAsync(key, xml, "application/xml", cancellationToken);
    return Results.Ok(new ProcessResponse(process.Id, process.ExternalKey, process.Name, process.FileName, process.UpdatedAt, parsed.Tasks.Count));
}).DisableAntiforgery();
processes.MapPost("/", async (CreateProcessRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var process = new Process(request.ExternalKey, request.Name);
    db.Processes.Add(process);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/processes/{process.Id}", new ProcessResponse(process.Id, process.ExternalKey, process.Name, process.FileName, process.UpdatedAt, 0));
});
processes.MapGet("/{processId:guid}/elements", async (Guid processId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Processes.AnyAsync(x => x.Id == processId, cancellationToken)) return Results.NotFound();
    return Results.Ok(await db.Elements.AsNoTracking().Where(x => x.ProcessId == processId).OrderBy(x => x.ExternalElementId)
        .Select(x => new ElementResponse(x.Id, x.ProcessId, x.ExternalElementId, x.ElementType, x.Name)).ToListAsync(cancellationToken));
});
processes.MapPost("/{processId:guid}/elements", async (Guid processId, CreateElementRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Processes.AnyAsync(x => x.Id == processId, cancellationToken)) return Results.NotFound();
    var element = new BpmnElement(processId, request.ExternalElementId, request.ElementType, request.Name);
    db.Elements.Add(element);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/elements/{element.Id}", new ElementResponse(element.Id, element.ProcessId, element.ExternalElementId, element.ElementType, element.Name));
});
processes.MapGet("/{processId:guid}/diagram", async (Guid processId, bool? download, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    var process = await db.Processes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == processId, cancellationToken);
    if (process?.DiagramObjectKey is null) return Results.NotFound();
    var content = await storage.GetAsync(process.DiagramObjectKey, cancellationToken);
    return Results.File(content, "application/xml", download == true ? process.FileName ?? "diagram.bpmn" : null);
});
processes.MapGet("/{processId:guid}/tasks", async (Guid processId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Processes.AnyAsync(x => x.Id == processId, cancellationToken)) return Results.NotFound();
    var inputId = await db.PropertyDefinitions.Where(x => x.ProcessId == null && x.Key == "input").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
    var outputId = await db.PropertyDefinitions.Where(x => x.ProcessId == null && x.Key == "output").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
    var elements = await db.Elements.AsNoTracking().Include(x => x.Attachments).Include(x => x.PropertyValues)
        .Where(x => x.ProcessId == processId).OrderBy(x => x.ExternalElementId).ToListAsync(cancellationToken);
    return Results.Ok(elements.Select(x => new TaskResponse(x.Id, x.ExternalElementId, x.Name,
        ReadValue(x, inputId), ReadValue(x, outputId),
        x.Attachments.OrderBy(a => a.CreatedAt).Select(a => new AttachmentResponse(a.Id, a.FileName, a.ContentType, a.Size, a.CreatedAt)).ToList())));
});
processes.MapPut("/{processId:guid}/tasks", async (Guid processId, SaveTasksRequest request, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    var process = await db.Processes.Include(x => x.Elements).SingleOrDefaultAsync(x => x.Id == processId, cancellationToken);
    if (process?.DiagramObjectKey is null) return Results.NotFound();
    var definitions = await db.PropertyDefinitions.Where(x => x.ProcessId == null && (x.Key == "input" || x.Key == "output")).ToDictionaryAsync(x => x.Key, cancellationToken);
    var values = new Dictionary<string, (string? Input, string? Output)>();
    foreach (var item in request.Tasks)
    {
        var element = process.Elements.SingleOrDefault(x => x.ExternalElementId == item.ExternalElementId);
        if (element is null) continue;
        UpsertValue(db, element, definitions.GetValueOrDefault("input"), item.Input);
        UpsertValue(db, element, definitions.GetValueOrDefault("output"), item.Output);
        values[item.ExternalElementId] = (item.Input, item.Output);
    }
    await using var source = await storage.GetAsync(process.DiagramObjectKey, cancellationToken);
    var parsed = BpmnXml.Parse(source, process.FileName);
    BpmnXml.ApplyTaskValues(parsed.Xml, values.ToDictionary(x => x.Key, x => x.Value));
    process.AttachDiagram(process.DiagramObjectKey!, process.FileName ?? "diagram.bpmn");
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    await using var output = new MemoryStream(Encoding.UTF8.GetBytes(BpmnXml.Serialize(parsed.Xml)));
    await storage.PutAsync(process.DiagramObjectKey, output, "application/xml", cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return Results.NoContent();
});

var elements = app.MapGroup("/api/v1/elements");
elements.MapPost("/{elementId:guid}/attachments", async (Guid elementId, HttpRequest request, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    var form = await request.ReadFormAsync(cancellationToken);
    if (form.Files.Count == 0) return Results.BadRequest("At least one file is required.");
    var result = new List<AttachmentResponse>();
    foreach (var file in form.Files)
    {
        if (file.Length <= 0 || file.Length > 26_214_400) return Results.BadRequest("Each file must be between 1 byte and 25 MB.");
        var key = $"elements/{elementId}/{Guid.NewGuid():N}";
        await using var stream = file.OpenReadStream();
        await storage.PutAsync(key, stream, file.ContentType ?? "application/octet-stream", cancellationToken);
        var attachment = new ElementAttachment(elementId, key, Path.GetFileName(file.FileName), file.ContentType ?? "application/octet-stream", file.Length);
        db.ElementAttachments.Add(attachment);
        result.Add(new AttachmentResponse(attachment.Id, attachment.FileName, attachment.ContentType, attachment.Size, attachment.CreatedAt));
    }
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(result);
}).DisableAntiforgery();
elements.MapGet("/{elementId:guid}/attachments", async (Guid elementId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    return Results.Ok(await db.ElementAttachments.AsNoTracking().Where(x => x.ElementId == elementId).OrderBy(x => x.CreatedAt)
        .Select(x => new AttachmentResponse(x.Id, x.FileName, x.ContentType, x.Size, x.CreatedAt)).ToListAsync(cancellationToken));
});
app.MapGet("/api/v1/attachments/{attachmentId:guid}/content", async (Guid attachmentId, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    var attachment = await db.ElementAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);
    if (attachment is null) return Results.NotFound();
    return Results.File(await storage.GetAsync(attachment.ObjectKey, cancellationToken), attachment.ContentType, attachment.FileName);
});
app.MapDelete("/api/v1/attachments/{attachmentId:guid}", async (Guid attachmentId, AppDbContext db, IObjectStorage storage, CancellationToken cancellationToken) =>
{
    var attachment = await db.ElementAttachments.SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);
    if (attachment is null) return Results.NotFound();
    await storage.DeleteAsync(attachment.ObjectKey, cancellationToken);
    db.ElementAttachments.Remove(attachment);
    await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

var definitions = app.MapGroup("/api/v1/property-definitions");
definitions.MapGet("/", async (Guid? processId, AppDbContext db, CancellationToken cancellationToken) =>
{
    var query = db.PropertyDefinitions.AsNoTracking().Where(x => x.IsActive);
    if (processId.HasValue) query = query.Where(x => x.ProcessId == null || x.ProcessId == processId);
    var definitions = await query.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
    return Results.Ok(definitions.Select(x => new PropertyDefinitionResponse(
        x.Id, x.ProcessId, x.Key, x.DisplayName, x.Description, x.DataType, x.IsRequired, x.IsActive,
        x.DisplayOrder, x.DefaultValueJson is null ? null : JsonSerializer.Deserialize<JsonElement>(x.DefaultValueJson, new JsonSerializerOptions()))));
});
definitions.MapPost("/", async (CreatePropertyDefinitionRequest request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var definition = new PropertyDefinition(request.ProcessId, request.Key, request.DisplayName, request.DataType);
    definition.Configure(request.Description, request.IsRequired, request.DisplayOrder, request.DefaultValue?.GetRawText());
    db.PropertyDefinitions.Add(definition);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/property-definitions/{definition.Id}", new PropertyDefinitionResponse(definition.Id, definition.ProcessId, definition.Key, definition.DisplayName, definition.Description, definition.DataType, definition.IsRequired, definition.IsActive, definition.DisplayOrder, request.DefaultValue));
});
elements.MapGet("/{elementId:guid}/properties", async (Guid elementId, AppDbContext db, CancellationToken cancellationToken) =>
{
    if (!await db.Elements.AnyAsync(x => x.Id == elementId, cancellationToken)) return Results.NotFound();
    var values = await db.PropertyValues.AsNoTracking().Where(x => x.ElementId == elementId).Select(x => new { x.PropertyDefinitionId, x.ValueJson, x.UpdatedAt }).ToListAsync(cancellationToken);
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

static string? ReadValue(BpmnElement element, Guid? definitionId) =>
    definitionId.HasValue
        ? element.PropertyValues.SingleOrDefault(x => x.PropertyDefinitionId == definitionId) is { } value
            ? JsonSerializer.Deserialize<string>(value.ValueJson)
            : null
        : null;

static void UpsertValue(AppDbContext db, BpmnElement element, PropertyDefinition? definition, string? value)
{
    if (definition is null) return;
    var json = JsonSerializer.Serialize(value);
    var current = element.PropertyValues.SingleOrDefault(x => x.PropertyDefinitionId == definition.Id);
    if (current is null) db.PropertyValues.Add(new PropertyValue(element.Id, definition.Id, json));
    else current.Update(json);
}

app.Run();
public partial class Program { }
