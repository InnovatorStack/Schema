using System.Reflection;

namespace InnovatorSchema;

/// <summary>Information about the DLL executing the Method.</summary>
public static class SchemaInfo
{
    /// <summary>Return the loaded DLL's version, including build metadata when available.</summary>
    public static string GetVersion()
    {
        var assembly = typeof(SchemaInfo).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version!.ToString();
    }
}
