using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using BpmnProperties.Application;
using BpmnProperties.Domain;
using Xunit;

namespace BpmnProperties.Domain.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void BpmnXmlParsesTasksAndRoundTripsIo()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""
            <definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL" xmlns:vendor="urn:vendor">
              <process id="Process_1" name="Order"><task id="Task_1" name="Review"><extensionElements><vendor:custom /></extensionElements></task></process>
            </definitions>
            """));

        var parsed = BpmnXml.Parse(stream, "order.bpmn");
        Assert.Single(parsed.Tasks);
        Assert.Equal("Task_1", parsed.Tasks[0].ExternalElementId);

        BpmnXml.ApplyTaskValues(parsed.Xml, new Dictionary<string, (string?, string?)>
        {
            ["Task_1"] = ("Check order", "Approved")
        });

        var text = BpmnXml.Serialize(parsed.Xml);
        Assert.Contains("vendor:custom", text);
        Assert.Contains("Check order", text);
        Assert.Contains("Approved", text);
        Assert.DoesNotContain("encoding=\"utf-16\"", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BpmnXmlRejectsDtd()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""
            <!DOCTYPE definitions [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <definitions><process id="p" name="x"><task id="t" name="&xxe;" /></process></definitions>
            """));

        Assert.Throws<XmlException>(() => BpmnXml.Parse(stream));
    }

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
