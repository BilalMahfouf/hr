using System.Text;
using System.Text.Json;

namespace Shared.Paginations.Cursor;

public static class CursorHelper
{
    public static string Encode(DateTime createdOnUtc, Guid id)
    {
        var data = new CursorData(createdOnUtc, id);
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
    
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
