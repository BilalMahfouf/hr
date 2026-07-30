using Shared.CQRS;

namespace Shared.Paginations.Cursor;

public class CursorRequest<TResponse> : IQuery<CursorPagedList<TResponse>>
{
    public int PageSize { get; private set; }

    public string? Cursor { get; private set; }

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
        size = Math.Min(size, 100);

        var dir = ParseDirection(direction);
        var search = string.IsNullOrWhiteSpace(cursor) ?
            null : cursor.Trim().ToLower();

        return new CursorRequest<TResponse>(size, cursor, dir,search);
    }
    private static CursorDirection ParseDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return CursorDirection.Next;
        direction = direction.Trim().ToLower();
        return direction switch
        {
            "next" => CursorDirection.Next,
            "prev" => CursorDirection.Prev,
            _ => CursorDirection.Next
        };
    }
}
