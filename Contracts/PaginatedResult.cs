using Microsoft.EntityFrameworkCore;

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    // Additional helper properties
    public int FirstItemIndex => (PageNumber - 1) * PageSize + 1;
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
    public bool HasItems => Items.Any();
    
    // Static factory method for creating empty result
    public static PaginatedResult<T> Empty(int pageNumber, int pageSize) => new()
    {
        Items = Enumerable.Empty<T>(),
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = 0,
        TotalPages = 0
    };

    // Method to create from IQueryable for reusability
    public static async Task<PaginatedResult<T>> CreateAsync(
        IQueryable<T> source, 
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)pageSize)
        };
    }
}

// Optional: Add pagination parameters class for consistency
public class PaginationParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
} 