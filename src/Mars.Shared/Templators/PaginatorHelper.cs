namespace Mars.Shared.Templators;

public class PaginatorHelper
{
    public int Page { get; set; }
    public int Total { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public int PageSize { get; set; }

    public Dictionary<int, string> Items { get; set; }
    public string Prev { get; set; }
    public string Next { get; set; }

    public int PagesCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }


    public PaginatorHelper(int page, int total, int pageSize)
    {
        page = Math.Max(1, page);
        Page = page;
        Total = total;
        PageSize = pageSize;

        int elements = 5;
        int pages = total / pageSize;
        int lastPage = (pages * pageSize < total) ? pages + 1 : pages;
        PagesCount = lastPage;

        Start = Math.Max(1, page - elements);
        End = Math.Min(lastPage, page + elements);

        int _prev = page - 1;
        int _next = page + 1;

        Dictionary<int, string> items = new();
        for (int p = Start; p <= End; p++)
        {
            string url = $"page={p}";
            items.Add(p, url);

            if (_prev == p)
            {
                Prev = url;
            }
            if (_next == p)
            {
                Next = url;
            }
        }

        Take = pageSize;
        Skip = (page - 1) * pageSize;

        Items = items;
    }
}
