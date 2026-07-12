using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.API.Contracts;
using TaskFlow.API.Contracts.Auth;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.UseCases.AuthenticateUser;
using TaskFlow.Application.UseCases.RegisterUser;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IValidator<RegisterUserCommand> _registerValidator;
    private readonly RegisterUserHandler _registerHandler;
    private readonly IValidator<AuthenticateUserCommand> _loginValidator;
    private readonly AuthenticateUserHandler _loginHandler;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;

    public AuthController(
        IValidator<RegisterUserCommand> registerValidator,
        RegisterUserHandler registerHandler,
        IValidator<AuthenticateUserCommand> loginValidator,
        AuthenticateUserHandler loginHandler,
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository)
    {
        _registerValidator = registerValidator;
        _registerHandler = registerHandler;
        _loginValidator = loginValidator;
        _loginHandler = loginHandler;
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Email, request.Name, request.Password);

        var validationResult = await _registerValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // which maps it to the standard 400 error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _registerHandler.Handle(command, ct);
        // DuplicateEmailException thrown by handler is caught by
        // DuplicateEmailExceptionHandler middleware -> 409 standard error shape.

        var response = new RegisterResponse(result.Id, result.Email, result.Name, result.CreatedAt);

        return StatusCode(StatusCodes.Status201Created, response);
        // No CreatedAtAction/Location header — no GET /api/auth/users/{id} endpoint exists.
    }

    // Public endpoint — deliberately NO [Authorize]. Rate-limited via the "login"
    // fixed-window policy (5 requests/min/IP) registered in Program.cs.
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new AuthenticateUserCommand(request.Email, request.Password);

        var validationResult = await _loginValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // which maps it to the standard 400 error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _loginHandler.Handle(command, ct);
        // InvalidCredentialsException thrown by handler is caught by
        // InvalidCredentialsExceptionHandler middleware -> 401 standard error shape.

        var response = new LoginResponse(
            result.AccessToken,
            result.TokenType,
            result.ExpiresIn,
            new LoginUserSummary(result.User.Id, result.User.Email, result.User.Name));

        return Ok(response);
    }

    // Protected endpoint — [Authorize] applies ONLY to this action, not the
    // controller. Register/Login MUST remain public. Identifies the caller
    // via ICurrentUserContext.OwnerId (sourced from the JWT "sub" claim).
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = _currentUserContext.OwnerId;
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null)
        {
            // Should not happen if the JWT "sub" claim references a valid
            // user, but handled defensively.
            return NotFound();
        }

        var response = new MeResponse(user.Id, user.Email.Value, user.Name, user.CreatedAt);

        return Ok(response);
    }
}
