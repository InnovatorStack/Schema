#if ARAS_TESTS
using System;
using System.Reflection;
using Aras.IOM;

namespace InnovatorSchema.Testing;

public class OfflineConnection : DispatchProxy
{
    public static Innovator CreateInnovator()
    {
        var connection = Create<IServerConnection, OfflineConnection>();

        return IomFactory.CreateInnovator(connection);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        throw new InvalidOperationException("Outbound IOM call: " + targetMethod?.Name);
    }
}
#endif
