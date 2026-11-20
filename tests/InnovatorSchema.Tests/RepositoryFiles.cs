using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace InnovatorSchema.Tests;

internal static class RepositoryFiles
{
    public static string Root
    {
        get
        {
            string sourceDirectory =
                Path.GetDirectoryName(SourcePath()) ?? AppContext.BaseDirectory;
            return FindRoot(AppContext.BaseDirectory) ??
                FindRoot(Directory.GetCurrentDirectory()) ?? FindRoot(sourceDirectory) ??
                throw new DirectoryNotFoundException("The repository root was not found.");
        }
    }

    private static string? FindRoot(string path)
    {
        var directory = new DirectoryInfo(path);

        for (int depth = 0; directory is not null && depth < 20; depth++)
        {
            if (File.Exists(Path.Combine(directory.FullName, "InnovatorSchema.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string SourcePath([CallerFilePath] string path = "")
    {
        return path;
    }

    public static IEnumerable<string> CSharpSources()
    {
        var pending = new Stack<string>();
        pending.Push(Root);
        int entries = 0;

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                entries++;

                if (entries > 100_000)
                {
                    throw new IOException("Repository scan exceeded 100,000 entries.");
                }

                FileAttributes attributes = File.GetAttributes(entry);

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (!IsGenerated(Path.GetFileName(entry)))
                    {
                        pending.Push(entry);
                    }
                }
                else if (string.Equals(Path.GetExtension(entry), ".cs", StringComparison.Ordinal))
                {
                    yield return entry;
                }
            }
        }
    }

    private static bool IsGenerated(string name)
    {
        return name is ".git" or ".local" or ".vs" or "bin" or "obj" or
            "artifacts" or "TestResults" or "node_modules" or ".vitepress" or ".idea";
    }
}
