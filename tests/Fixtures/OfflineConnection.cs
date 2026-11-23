#if ARAS_TESTS
using System;
using System.Reflection;
using Aras.IOM;

namespace InnovatorSchema.Testing;

public class OfflineConnection : DispatchProxy
{
    public static Innovator CreateInnovator()
    {
        return IomFactory.CreateInnovator(CreateConnection());
    }

    public static IServerConnection CreateConnection()
    {
        return Create<IServerConnection, OfflineConnection>();
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        throw new InvalidOperationException("Outbound IOM call: " + targetMethod?.Name);
    }
}
#endif
