using System.Security.Claims;
using System.Text.Json;

namespace RevEx.Desktop.Core.Authentication;

public static class AccessTokenAuthor
{
    public static string? Read(string? accessToken)
    {
        var parts = accessToken?.Split('.');
        if (parts is not { Length: 3 })
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var claim in new[] { "preferred_username", ClaimTypes.Name, "unique_name", "name", "email", "sub" })
            {
                if (document.RootElement.TryGetProperty(claim, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }

            return null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return null;
        }
    }
}
