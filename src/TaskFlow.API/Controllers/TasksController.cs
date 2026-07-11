using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.API.Contracts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Application.Tasks.Commands.DeleteTask;
using TaskFlow.Application.Tasks.Commands.UpdateTask;
using TaskFlow.Application.Tasks.Queries.GetTaskById;
using TaskFlow.Application.Tasks.Queries.ListTasks;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IValidator<CreateTaskCommand> _createTaskValidator;
    private readonly CreateTaskCommandHandler _createTaskHandler;
    private readonly IValidator<UpdateTaskCommand> _updateValidator;
    private readonly UpdateTaskCommandHandler _updateHandler;
    private readonly DeleteTaskCommandHandler _deleteHandler;
    private readonly IValidator<ListTasksQuery> _listTasksValidator;
    private readonly ListTasksQueryHandler _listTasksHandler;
    private readonly GetTaskByIdQueryHandler _getTaskByIdHandler;
    private readonly ICurrentUserContext _currentUserContext;

    public TasksController(
        IValidator<CreateTaskCommand> createTaskValidator,
        CreateTaskCommandHandler createTaskHandler,
        IValidator<UpdateTaskCommand> updateValidator,
        UpdateTaskCommandHandler updateHandler,
        DeleteTaskCommandHandler deleteHandler,
        IValidator<ListTasksQuery> listTasksValidator,
        ListTasksQueryHandler listTasksHandler,
        GetTaskByIdQueryHandler getTaskByIdHandler,
        ICurrentUserContext currentUserContext)
    {
        _createTaskValidator = createTaskValidator;
        _createTaskHandler = createTaskHandler;
        _updateValidator = updateValidator;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _listTasksValidator = listTasksValidator;
        _listTasksHandler = listTasksHandler;
        _getTaskByIdHandler = getTaskByIdHandler;
        _currentUserContext = currentUserContext;
    }

    // page/perPage are deliberately `string?`, NOT `int?`. With `int?`,
    // non-integer values (e.g. "abc", "1.5") fail ASP.NET Core's automatic
    // model binding BEFORE the FluentValidation validator runs, and the
    // [ApiController] attribute's automatic 400 kicks in with the default
    // ProblemDetails shape ({type,title,status,errors}) instead of the
    // project's standard error shape. Parsing manually here (same pattern
    // as `Guid.TryParse` for `id` elsewhere in this controller) keeps us in
    // control of the response shape for every invalid-input path.
    [HttpGet]
    [ProducesResponseType(typeof(TaskListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] string? page,
        [FromQuery] string? perPage,
        CancellationToken ct)
    {
        int? parsedPage = null;
        int? parsedPerPage = null;

        if (page is not null)
        {
            if (!int.TryParse(page, out var p))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "VALIDATION_ERROR",
                    message = "One or more validation errors occurred.",
                    details = new[] { new { field = "page", issue = "must be a valid integer" } }
                });
            }
            parsedPage = p;
        }

        if (perPage is not null)
        {
            if (!int.TryParse(perPage, out var pp))
            {
                return BadRequest(new
                {
                    status = 400,
                    error = "VALIDATION_ERROR",
                    message = "One or more validation errors occurred.",
                    details = new[] { new { field = "perPage", issue = "must be a valid integer" } }
                });
            }
            parsedPerPage = pp;
        }

        var query = new ListTasksQuery(_currentUserContext.OwnerId, status, parsedPage, parsedPerPage);

        var validationResult = await _listTasksValidator.ValidateAsync(query, ct);
        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // which maps it to the standard error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _listTasksHandler.Handle(query, ct);

        var response = new TaskListResponse(
            result.Items
                .Select(item => new TaskListItemResponse(item.Id, item.Title, item.Status, item.DueDate))
                .ToList(),
            new PagingResponse(
                result.Paging.Page,
                result.Paging.PerPage,
                result.Paging.Total,
                result.Paging.Prev,
                result.Paging.Next));

        return Ok(response);
    }

    // Route param is deliberately `string id`, NOT `Guid id` / `{id:guid}`,
    // matching the Update/Delete actions' pattern — see remarks above Update.
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
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

        var query = new GetTaskByIdQuery(taskId, _currentUserContext.OwnerId);
        var taskDto = await _getTaskByIdHandler.Handle(query, ct);

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
    // TaskNotFoundException thrown by handler is caught by existing
    // TaskNotFoundExceptionHandler middleware -> 404 standard error shape.

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
