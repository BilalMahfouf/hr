namespace VeterinaryApi.Common.Paginations.Cursor;


public class CursorPagedList<T>
{
    public IEnumerable<T> Items { get; private set; } = [];
    public int PageSize { get; private set; }
    public bool HasNextPage { get; private set; }
    public bool HasPreviousPage { get; private set; }

    /// <summary>
    /// Opaque cursor to fetch the NEXT page. 
    /// Pass this as the 'cursor' parameter to get items AFTER the last item.
    /// Null if no more pages.
    /// </summary>
    public string? NextCursor { get; private set; }

    /// <summary>
    /// Opaque cursor to fetch the PREVIOUS page.
    /// Null if this is the first page.
    /// </summary>
    public string? PreviousCursor { get; private set; }

    private CursorPagedList() { }

    public static CursorPagedList<T> Create(
        IEnumerable<T> items,
        int pageSize,
        bool hasNextPage,
        bool hasPreviousPage,
        string? nextCursor,
        string? previousCursor)
    {
        return new CursorPagedList<T>
        {
            Items = items,
            PageSize = pageSize,
            HasNextPage = hasNextPage,
            HasPreviousPage = hasPreviousPage,
            NextCursor = nextCursor,
            PreviousCursor = previousCursor
        };
    }
}
