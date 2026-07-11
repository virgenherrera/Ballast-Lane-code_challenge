using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.API.Contracts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IValidator<CreateTaskCommand> _createTaskValidator;
    private readonly CreateTaskCommandHandler _createTaskHandler;
    private readonly ITaskRepository _taskRepository;

    public TasksController(
        IValidator<CreateTaskCommand> createTaskValidator,
        CreateTaskCommandHandler createTaskHandler,
        ITaskRepository taskRepository)
    {
        _createTaskValidator = createTaskValidator;
        _createTaskHandler = createTaskHandler;
        _taskRepository = taskRepository;
    }

    // TEMPORARY: returns all tasks with no filtering/sorting/pagination.
    // Superseded by US-005 (list/filter/paginate tasks).
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tasks = await _taskRepository.GetAllAsync(ct);

        var response = tasks.Select(task => new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.DueDate,
            task.OwnerId,
            task.CreatedAt,
            task.UpdatedAt));

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        // Request -> Command. Status/Id/OwnerId are never accepted from the
        // client (AC-004.5, AC-004.10) — CreateTaskRequest has no such
        // properties, so there is nothing to map even if the raw JSON body
        // contains them.
        var command = new CreateTaskCommand(request.Title, request.Description, request.DueDate);

        var validationResult = await _createTaskValidator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // which maps it to the standard error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var taskDto = await _createTaskHandler.Handle(command, ct);

        var response = new TaskResponse(
            taskDto.Id,
            taskDto.Title,
            taskDto.Description,
            taskDto.Status,
            taskDto.DueDate,
            taskDto.OwnerId,
            taskDto.CreatedAt,
            taskDto.UpdatedAt);

        return CreatedAtAction(nameof(Create), new { id = response.Id }, response);
    }
}
