namespace OrderService.Application.Wrappers
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int Total { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    }
}
