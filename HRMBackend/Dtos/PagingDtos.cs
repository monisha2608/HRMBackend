namespace HRMBackend.Dtos
{
    public class PagedRequest
    {
        public int Page { get; set; } = 1;           // 1-based
        public int PageSize { get; set; } = 10;      // max 100 in controller guard
        public string? Search { get; set; }          // free text (title, dept, location, user)
        public string? Status { get; set; }          // for applications: Applied/Shortlisted/...
    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    }
}
