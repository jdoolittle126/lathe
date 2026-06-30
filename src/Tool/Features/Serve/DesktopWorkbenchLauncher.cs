using System.Diagnostics;
using System.Globalization;
using Spectre.Console;

namespace Lathe.Tool.Features.Serve;

public sealed class DesktopWorkbenchLauncher
{
    public bool TryLaunch(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        int count)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var launchTarget = FindLaunchTarget();

        if (launchTarget is null)
        {
            return false;
        }

        var startInfo = new ProcessStartInfo(launchTarget.FileName)
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
        };

        foreach (var argument in launchTarget.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var argument in BuildDesktopArguments(files, globs, rollingDirectories, count))
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            AnsiConsole.MarkupLine("[green]Opening Lathe desktop UI.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not open the desktop UI: {Markup.Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static IEnumerable<string> BuildDesktopArguments(
        IReadOnlyList<string> files,
        IReadOnlyList<string> globs,
        IReadOnlyList<string> rollingDirectories,
        int count)
    {
        foreach (var file in files)
        {
            yield return "--file";
            yield return file;
        }

        foreach (var glob in globs)
        {
            yield return "--glob";
            yield return glob;
        }

        foreach (var rollingDirectory in rollingDirectories)
        {
            yield return "--rolling";
            yield return rollingDirectory;
        }

        yield return "--count";
        yield return count.ToString(CultureInfo.InvariantCulture);
    }

    private static DesktopLaunchTarget? FindLaunchTarget()
    {
        foreach (var probeDirectory in ProbeDesktopOutputDirectories())
        {
            var executablePath = Path.Combine(probeDirectory, "Lathe.Desktop.exe");

            if (File.Exists(executablePath))
            {
                return new DesktopLaunchTarget(executablePath, []);
            }

            var assemblyPath = Path.Combine(probeDirectory, "Lathe.Desktop.dll");

            if (File.Exists(assemblyPath))
            {
                return new DesktopLaunchTarget("dotnet", [assemblyPath]);
            }
        }

        foreach (var root in ProbeRepositoryRoots())
        {
            var projectPath = Path.Combine(root, "src", "Desktop", "Desktop.csproj");

            if (File.Exists(projectPath))
            {
                return new DesktopLaunchTarget("dotnet", ["run", "--project", projectPath, "--"]);
            }
        }

        return null;
    }

    private static IEnumerable<string> ProbeDesktopOutputDirectories()
    {
        var baseDirectory = AppContext.BaseDirectory;

        yield return Path.Combine(baseDirectory, "desktop");
        yield return baseDirectory;

        foreach (var root in ProbeRepositoryRoots())
        {
            yield return Path.Combine(root, "src", "Desktop", "bin", "Debug", "net10.0-windows");
            yield return Path.Combine(root, "src", "Desktop", "bin", "Release", "net10.0-windows");
        }
    }

    private static IEnumerable<string> ProbeRepositoryRoots() =>
        EnumerateParents(Environment.CurrentDirectory)
            .Concat(EnumerateParents(AppContext.BaseDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateParents(string start)
    {
        var directory = new DirectoryInfo(start);

        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private sealed record DesktopLaunchTarget(string FileName, IReadOnlyList<string> Arguments);
}
