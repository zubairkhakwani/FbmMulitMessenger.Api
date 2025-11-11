namespace FBMMultiMessenger.Contracts
{
    public class PageableRequest
    {
        public int PageNo { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Keyword { get; set; }
    }

    public class PageableResponse<T>
    {
        public PageableResponse(List<T> records, int pageNumber, int pageSize, int totalCount, int totalPages)
        {
            Records = records;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = totalPages;
            TotalCount = totalCount;
        }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public List<T> Records { get; set; } = new();
    }
}
