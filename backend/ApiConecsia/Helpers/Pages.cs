namespace ApiConecsia.Helpers;

public sealed class Pages<T> where T : class
{
    public Pages(
        IEnumerable<T> registers,
        int total,
        int pageIndex,
        int pageSize,
        string search)
    {
        Registers = registers;
        Total = total;
        PageIndex = pageIndex;
        PageSize = pageSize;
        Search = search;
    }

    public IEnumerable<T> Registers { get; }
    public int Total { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public string Search { get; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}
