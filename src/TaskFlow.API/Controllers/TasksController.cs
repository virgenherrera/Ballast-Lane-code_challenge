using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.API.Contracts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Application.Tasks.Commands.DeleteTask;
using TaskFlow.Application.Tasks.Commands.UpdateTask;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IValidator<CreateTaskCommand> _createTaskValidator;
    private readonly CreateTaskCommandHandler _createTaskHandler;
    private readonly ITaskRepository _taskRepository;
    private readonly IValidator<UpdateTaskCommand> _updateValidator;
    private readonly UpdateTaskCommandHandler _updateHandler;
    private readonly DeleteTaskCommandHandler _deleteHandler;

    public TasksController(
        IValidator<CreateTaskCommand> createTaskValidator,
        CreateTaskCommandHandler createTaskHandler,
        ITaskRepository taskRepository,
        IValidator<UpdateTaskCommand> updateValidator,
        UpdateTaskCommandHandler updateHandler,
        DeleteTaskCommandHandler deleteHandler)
    {
        _createTaskValidator = createTaskValidator;
        _createTaskHandler = createTaskHandler;
        _taskRepository = taskRepository;
        _updateValidator = updateValidator;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
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

    // Route param is deliberately `string id`, NOT `Guid id` / `{id:guid}`.
    // A failed {id:guid} route constraint yields 404 by default, which
    // contradicts AC-007.8 (malformed GUID must return 400). Parsing is
    // done manually below so we control the response shape.
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest(new
            {
                status = 400,
                error = "VALIDATION_ERROR",
                message = "Task id is not a valid GUID.",
                details = new[] { new { field = "id", issue = "must be a valid UUID/GUID" } }
            });
        }

        var command = new UpdateTaskCommand(
            taskId, request.Title, request.Description, request.Status, request.DueDate);

        var validationResult = await _updateValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // which maps it to the standard error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var taskDto = await _updateHandler.Handle(command, ct);

        var response = new TaskResponse(
            taskDto.Id,
            taskDto.Title,
            taskDto.Description,
            taskDto.Status,
            taskDto.DueDate,
            taskDto.OwnerId,
            taskDto.CreatedAt,
            taskDto.UpdatedAt);

        return Ok(response);
    }

    // Route param is deliberately `string id`, NOT `Guid id` / `{id:guid}`,
    // matching the Update action's pattern for consistency — see remarks
    // above Update. Malformed-GUID -> 400 is not an AC for this story, but
    // the pattern is kept identical rather than mixing conventions.
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest(new
            {
                status = 400,
                error = "VALIDATION_ERROR",
                message = "Task id is not a valid GUID.",
                details = new[] { new { field = "id", issue = "must be a valid UUID/GUID" } }
            });
        }

        var command = new DeleteTaskCommand(taskId);
        await _deleteHandler.Handle(command, ct);

        return NoContent();
    }
    // TaskNotFoundException thrown by handler is caught by existing
    // TaskNotFoundExceptionHandler middleware -> 404 standard error shape.
}
