using Lathe.Core.Domain;
using System.Text;
using System.Text.Json;

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

            var message = ReadMessage(root);

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

    private static string? ReadMessage(JsonElement root)
    {
        if (root.TryGetProperty("@m", out var renderedMessage))
        {
            return renderedMessage.GetString();
        }

        if (!root.TryGetProperty("@mt", out var template))
        {
            return null;
        }

        var value = template.GetString();
        return string.IsNullOrWhiteSpace(value)
            ? value
            : RenderMessageTemplate(value, root);
    }

    private static string RenderMessageTemplate(string template, JsonElement root)
    {
        var builder = new StringBuilder(template.Length + 32);

        for (var index = 0; index < template.Length; index++)
        {
            var current = template[index];

            if (current == '{')
            {
                if (index + 1 < template.Length && template[index + 1] == '{')
                {
                    builder.Append('{');
                    index++;
                    continue;
                }

                var end = template.IndexOf('}', index + 1);
                if (end < 0)
                {
                    builder.Append(current);
                    continue;
                }

                var token = template[(index + 1)..end];
                builder.Append(RenderToken(token, root, template[index..(end + 1)]));
                index = end;
                continue;
            }

            if (current == '}' && index + 1 < template.Length && template[index + 1] == '}')
            {
                builder.Append('}');
                index++;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string RenderToken(string token, JsonElement root, string fallback)
    {
        var propertyName = GetPropertyName(token);
        return root.TryGetProperty(propertyName, out var value)
            ? FormatPropertyValue(value)
            : fallback;
    }

    private static string GetPropertyName(string token)
    {
        var span = token.AsSpan().Trim();

        if (span.Length > 0 && (span[0] == '@' || span[0] == '$'))
        {
            span = span[1..];
        }

        var end = span.IndexOfAny(',', ':');
        return end >= 0
            ? span[..end].Trim().ToString()
            : span.ToString();
    }

    private static string FormatPropertyValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
}
