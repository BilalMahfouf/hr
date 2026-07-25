namespace VeterinaryApi.Common.Paginations.OffSet;

/// <summary>
/// Generic offset-based paginated result container.
/// Use <see cref="Create"/> to build instances; the default constructor is private.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public class OffSetPagedList<T>
{
    /// <summary>The items in the current page.</summary>
    public IEnumerable<T> Item { get; private set; } = null!;

    /// <summary>Total number of items matching the query (across all pages).</summary>
    public int TotalCount { get; private set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; private set; }

    /// <summary>Current 1-based page number.</summary>
    public int Page { get; private set; }

    /// <summary><c>true</c> if a next page of results exists.</summary>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary><c>true</c> if a previous page of results exists.</summary>
    public bool HasPreviousPage => Page > 1;

    private OffSetPagedList()
    {

    }

    /// <summary>Factory method that creates a paginated result container.</summary>
    /// <param name="items">The current page's items.</param>
    /// <param name="totalCount">Total matching item count.</param>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Page size limit.</param>
    public static OffSetPagedList<T> Create(
        IEnumerable<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        return new OffSetPagedList<T>
        {
            Item = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

}
