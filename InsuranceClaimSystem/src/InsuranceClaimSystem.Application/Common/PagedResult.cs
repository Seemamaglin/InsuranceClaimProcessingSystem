namespace InsuranceClaimSystem.Application.Common;

public class PagedResult<T>
{
    public List<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page * PageSize < TotalCount;
    public bool HasPrevious => Page > 1;

    private PagedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page must be greater than or equal to 1.", nameof(page));
        if (pageSize < 1)
            throw new ArgumentException("PageSize must be greater than or equal to 1.", nameof(pageSize));

        return new PagedResult<T>(items, totalCount, page, pageSize);
    }
}