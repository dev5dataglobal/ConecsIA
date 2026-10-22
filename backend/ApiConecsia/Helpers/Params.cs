namespace ApiConecsia.Helpers;

public sealed class Params
{
    private const int MaxPageSize = 20;
    private int _pageSize = 5;
    private int _pageIndex = 1;
    private string _search = string.Empty;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? 5 : Math.Min(value, MaxPageSize);
    }

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value <= 0 ? 1 : value;
    }

    public string Search
    {
        get => _search;
        set => _search = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
