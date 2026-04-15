using System.Text.Json;
using Lathe.Core.Domain;

namespace Lathe.Core.Infrastructure.Logs;

public sealed class JsonLogEventParser
{
    public bool TryParse(string line, string sourceAlias, string sourcePath, out LogEventRecord logEvent)
    {
        logEvent = default!;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("@t", out var timestampProperty))
            {
                return false;
            }

            if (!timestampProperty.TryGetDateTimeOffset(out var timestamp))
            {
                return false;
            }

            var level = root.TryGetProperty("@l", out var levelProperty)
                ? levelProperty.GetString() ?? "Information"
                : "Information";

            var message = root.TryGetProperty("@m", out var renderedMessage)
                ? renderedMessage.GetString()
                : root.TryGetProperty("@mt", out var template)
                    ? template.GetString()
                    : null;

            logEvent = new LogEventRecord(
                timestamp,
                level,
                message ?? line,
                root.TryGetProperty("@x", out var exception) ? exception.GetString() : null,
                sourceAlias,
                sourcePath,
                line)
            {
                Properties = ReadProperties(root),
            };

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(JsonElement root)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.StartsWith('@'))
            {
                continue;
            }

            properties[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.GetRawText();
        }

        return properties;
    }
}
