using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SocketTest.Wpf;

internal static class JsonFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static string TryPrettyPrint(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var trimmed = text.TrimStart();
        if (trimmed.Length == 0) return text;
        var first = trimmed[0];
        if (first != '{' && first != '[') return text;

        try
        {
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            return JsonSerializer.Serialize(doc.RootElement, _options);
        }
        catch
        {
            return text;
        }
    }
}
