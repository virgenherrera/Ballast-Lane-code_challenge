using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Specifications;
using TaskFlow.Application.Tasks.Dtos;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserContext currentUserContext)
    {
        _taskRepository = taskRepository;
        _currentUserContext = currentUserContext;
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(
        UpdateTaskCommand command,
        CancellationToken ct)
    {
        var existing = await _taskRepository.GetByIdAsync(command.Id, ct);

        var task = TaskOwnershipSpecification.EnsureOwnedBy(
            existing,
            _currentUserContext.OwnerId,
            command.Id);

        if (command.Title is not null)
        {
            task.Rename(command.Title);
        }

        if (command.Description is not null)
        {
            task.UpdateDescription(command.Description);
        }

        if (command.Status is not null)
        {
            var parsedStatus = ParseStatus(command.Status);
            task.ChangeStatus(parsedStatus);
        }

        if (command.DueDate.HasValue)
        {
            task.Reschedule(command.DueDate);
        }

        await _taskRepository.SaveChangesAsync(ct);

        return new TaskDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.DueDate,
            task.OwnerId,
            task.CreatedAt,
            task.UpdatedAt);
    }

    private static DomainTaskStatus ParseStatus(string status) => status switch
    {
        "Pending" => DomainTaskStatus.Pending,
        "In Progress" => DomainTaskStatus.InProgress,
        "Completed" => DomainTaskStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "unsupported status value")
    };
}
