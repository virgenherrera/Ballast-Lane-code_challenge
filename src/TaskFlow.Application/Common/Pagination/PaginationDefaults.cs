namespace TaskFlow.Application.Common.Pagination;

/// <summary>
/// Literal constants for list pagination — deliberately NOT configuration
/// (no options binding, no environment override). The API contract does not
/// hardcode these values in its docs; the backend owns them exclusively via
/// this single source of truth (TASKFLOW-PAGINATION).
/// </summary>
public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPerPage = 20;
    public const int MaxPerPage = 100;
}
