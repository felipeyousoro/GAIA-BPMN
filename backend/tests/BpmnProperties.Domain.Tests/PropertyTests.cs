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
    public void BpmnXmlParsesGatewaysAndEventsAsEditableElements()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""
            <definitions xmlns="http://www.omg.org/spec/BPMN/20100524/MODEL">
              <process id="Process_1" name="Order">
                <startEvent id="Start_1" name="Start" />
                <exclusiveGateway id="Gateway_1" name="Decision" />
                <endEvent id="End_1" name="End" />
              </process>
            </definitions>
            """));

        var parsed = BpmnXml.Parse(stream, "order.bpmn");

        Assert.Empty(parsed.Tasks);
        Assert.Equal(3, parsed.Elements.Count);
        Assert.Contains(parsed.Elements, element => element.ElementType == "startEvent");
        Assert.Contains(parsed.Elements, element => element.ElementType == "exclusiveGateway");
        Assert.Contains(parsed.Elements, element => element.ElementType == "endEvent");
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
    public void OrganizationalEntitiesRequireValidNamesAndPreserveHierarchy()
    {
        var organization = new Organization("  Saúde  ", "SAUDE");
        var suborganization = new Suborganization(organization.Id, "  Vigilância  ", "VIG");

        Assert.Equal("Saúde", organization.Name);
        Assert.Equal("SAUDE", organization.Code);
        Assert.Equal(organization.Id, suborganization.OrganizationId);
        Assert.Equal("Vigilância", suborganization.Name);
    }

    [Fact]
    public void ElementMetadataNormalizesOptionalFields()
    {
        var element = new BpmnElement(Guid.NewGuid(), "Task_1", "task", "Review");

        element.UpdateMetadata("  Description  ", "  People and system  ", "  Follow the checklist  ");

        Assert.Equal("Description", element.Description);
        Assert.Equal("People and system", element.Resources);
        Assert.Equal("Follow the checklist", element.Instructions);
        element.UpdateMetadata(" ", null, "");
        Assert.Null(element.Description);
        Assert.Null(element.Resources);
        Assert.Null(element.Instructions);
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

    [Fact]
    public void UserCreationNormalizesUsernameAndUpdatesProfile()
    {
        var hash = PasswordHasher.Hash("secret123");
        var user = new User("  Admin.User  ", "  Admin User  ", hash, UserRole.Admin);

        Assert.Equal("admin.user", user.Username);
        Assert.Equal("Admin User", user.DisplayName);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Null(user.AvatarObjectKey);

        user.UpdateProfile("  Novo Nome  ");
        Assert.Equal("Novo Nome", user.DisplayName);

        user.SetAvatar("avatars/123", "image/png");
        Assert.Equal("avatars/123", user.AvatarObjectKey);
        Assert.Equal("image/png", user.AvatarContentType);
    }

    [Fact]
    public void PasswordHasherVerifiesAndRejectsInvalidPasswords()
    {
        var hash = PasswordHasher.Hash("admin123");

        Assert.True(PasswordHasher.Verify("admin123", hash));
        Assert.False(PasswordHasher.Verify("wrongpass", hash));
        Assert.False(PasswordHasher.Verify("", hash));

        var user = new User("guest", "Convidado", hash, UserRole.Guest);
        var newHash = PasswordHasher.Hash("novaSenha456");
        user.SetPasswordHash(newHash);

        Assert.False(PasswordHasher.Verify("admin123", user.PasswordHash));
        Assert.True(PasswordHasher.Verify("novaSenha456", user.PasswordHash));
    }
}
