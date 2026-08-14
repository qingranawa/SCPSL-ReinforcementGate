using System;
using System.IO;

namespace ReinforcementGate.Tests;

internal static class RepositoryRoot
{
    public static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ReinforcementGate.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find ReinforcementGate.sln above {AppContext.BaseDirectory}.");
    }
}
