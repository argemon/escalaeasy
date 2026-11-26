using System.Reflection;
using System.Text.Json;
namespace EscalaEasy.Application.Contracts;

public static class NativeDocumentLimits
{
    public static readonly int MaxBytes = ReadLimit();
    public static int MaxBase64Characters => 4 * ((MaxBytes + 2) / 3);
    private static int ReadLimit()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("EscalaEasy.NativeDocumentLimits")!;
        using var json=JsonDocument.Parse(stream);return json.RootElement.GetProperty("maxBytes").GetInt32();
    }
}
