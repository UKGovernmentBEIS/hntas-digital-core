namespace HNTAS.Core.Api.Models
{
    public class PagedResult<T>
    {
        // The actual slice of data records for the current page
        public List<T> Items { get; set; } = new();

        // The current page number requested (e.g., 1)
        public int PageNumber { get; set; }

        // Number of records per page (e.g., 10)
        public int PageSize { get; set; }

        // Total number of matching records across all pages in the database
        public int TotalCount { get; set; }

        // Total pages available = Math.Ceiling(TotalCount / PageSize)
        public int TotalPages { get; set; }

        // Handy helper flags for UI consumption
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
