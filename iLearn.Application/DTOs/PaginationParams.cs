namespace iLearn.Application.DTOs
{
    /// <summary>
    /// Standard pagination / filtering parameters for list endpoints.
    /// </summary>
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        /// <summary>1-based page number.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Items per page (capped at 100).</summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>Optional search/filter text.</summary>
        public string? Search { get; set; }

        /// <summary>Optional status filter (e.g. Completed, InProgress, Expired, Upcoming).</summary>
        public string? Status { get; set; }
    }

    /// <summary>
    /// Generic wrapper for paginated API responses.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
