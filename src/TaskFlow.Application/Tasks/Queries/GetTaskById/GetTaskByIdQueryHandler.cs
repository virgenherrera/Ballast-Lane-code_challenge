using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Mapping;
using TaskFlow.Application.Common.Specifications;
using TaskFlow.Application.Tasks.Dtos;

namespace TaskFlow.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// Handles <see cref="GetTaskByIdQuery"/> (US-006). Reuses the same
/// <c>GetByIdAsync</c> + <see cref="TaskOwnershipSpecification.EnsureOwnedBy"/>
/// pattern as <c>UpdateTaskCommandHandler</c> — deliberately NOT a new
/// repository method (TASKFLOW-ANTI-DRIFT). <see cref="TaskOwnershipSpecification.EnsureOwnedBy"/>
/// throws <see cref="Common.Exceptions.TaskNotFoundException"/> for both a
/// missing task and a task owned by another user, giving byte-identical 404
/// responses for both cases (anti-enumeration, AC-006.3).
/// </summary>
public sealed class GetTaskByIdQueryHandler
{
    private readonly ITaskRepository _taskRepository;

    public GetTaskByIdQueryHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery query, CancellationToken ct)
    {
        var existing = await _taskRepository.GetByIdAsync(query.TaskId, ct);

        var task = TaskOwnershipSpecification.EnsureOwnedBy(
            existing, query.OwnerId, query.TaskId);

        return new TaskDto(
            task.Id,
            task.Title,
            task.Description,
            TaskStatusMapper.ToDisplayString(task.Status),
            task.DueDate,
            task.OwnerId,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
