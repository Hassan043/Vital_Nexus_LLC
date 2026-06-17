using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Application.Auth.RegisterUser;
using VitalNexus.Contracts.Auth;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterUserService _registerUserService;

    public AuthController(IRegisterUserService registerUserService)
    {
        _registerUserService = registerUserService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _registerUserService.RegisterAsync(
            new RegisterUserCommand(
                request.Email,
                request.Password,
                request.ConfirmPassword,
                request.DisplayName),
            cancellationToken);

        if (result.Succeeded)
        {
            return Created(
                $"/api/auth/users/{result.UserId}",
                new RegisterUserResponse
                {
                    UserId = result.UserId,
                    Email = result.Email,
                    DisplayName = result.DisplayName,
                    AccessToken = result.AccessToken!,
                    ExpiresAtUtc = result.ExpiresAtUtc!.Value,
                });
        }

        return result.ErrorCode switch
        {
            RegisterUserErrorCode.DuplicateEmail => Conflict(new ProblemDetails
            {
                Title = "Registration failed",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status409Conflict,
            }),
            _ => BadRequest(new ProblemDetails
            {
                Title = "Registration failed",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status400BadRequest,
            }),
        };
    }
}
