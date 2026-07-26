namespace VeterinaryApi.Common.Paginations.OffSet;

public class OffSetPagedList<T>
{
    public IEnumerable<T> Item { get; private set; } = null!;

    public int TotalCount { get; private set; }

    public int PageSize { get; private set; }

    public int Page { get; private set; }

    public bool HasNextPage => Page * PageSize < TotalCount;

    public bool HasPreviousPage => Page > 1;

    private OffSetPagedList()
    {

    }

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
