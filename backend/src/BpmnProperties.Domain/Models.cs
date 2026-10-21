namespace BpmnProperties.Domain;

public enum PropertyDataType
{
    Text,
    MultilineText,
    Integer,
    Decimal,
    Boolean,
    Date,
    DateTime,
    SingleSelect,
    MultiSelect,
    FileReference
}

public sealed class Process
{
    private Process() { }

    public Process(string externalKey, string name)
    {
        if (string.IsNullOrWhiteSpace(externalKey)) throw new ArgumentException("External key is required.", nameof(externalKey));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        ExternalKey = externalKey.Trim();
        Name = name.Trim();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string ExternalKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? DiagramObjectKey { get; private set; }
    public string? FileName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public List<BpmnElement> Elements { get; private set; } = [];
    public List<PropertyDefinition> PropertyDefinitions { get; private set; } = [];

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        Name = name.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AttachDiagram(string key, string fileName)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Diagram key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        DiagramObjectKey = key.Trim();
        FileName = fileName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class BpmnElement
{
    private BpmnElement() { }

    public BpmnElement(Guid processId, string externalElementId, string? elementType, string? name)
    {
        if (string.IsNullOrWhiteSpace(externalElementId)) throw new ArgumentException("External element ID is required.", nameof(externalElementId));
        ProcessId = processId;
        ExternalElementId = externalElementId.Trim();
        ElementType = elementType?.Trim();
        Name = name?.Trim();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProcessId { get; private set; }
    public string ExternalElementId { get; private set; } = string.Empty;
    public string? ElementType { get; private set; }
    public string? Name { get; private set; }
    public List<PropertyValue> PropertyValues { get; private set; } = [];
    public List<ElementAttachment> Attachments { get; private set; } = [];

    public void Rename(string? name, string? elementType)
    {
        Name = name?.Trim();
        ElementType = elementType?.Trim();
    }
}

public sealed class ElementAttachment
{
    private ElementAttachment() { }

    public ElementAttachment(Guid elementId, string objectKey, string fileName, string contentType, long size)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) throw new ArgumentException("Object key is required.", nameof(objectKey));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));
        Id = Guid.NewGuid();
        ElementId = elementId;
        ObjectKey = objectKey.Trim();
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        Size = size;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ElementId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long Size { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public sealed class PropertyDefinition
{
    private PropertyDefinition() { }

    public PropertyDefinition(Guid? processId, string key, string displayName, PropertyDataType dataType)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        Id = Guid.NewGuid();
        ProcessId = processId;
        Key = key.Trim();
        DisplayName = displayName.Trim();
        DataType = dataType;
    }

    public Guid Id { get; private set; }
    public Guid? ProcessId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PropertyDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }
    public string? DefaultValueJson { get; private set; }
    public List<PropertyOption> Options { get; private set; } = [];

    public void Configure(string? description, bool isRequired, int displayOrder, string? defaultValueJson)
    {
        Description = description?.Trim();
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
        DefaultValueJson = defaultValueJson;
    }
}

public sealed class PropertyOption
{
    private PropertyOption() { }

    public PropertyOption(Guid propertyDefinitionId, string value, string label, int displayOrder)
    {
        Id = Guid.NewGuid();
        PropertyDefinitionId = propertyDefinitionId;
        Value = value.Trim();
        Label = label.Trim();
        DisplayOrder = displayOrder;
    }

    public Guid Id { get; private set; }
    public Guid PropertyDefinitionId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
}

public sealed class PropertyValue
{
    private PropertyValue() { }

    public PropertyValue(Guid elementId, Guid propertyDefinitionId, string valueJson)
    {
        Id = Guid.NewGuid();
        ElementId = elementId;
        PropertyDefinitionId = propertyDefinitionId;
        ValueJson = valueJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ElementId { get; private set; }
    public Guid PropertyDefinitionId { get; private set; }
    public string ValueJson { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] Version { get; private set; } = [];

    public void Update(string valueJson)
    {
        ValueJson = valueJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
