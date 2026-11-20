#if ARAS_TESTS
using InnovatorSchema.Testing;
using Xunit;

namespace InnovatorSchema.Tests;

public sealed class ArasIntegrationTests
{
    [Fact]
    public void NestedItemsProduceOutput()
    {
        ArasScenarios.NestedItemOutput();
    }

    [Fact]
    public void RelationshipFailuresCreateErrors()
    {
        ArasScenarios.RelationshipFailureAndErrors();
    }

    [Fact]
    public void CardinalityAndExpansionAreExplicit()
    {
        ArasScenarios.CardinalityAndExpansion();
    }

    [Fact]
    public void ReusableDefinitionsKeepResultsIsolated()
    {
        ArasScenarios.ReusableSchemas();
    }
}
#endif
