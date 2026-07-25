using VeterinaryApi.Common.CQRS;

namespace VeterinaryApi.Common.Paginations.OffSet;

/// <summary>
/// Query model for offset-paginated, searchable, and sortable table requests.
/// Implements <see cref="IQuery{TResponse}"/> so it can be dispatched through the CQRS pipeline.
/// Use <see cref="Create"/> to construct instances with normalized defaults.
/// </summary>
/// <typeparam name="TResponse">The row DTO type for the result table.</typeparam>
public class TableRequest<TResponse> : IQuery<OffSetPagedList<TResponse>>
{
    /// <summary>Number of items to return per page (default: 10).</summary>
    public int PageSize { get; private set; }

    /// <summary>1-based current page number (default: 1).</summary>
    public int Page { get; private set; }

    /// <summary>Optional search keyword; trimmed and lowercased by <see cref="Create"/>.</summary>
    public string? search { get; private set; } = null;

    /// <summary>Optional column name to sort by; trimmed and lowercased.</summary>
    public string? SortColumn { get; private set; } = null;

    /// <summary>Sort direction (“asc” or “desc”); trimmed and lowercased.</summary>
    public string? SortOrder { get; private set; } = null;

    /// <summary>Parameterless constructor for model binding (EF / serialization).</summary>
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

    /// <summary>
    /// Factory method that creates a <see cref="TableRequest{TResponse}"/> with normalized defaults.
    /// Defaults: <c>page = 1</c>, <c>pageSize = 10</c>; whitespace is trimmed and lowercased.
    /// </summary>
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
