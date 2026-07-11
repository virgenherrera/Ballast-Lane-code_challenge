using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserContext currentUserContext)
    {
        _taskRepository = taskRepository;
        _currentUserContext = currentUserContext;
    }

    public async System.Threading.Tasks.Task Handle(
        DeleteTaskCommand command,
        CancellationToken ct)
    {
        var deleted = await _taskRepository.DeleteAsync(
            command.Id, _currentUserContext.OwnerId, ct);

        if (!deleted)
        {
            throw new TaskNotFoundException(command.Id);
        }
    }
}
