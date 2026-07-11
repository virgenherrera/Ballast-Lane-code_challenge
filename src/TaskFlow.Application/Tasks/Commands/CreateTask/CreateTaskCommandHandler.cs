using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserContext currentUserContext)
    {
        _taskRepository = taskRepository;
        _currentUserContext = currentUserContext;
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(
        CreateTaskCommand command,
        CancellationToken ct)
    {
        var description = string.IsNullOrEmpty(command.Description)
            ? null
            : command.Description;

        var task = TaskItem.Create(
            command.Title,
            description,
            command.DueDate,
            _currentUserContext.OwnerId);

        await _taskRepository.AddAsync(task, ct);

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
}
