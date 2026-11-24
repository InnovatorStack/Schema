using Aras.IOM;
using InnovatorSchema.Testing;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ArasIntegrationTests
{
    [Theory]
    [InlineData("<Item type='Part' id='p'/>")]
    [InlineData("<Item type='Part'><id condition='eq'>p</id></Item>")]
    [InlineData("<Item type='Part' id='p'><id>different</id></Item>")]
    [InlineData("<Item type='Part'><id condition='in'>p</id></Item>")]
    [InlineData("<Item type='Part' id=' '><id>p</id></Item>")]
    public void IdValidationMatchesTheRealIomGetter(string aml)
    {
        var item = ItemFrom(aml);
        var schema = V.Item("Part").ID();
        bool hasId = !string.IsNullOrWhiteSpace(item.getID());
        var iomError = Record.Exception(() => V.AssertInput(schema, item));
        var amlError = Record.Exception(() => V.AssertInput(schema, aml));

        Assert.Equal(hasId, iomError is null);
        Assert.Equal(hasId, amlError is null);
    }

    [Fact]
    public void IomSelectionUsesTheSameRulesAsAmlWithoutMutatingInput()
    {
        var schema = V.Array(V.Item("Part").ID().Property("custom", V.Item("Part").ID()));
        var item = ItemFrom("<AML><Item type='Part' id='one'><custom><Item type='Part' id='c'/>" +
            "</custom></Item><Item type='Part' id='two'><custom><Item type='Part'/></custom>" +
            "</Item></AML>");
        Assert.True(item.isCollection());
        string before = item.dom.OuterXml;

        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, item));

        Assert.Equal("$[1].custom.@id", Assert.Single(error.Issues).Path);
        Assert.Equal(before, item.dom.OuterXml);
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Item(), item));
    }

    [Fact]
    public void SelectedRowsExcludeSiblingsOutsideTheSelection()
    {
        var item = ItemFrom("<AML><Item type='Document'/><Item type='Part' id='a'/></AML>");
        var selection = item.getItemsByXPath("//Item[@type='Part']");
        V.AssertInput(V.Array(V.Item("Part").ID()), selection);
        V.AssertInput(V.Item("Part").ID(), selection);
    }

    [Fact]
    public void IdRuleUsesTheNestedItemsOwnId()
    {
        const string parent = "783BA98DA5D04BF9AF4B1760826221F0";
        const string child = "938599DAAAE34871BFA51167C242DB12";
        var schema = V.Item("Part").ID(V.Guid()).Property("related_id",
            V.Item("Part").ID(V.Guid()).Attribute("action", V.Enum("edit")));

        V.AssertInput(schema, ItemFrom(ParentWithChild(parent, child)));

        var error = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, ItemFrom(ParentWithChild(parent, "wrong"))));
        Assert.Equal("$.related_id.@id", Assert.Single(error.Issues).Path);
    }

    [Fact]
    public void UserMessageAndDetailCanBeReturnedAsAnIomError()
    {
        var schema = V.Item("Part").Property("weight", V.Integer().Min(3));
        var input = ItemFrom("<Item type='Part'><weight>2</weight></Item>");
        var error = Assert.Throws<ValidationException>(() => V.AssertInput(schema, input));

        var fault = input.getInnovator().newError(error.Message);
        fault.setErrorDetail(error.GetErrorString());

        Assert.Equal("Weight must be at least 3.", fault.getErrorString());
        Assert.Equal(error.GetErrorString(), fault.getErrorDetail());
    }

    [Fact]
    public void ARegularIomErrorCannotPassAnArrayAssertion()
    {
        var item = OfflineConnection.CreateInnovator().newError("An operation failed.");
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Array(V.Item()), item));
    }

    [Fact]
    public void IomNoItemsFoundUsesEmptyCollectionRules()
    {
        var item = ItemFrom(
            "<SOAP-ENV:Envelope xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/'>" +
            "<SOAP-ENV:Body><SOAP-ENV:Fault><faultcode>0</faultcode>" +
            "<faultstring>No items found.</faultstring></SOAP-ENV:Fault></SOAP-ENV:Body>" +
            "</SOAP-ENV:Envelope>");
        Assert.Equal(0, item.getItemCount());
        V.AssertInput(V.Array(V.Item("Part")), item);
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Item("Part"), item));
    }

    internal static Item ItemFrom(string aml)
    {
        var item = OfflineConnection.CreateInnovator().newItem();
        item.loadAML(aml);
        return item;
    }

    private static string ParentWithChild(string parent, string child)
    {
        return $"<Item type='Part' id='{parent}'><related_id>" +
            $"<Item type='Part' id='{child}' action='edit'/></related_id></Item>";
    }
}
