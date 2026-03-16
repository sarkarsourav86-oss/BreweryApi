namespace BreweryApi.Models
{
    public sealed class ErrorResponse
    {
        public string Title { get; init; } = string.Empty;
        public int Status { get; init; }
        public string Detail { get; init; } = string.Empty;
        public string TraceId { get; init; } = string.Empty;
    }
}
