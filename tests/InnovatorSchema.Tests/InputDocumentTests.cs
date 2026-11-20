using System.Text.Json;
using System.Xml;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class InputDocumentTests
{
    [Fact]
    public void JsonPreservesRepresentationAndPresence()
    {
        using var input = InputDocument.Json(
            "{\"text\":\"2.5\",\"number\":2.5,\"null\":null,\"flag\":true}",
            new ValidationOptions());

        Assert.Equal(InputKind.String, input.Root.Property("text").Kind);
        Assert.Equal("2.5", input.Root.Property("text").Text);
        Assert.Equal(InputKind.Number, input.Root.Property("number").Kind);
        Assert.Equal(InputKind.Null, input.Root.Property("null").Kind);
        Assert.Equal(InputKind.Missing, input.Root.Property("absent").Kind);
        Assert.True(input.Root.Property("flag").Boolean);
    }

    [Fact]
    public void JsonRejectsDuplicateNamesWithAnIndexedPath()
    {
        var error = Assert.Throws<InputFailureException>(() => InputDocument.Json(
            "{\"lines\":[{\"code\":1,\"code\":2}]}", new ValidationOptions()));

        Assert.Equal(IssueCode.DuplicateProperty, error.Code);
        Assert.Equal("$.lines[0].code", error.Path.ToString());
    }

    [Theory]
    [InlineData("{\"x\":1,}")]
    [InlineData("{'x':1}")]
    [InlineData("{x:1}")]
    [InlineData("/* comment */{}")]
    [InlineData("NaN")]
    public void JsonRejectsNonJsonSyntax(string text)
    {
        Assert.ThrowsAny<JsonException>(() => InputDocument.Json(text, new ValidationOptions()));
    }

    [Fact]
    public void JsonBoundsDepthBeforeBuildingTheDocument()
    {
        var options = new ValidationOptions(maximumDepth: 2);
        using var valid = InputDocument.Json("{\"x\":\"[[[[\"}", options);

        Assert.Equal("[[[[", valid.Root.Property("x").Text);
        Assert.Throws<InputFailureException>(() => InputDocument.Json("[[[0]]]", options));
    }

    [Fact]
    public void XmlPreservesLiteralTextAndAttributes()
    {
        using var input = InputDocument.Xml(
            "<Item type='Part'><name> A </name><empty/><none is_null='1'/></Item>",
            new ValidationOptions());

        Assert.Equal(" A ", input.Root.Property("name").Text);
        Assert.Equal(string.Empty, input.Root.Property("empty").Text);
        Assert.Equal(InputKind.String, input.Root.Property("none").Kind);
        Assert.Equal("1", input.Root.Property("none").Attribute("is_null").Text);
        Assert.Equal(InputKind.Missing, input.Root.Property("absent").Kind);
        Assert.Equal("Part", input.Root.Attribute("type").Text);
    }

    [Fact]
    public void XmlDoesNotCollapseSingleElementCollections()
    {
        using var input = InputDocument.Xml(
            "<root><lines><Item type='Part'><name>P-100</name></Item></lines></root>",
            new ValidationOptions());
        InputNode lines = input.Root.Property("lines");

        Assert.Single(lines.Elements);
        Assert.Equal("Part", lines.Elements[0].Attribute("type").Text);
        Assert.Equal("P-100", lines.Property("Item").Property("name").Text);
    }

    [Fact]
    public void XmlDepthMeansElementNestingForTextAndDomInputs()
    {
        var options = new ValidationOptions(maximumDepth: 1);
        using var text = InputDocument.Xml("<name>value</name>", options);
        var document = new XmlDocument();
        document.LoadXml("<name>value</name>");
        using var borrowed = InputDocument.FromXml(
            Assert.IsType<XmlElement>(document.DocumentElement), options);

        Assert.Equal("value", text.Root.Text);
        Assert.Equal(text.Root.Text, borrowed.Root.Text);
    }

    [Fact]
    public void XmlDoesNotHideMixedContentBehindAnExpandedItem()
    {
        using var input = InputDocument.Xml(
            "<reference>text<Item type='Part'/></reference>", new ValidationOptions());

        Assert.False(input.Root.CanReadObject);
    }

    [Fact]
    public void XmlParsingAndBorrowedDocumentsAreBounded()
    {
        Assert.Throws<XmlException>(() => InputDocument.Xml(
            "<!DOCTYPE x [<!ENTITY e 'value'>]><x>&e;</x>", new ValidationOptions()));
        Assert.Throws<InputFailureException>(() => InputDocument.Xml(
            "<a><b><c/></b></a>", new ValidationOptions(maximumDepth: 2)));

        var document = new XmlDocument { PreserveWhitespace = true };
        document.LoadXml("<Item type='Part'><name> A </name></Item>");
        string original = document.OuterXml;
        var element = Assert.IsType<XmlElement>(document.DocumentElement);
        using var input = InputDocument.FromXml(element, new ValidationOptions());

        Assert.Equal(" A ", input.Root.Property("name").Text);
        Assert.Equal(original, document.OuterXml);
        Assert.Same(element, Assert.IsType<XmlInputNode>(input.Root).Element);
    }
}
