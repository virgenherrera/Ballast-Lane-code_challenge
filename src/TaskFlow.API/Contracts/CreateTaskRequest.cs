namespace TaskFlow.API.Contracts;

/// <summary>
/// Inbound payload for POST /api/tasks. Deliberately has NO Status, Id, or
/// OwnerId properties — those are server-assigned (see AC-004.5, AC-004.10).
/// Any such fields present in the raw JSON body are silently ignored by
/// System.Text.Json's default UnmappedMemberHandling (Skip).
/// </summary>
public sealed record CreateTaskRequest(string Title, string? Description, DateTime? DueDate);
