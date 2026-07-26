using VeterinaryApi.Common.CQRS;

namespace VeterinaryApi.Common.Paginations.OffSet;

public class TableRequest<TResponse> : IQuery<OffSetPagedList<TResponse>>
{
    public int PageSize { get; private set; }

    public int Page { get; private set; }

    public string? search { get; private set; } = null;

    public string? SortColumn { get; private set; } = null;

    public string? SortOrder { get; private set; } = null;

    public TableRequest()
    {

    }

    private TableRequest(
        int pageSize,
        int page,
        string? search,
        string? sortColumn,
        string? sortOrder)
    {
        PageSize = pageSize;
        Page = page;
        this.search = search;
        SortColumn = sortColumn;
        SortOrder = sortOrder;
    }

    public static TableRequest<TResponse> Create(
        int? pageSize,
        int? page,
        string? search = null,
        string? sortColumn = null,
        string? sortOrder = null)
    {
        int pageNumber = page is null || page <= 0 ? 1 : (int)page;
        int size = pageSize is null || pageSize <= 0 ? 10 : (int)pageSize;
        search = string.IsNullOrWhiteSpace(search) ? search : search.Trim().ToLower();
        sortColumn = string.IsNullOrWhiteSpace(sortColumn)
            ? sortColumn : sortColumn.Trim().ToLower();
        sortOrder = string.IsNullOrWhiteSpace(sortOrder)
            ? sortOrder : sortOrder.Trim().ToLower();
        return new TableRequest<TResponse>(
            size,
            pageNumber,
            search,
            sortColumn,
            sortOrder);
    }

}
