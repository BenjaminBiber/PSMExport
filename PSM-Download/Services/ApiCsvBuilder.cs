using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PSM_Download.Services;

public sealed class ApiCsvBuilder
{
    private const char Separator = ';';
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string BuildCsv(IEnumerable? items)
    {
        if (items is null)
        {
            return string.Empty;
        }

        var materialized = new List<object?>();
        foreach (var item in items)
        {
            materialized.Add(item);
        }

        if (materialized.Count == 0)
        {
            return string.Empty;
        }

        if (materialized[0] is JsonElement)
        {
            return BuildFromJsonElements(materialized.Cast<JsonElement>());
        }

        return BuildFromObjects(materialized);
    }

    public static string ToFileName(string methodName)
    {
        if (methodName.StartsWith("GetAll", StringComparison.Ordinal))
        {
            methodName = methodName["GetAll".Length..];
        }

        if (methodName.EndsWith("Async", StringComparison.Ordinal))
        {
            methodName = methodName[..^"Async".Length];
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            return "psm-export";
        }

        var builder = new StringBuilder(methodName.Length);
        for (var i = 0; i < methodName.Length; i++)
        {
            var ch = methodName[i];
            if (char.IsUpper(ch) && i > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static string BuildFromObjects(IReadOnlyList<object?> items)
    {
        var elementType = items.FirstOrDefault(item => item is not null)?.GetType() ?? typeof(object);
        var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0)
            .OrderBy(prop => prop.Name, StringComparer.Ordinal)
            .ToList();
        if (ShouldExcludeExtendedValues(properties.Select(prop => prop.Name)))
        {
            properties = properties
                .Where(prop => !IsExtendedValuesProperty(prop.Name))
                .ToList();
        }

        var builder = new StringBuilder();
        if (properties.Count == 0)
        {
            builder.AppendLine("Value");
            foreach (var item in items)
            {
                builder.AppendLine(EscapeCsv(FormatValue(item)));
            }

            return builder.ToString();
        }

        builder.AppendLine(string.Join(Separator, properties.Select(prop => EscapeCsv(prop.Name))));

        foreach (var item in items)
        {
            if (item is null)
            {
                builder.AppendLine(string.Join(Separator, properties.Select(_ => string.Empty)));
                continue;
            }

            var values = new string[properties.Count];
            for (var i = 0; i < properties.Count; i++)
            {
                var value = properties[i].GetValue(item);
                values[i] = EscapeCsv(FormatValue(value));
            }

            builder.AppendLine(string.Join(Separator, values));
        }

        return builder.ToString();
    }

    private static string BuildFromJsonElements(IEnumerable<JsonElement> items)
    {
        var elements = items.ToList();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (headerSet.Add(property.Name))
                {
                    headers.Add(property.Name);
                }
            }
        }

        if (headers.Count == 0)
        {
            headers.Add("Value");
        }
        else if (ShouldExcludeExtendedValues(headers))
        {
            headers = headers
                .Where(header => !IsExtendedValuesProperty(header))
                .ToList();
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(Separator, headers.Select(EscapeCsv)));

        foreach (var element in elements)
        {
            if (headers.Count == 1 && headers[0] == "Value")
            {
                builder.AppendLine(EscapeCsv(FormatJsonValue(element)));
                continue;
            }

            var values = new string[headers.Count];
            if (element.ValueKind == JsonValueKind.Object)
            {
                for (var i = 0; i < headers.Count; i++)
                {
                    if (element.TryGetProperty(headers[i], out var property))
                    {
                        values[i] = EscapeCsv(FormatJsonValue(property));
                    }
                    else
                    {
                        values[i] = string.Empty;
                    }
                }
            }
            else
            {
                values[0] = EscapeCsv(FormatJsonValue(element));
                for (var i = 1; i < values.Length; i++)
                {
                    values[i] = string.Empty;
                }
            }

            builder.AppendLine(string.Join(Separator, values));
        }

        return builder.ToString();
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is JsonElement jsonElement)
        {
            return FormatJsonValue(jsonElement);
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToString("yyyy-MM-dd 00:00:00", CultureInfo.InvariantCulture);
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (value is bool boolean)
        {
            return boolean ? "true" : "false";
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                parts.Add(FormatValue(item));
            }

            return string.Join(", ", parts);
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string FormatJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => FormatDateString(element.GetString()),
            JsonValueKind.Number => element.ToString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Object => JsonSerializer.Serialize(element, JsonOptions),
            JsonValueKind.Array => JsonSerializer.Serialize(element, JsonOptions),
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => element.ToString() ?? string.Empty
        };
    }

    private static string EscapeCsv(string value)
    {
        return value;
    }

    private static string FormatDateString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var offset))
        {
            return offset.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTime))
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static bool ShouldExcludeExtendedValues(IEnumerable<string> names)
    {
        var nameSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return nameSet.Contains("kennr") &&
               nameSet.Contains("mittelname") &&
               names.Any(IsExtendedValuesProperty);
    }

    private static bool IsExtendedValuesProperty(string name)
    {
        return string.Equals(name, "ExtendedValues", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "extended_values", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "ExtensionData", StringComparison.OrdinalIgnoreCase);
    }
}
