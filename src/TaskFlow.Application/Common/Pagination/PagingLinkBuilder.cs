namespace TaskFlow.Application.Common.Pagination;

/// <summary>
/// Static, pure helper that builds relative prev/next pagination URLs for
/// <c>GET /api/tasks</c>. Canonical query parameter order is alphabetical:
/// <c>page</c>, <c>perPage</c>, <c>status</c> (TASKFLOW-PAGINATION). The
/// active <c>status</c> filter (if any) is preserved on both links so the
/// frontend can follow them without re-deriving the filter.
/// </summary>
public static class PagingLinkBuilder
{
    private const string BasePath = "/api/tasks";

    public static (string? Prev, string? Next) Build(
        int page, int perPage, int total, string? status)
    {
        var lastPage = total == 0 ? 1 : (int)Math.Ceiling(total / (double)perPage);

        string? prev = page > 1
            ? BuildUrl(page - 1, perPage, status)
            : null;

        string? next = page < lastPage
            ? BuildUrl(page + 1, perPage, status)
            : null;

        return (prev, next);
    }

    private static string BuildUrl(int page, int perPage, string? status)
    {
        var query = $"page={page}&perPage={perPage}";

        if (!string.IsNullOrEmpty(status))
        {
            query += $"&status={Uri.EscapeDataString(status)}";
        }

        return $"{BasePath}?{query}";
    }
}
