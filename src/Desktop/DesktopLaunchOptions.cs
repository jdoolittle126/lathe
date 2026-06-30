using System.Globalization;

namespace Lathe.Desktop;

public sealed record DesktopLaunchOptions(
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Globs,
    IReadOnlyList<string> RollingDirectories,
    int Count)
{
    public const int DefaultCount = 100;

    public bool HasInputs => Files.Count > 0 || Globs.Count > 0 || RollingDirectories.Count > 0;

    public static DesktopLaunchOptions Empty { get; } = new([], [], [], DefaultCount);

    public static DesktopLaunchOptionsParseResult Parse(string[] args)
    {
        var files = new List<string>();
        var globs = new List<string>();
        var rollingDirectories = new List<string>();
        var errors = new List<string>();
        var count = DefaultCount;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (TryReadAssignment(arg, "--file", out var assignedFile))
            {
                files.Add(assignedFile);
                continue;
            }

            if (TryReadAssignment(arg, "--glob", out var assignedGlob))
            {
                globs.Add(assignedGlob);
                continue;
            }

            if (TryReadAssignment(arg, "--rolling", out var assignedRollingDirectory))
            {
                rollingDirectories.Add(assignedRollingDirectory);
                continue;
            }

            if (TryReadAssignment(arg, "--count", out var assignedCount))
            {
                ReadCount(assignedCount, errors, ref count);
                continue;
            }

            switch (arg)
            {
                case "--file":
                    ReadValue(args, ref index, "--file", files, errors);
                    break;
                case "--glob":
                    ReadValue(args, ref index, "--glob", globs, errors);
                    break;
                case "--rolling":
                    ReadValue(args, ref index, "--rolling", rollingDirectories, errors);
                    break;
                case "-n":
                case "--count":
                    if (TryReadValue(args, ref index, arg, errors, out var value))
                    {
                        ReadCount(value, errors, ref count);
                    }

                    break;
                default:
                    errors.Add($"Unknown desktop option '{arg}'.");
                    break;
            }
        }

        var options = errors.Count == 0
            ? new DesktopLaunchOptions(files, globs, rollingDirectories, count)
            : null;

        return new DesktopLaunchOptionsParseResult(options, errors);
    }

    private static bool TryReadAssignment(string arg, string option, out string value)
    {
        var prefix = $"{option}=";

        if (arg.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = arg[prefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static void ReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        ICollection<string> values,
        ICollection<string> errors)
    {
        if (TryReadValue(args, ref index, option, errors, out var value))
        {
            values.Add(value);
        }
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        ICollection<string> errors,
        out string value)
    {
        if (index + 1 >= args.Count)
        {
            errors.Add($"{option} requires a value.");
            value = string.Empty;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static void ReadCount(string value, ICollection<string> errors, ref int count)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            errors.Add("--count must be greater than zero.");
            return;
        }

        count = parsed;
    }
}

public sealed record DesktopLaunchOptionsParseResult(DesktopLaunchOptions? Options, IReadOnlyList<string> Errors);
