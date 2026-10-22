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
public sealed record ProcessResponse(Guid Id, string ExternalKey, string Name, string? FileName, DateTimeOffset UpdatedAt, int TaskCount);
public sealed record ElementResponse(Guid Id, Guid ProcessId, string ExternalElementId, string? ElementType, string? Name);
public sealed record AttachmentResponse(Guid Id, string FileName, string ContentType, long Size, DateTimeOffset CreatedAt);
public sealed record TaskResponse(Guid ElementId, string ExternalElementId, string? Name, string? Input, string? Output, IReadOnlyList<AttachmentResponse> Attachments);
public sealed record SaveTaskValue(string ExternalElementId, string? Input, string? Output);
public sealed record SaveTasksRequest(IReadOnlyList<SaveTaskValue> Tasks);
public sealed record PropertyDefinitionResponse(Guid Id, Guid? ProcessId, string Key, string DisplayName, string? Description, PropertyDataType DataType, bool IsRequired, bool IsActive, int DisplayOrder, JsonElement? DefaultValue);
public sealed record PropertyValueResponse(Guid PropertyDefinitionId, JsonElement Value, DateTimeOffset UpdatedAt);

public sealed record LoginRequest(string Username, string Password);
public sealed record UserResponse(Guid Id, string Username, string DisplayName, string Role, bool HasAvatar, DateTimeOffset CreatedAt);
public sealed record LoginResponse(string Token, UserResponse User);
public sealed record UpdateProfileRequest(string DisplayName);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record CreateUserRequest(string Username, string DisplayName, string Password, string Role);
public sealed record ResetPasswordRequest(string NewPassword);

