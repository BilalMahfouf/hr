using VeterinaryApi.Common.CQRS;

namespace VeterinaryApi.Common.Paginations.Cursor;

public class CursorRequest<TResponse> : IQuery<CursorPagedList<TResponse>>
{
    public int PageSize { get; private set; }

    /// <summary>
    /// Opaque cursor string from previous response's NextCursor/PreviousCursor.
    /// Null = start from the beginning (newest items).
    /// </summary>
    public string? Cursor { get; private set; }

    /// <summary>
    /// Direction of pagination.
    /// "next" = get items AFTER cursor (older items, going forward in the list)
    /// "prev" = get items BEFORE cursor (newer items, going backward)
    /// </summary>
    public CursorDirection Direction { get; private set; }

    public string? search { get; private set; } = null;

    private CursorRequest(
        int pageSize,
        string? cursor,
        CursorDirection direction,
        string? search)
    {
        PageSize = pageSize;
        Cursor = cursor;
        Direction = direction;
    }

    public static CursorRequest<TResponse> Create(
        int? pageSize,
        string? cursor,
        string? direction)
    {
        int size = pageSize is null || pageSize <= 0 ? 10 : pageSize.Value;
        size = Math.Min(size, 100); // Cap at 100 to prevent abuse

        var dir = ParseDirection(direction);
        var search = string.IsNullOrWhiteSpace(cursor) ?
            null : cursor.Trim().ToLower();

        return new CursorRequest<TResponse>(size, cursor, dir,search);
    }
    private static CursorDirection ParseDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return CursorDirection.Next; // Default to "next" if not specified
        direction = direction.Trim().ToLower();
        return direction switch
        {
            "next" => CursorDirection.Next,
            "prev" => CursorDirection.Prev,
            _ => CursorDirection.Next
        };
    }
}
