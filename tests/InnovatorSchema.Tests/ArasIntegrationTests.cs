using System;
using Aras.IOM;
using InnovatorSchema.Testing;
using Xunit;
using V = InnovatorSchema.Validator;

namespace InnovatorSchema.Tests;

public sealed class ArasIntegrationTests
{
    private const string PartId = "783BA98DA5D04BF9AF4B1760826221F0";
    private const string RelatedId = "938599DAAAE34871BFA51167C242DB12";

    [Theory]
    [InlineData("<Item type='Part' id='" + PartId + "'/>")]
    [InlineData("<Item type='Part'><id condition='eq'>" + PartId + "</id></Item>")]
    [InlineData("<Item type='Part' id='p'/>")]
    [InlineData("<Item type='Part' id='" + PartId + "'><id>different</id></Item>")]
    [InlineData("<Item type='Part'><id condition='in'>" + PartId + "</id></Item>")]
    [InlineData("<Item type='Part' id=' '><id>" + PartId + "</id></Item>")]
    public void IdValidationMatchesTheRealIomGetter(string aml)
    {
        var item = ItemFrom(aml);
        var schema = V.Item("Part").ID();
        bool valid = Guid.TryParseExact(item.getID(), "N", out Guid id) && id != Guid.Empty;
        var iomError = Record.Exception(() => V.AssertInput(schema, item));
        var amlError = Record.Exception(() => V.AssertInput(schema, aml));

        Assert.Equal(valid, iomError is null);
        Assert.Equal(valid, amlError is null);
    }

    [Fact]
    public void IomSelectionUsesTheSameRulesAsAmlWithoutMutatingInput()
    {
        var schema = V.Items(V.Item("Part").ID().Property("custom", V.Item("Part").ID()));
        var item = ItemFrom($"<AML><Item type='Part' id='{PartId}'><custom>" +
            $"<Item type='Part' id='{RelatedId}'/></custom></Item>" +
            $"<Item type='Part' id='{RelatedId}'><custom><Item type='Part'/></custom></Item>" +
            "</AML>");
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
        var item = ItemFrom($"<AML><Item type='Document'/><Item type='Part' id='{PartId}'/></AML>");
        var selection = item.getItemsByXPath("//Item[@type='Part']");
        V.AssertInput(V.Items(V.Item("Part").ID()), selection);
        V.AssertInput(V.Item("Part").ID(), selection);
        string id = V.ParseInput(V.Item("Part").ID(), selection).Input.id;
        Assert.Equal(PartId, id);
        var (parts, inn) = V.ParseInput(V.Items(V.Item("Part").ID()), selection);
        string firstId = Assert.Single(parts).id;
        Assert.Equal(PartId, firstId);
        Assert.IsType<Innovator>((object?)inn);
    }

    [Fact]
    public void ParsingAnIomItemCapturesValuesWithoutChangingIt()
    {
        var schema = V.Item("Part").Property("weight", V.Integer()).Property("related_id",
            V.Item("Part").ID());
        var input = ItemFrom("<Item type='Part'><weight>3</weight>" +
            $"<related_id><Item type='Part' id='{RelatedId}'/></related_id></Item>");
        string before = input.dom.OuterXml;

        var (parsed, _) = V.ParseInput(schema, input);

        Assert.Equal(before, input.dom.OuterXml);
        input.setProperty("weight", "8");
        long weight = parsed.weight;
        string relatedId = parsed.related_id.id;
        Assert.Equal(3L, weight);
        Assert.Equal(RelatedId, relatedId);
    }

    [Fact]
    public void IdRuleUsesTheNestedItemsOwnId()
    {
        var schema = V.Item("Part").ID().Property("related_id",
            V.Item("Part").ID().Attribute("action", V.String().OneOf("edit")));

        V.AssertInput(schema, ItemFrom(ParentWithChild(PartId, RelatedId)));

        var error = Assert.Throws<ValidationException>(() =>
            V.AssertInput(schema, ItemFrom(ParentWithChild(PartId, "wrong"))));
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
        Assert.Throws<ValidationException>(() => V.AssertInput(V.Items(V.Item()), item));
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
        V.AssertInput(V.Items(V.Item("Part")), item);
        Assert.Empty(V.ParseInput(V.Items(V.Item("Part")), item).Input);
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
