
using System.Text;
using System.Text.Json;

namespace VeterinaryApi.Common.Paginations.Cursor;

// <summary>
/// Encodes/decodes cursor from CreatedOnUtc + Id.
/// We use BOTH fields because:
/// - CreatedOnUtc provides ordering (newest first)
/// - Id (GUID) breaks ties when multiple items have same timestamp
/// </summary>
public static class CursorHelper
{
    
    /// <summary>
    /// Encode the last item's position into an opaque cursor string.
    /// Uses Base64 so it's URL-safe and hides implementation details.
    /// </summary>
    public static string Encode(DateTime createdOnUtc, Guid id)
    {
        var data = new CursorData(createdOnUtc, id);
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
    
    /// <summary>
    /// Decode cursor string back to CreatedOnUtc and Id.
    /// Returns null if cursor is invalid/tampered.
    /// </summary>
    public static CursorData? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;
            
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<CursorData>(json);
            
            if (data is null)
                return null;

            return new CursorData(data.CreatedOnUtc, data.Id);
        }
        catch
        {
            return null;
        }
    }
}

public sealed record CursorData(DateTime CreatedOnUtc, Guid Id);
