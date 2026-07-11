namespace TaskFlow.API.Contracts;

/// <summary>
/// Paging metadata for <c>GET /api/tasks</c>. <see cref="Prev"/> and
/// <see cref="Next"/> are relative URLs (no scheme/host) preserving any
/// active <c>status</c> filter, or <c>null</c> at the respective boundary.
/// </summary>
public sealed record PagingResponse(
    int Page, int PerPage, int Total, string? Prev, string? Next);
