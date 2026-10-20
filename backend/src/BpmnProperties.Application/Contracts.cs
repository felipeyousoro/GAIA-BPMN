using System.Text.Json;
using BpmnProperties.Domain;

namespace BpmnProperties.Application;

public sealed record CreateProcessRequest(string ExternalKey, string Name);
public sealed record CreateElementRequest(string ExternalElementId, string? ElementType, string? Name);
public sealed record CreatePropertyDefinitionRequest(
    Guid? ProcessId,
    string Key,
    string DisplayName,
    string? Description,
    PropertyDataType DataType,
    bool IsRequired,
    int DisplayOrder,
    JsonElement? DefaultValue);
public sealed record SetPropertyValueRequest(JsonElement Value);
public sealed record ProcessResponse(Guid Id, string ExternalKey, string Name);
public sealed record ElementResponse(Guid Id, Guid ProcessId, string ExternalElementId, string? ElementType, string? Name);
public sealed record PropertyDefinitionResponse(Guid Id, Guid? ProcessId, string Key, string DisplayName, string? Description, PropertyDataType DataType, bool IsRequired, bool IsActive, int DisplayOrder, JsonElement? DefaultValue);
public sealed record PropertyValueResponse(Guid PropertyDefinitionId, JsonElement Value, DateTimeOffset UpdatedAt);
