using System.Text.Json;
using BpmnProperties.Application;
using BpmnProperties.Domain;

namespace BpmnProperties.Domain.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void ElementIdsAreScopedByProcess()
    {
        var first = new BpmnElement(Guid.NewGuid(), "Task_1", "task", "First");
        var second = new BpmnElement(Guid.NewGuid(), "Task_1", "task", "Second");

        Assert.Equal(first.ExternalElementId, second.ExternalElementId);
        Assert.NotEqual(first.ProcessId, second.ProcessId);
    }

    [Fact]
    public void RequiredPropertyRejectsNull()
    {
        var definition = new PropertyDefinition(null, "description", "Description", PropertyDataType.Text);
        definition.Configure(null, true, 0, null);

        var error = PropertyValueValidation.Validate(definition, JsonDocument.Parse("null").RootElement);

        Assert.NotNull(error);
    }

    [Fact]
    public void IntegerPropertyRejectsString()
    {
        var definition = new PropertyDefinition(null, "count", "Count", PropertyDataType.Integer);
        var error = PropertyValueValidation.Validate(definition, JsonDocument.Parse("\"1\"").RootElement);

        Assert.NotNull(error);
    }
}
