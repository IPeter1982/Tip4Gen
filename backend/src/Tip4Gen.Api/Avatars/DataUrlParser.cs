using Tip4Gen.Domain.Users;

namespace Tip4Gen.Api.Avatars;

/// <summary>
/// Decodes the `data:image/...;base64,...` payload the SPA's canvas-resize pipeline
/// posts to avatar endpoints. Pre-decode length cap is sized for 50 KB raw bytes ×
/// 4/3 base64 expansion + slop — cheap O(1) guard before allocating a byte[].
/// </summary>
public static class DataUrlParser
{
    public const int MaxDataUrlLength = (User.MaxAvatarBytes * 4 / 3) + 256;

    public static bool TryParse(string? dataUrl, out string? contentType, out byte[]? bytes)
    {
        contentType = null;
        bytes = null;
        if (string.IsNullOrEmpty(dataUrl) || dataUrl.Length > MaxDataUrlLength) return false;
        if (!dataUrl.StartsWith("data:", StringComparison.Ordinal)) return false;

        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return false;

        var header = dataUrl.AsSpan(5, comma - 5); // skip "data:"
        var semi = header.IndexOf(';');
        if (semi < 0) return false;
        var declaredType = header[..semi].ToString();
        var encoding = header[(semi + 1)..].ToString();
        if (!string.Equals(encoding, "base64", StringComparison.Ordinal)) return false;

        try
        {
            bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return false;
        }
        contentType = declaredType;
        return true;
    }
}
