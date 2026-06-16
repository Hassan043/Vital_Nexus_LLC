using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Controllers;
using VitalNexus.Application.Auth.RegisterUser;
using VitalNexus.Contracts.Auth;

namespace VitalNexus.UnitTests.Auth;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Register_WithSuccessfulRegistration_ReturnsCreatedResponse()
    {
        var controller = new AuthController(new StubRegisterUserService(RegisterUserResult.Success(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "provider@example.com",
            "Dr. Example",
            "access-token",
            new DateTime(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc))));

        var response = await controller.Register(
            new RegisterUserRequest
            {
                Email = "provider@example.com",
                Password = "Password1!",
                ConfirmPassword = "Password1!",
                DisplayName = "Dr. Example",
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(response);
        var payload = Assert.IsType<RegisterUserResponse>(created.Value);
        Assert.Equal("provider@example.com", payload.Email);
        Assert.Equal("access-token", payload.AccessToken);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var controller = new AuthController(new StubRegisterUserService(
            RegisterUserResult.Failure(
                RegisterUserErrorCode.DuplicateEmail,
                "An account with this email already exists.")));

        var response = await controller.Register(
            new RegisterUserRequest
            {
                Email = "provider@example.com",
                Password = "Password1!",
                ConfirmPassword = "Password1!",
            },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(response);
        Assert.IsType<ProblemDetails>(conflict.Value);
    }

    private sealed class StubRegisterUserService(RegisterUserResult result) : IRegisterUserService
    {
        public Task<RegisterUserResult> RegisterAsync(
            RegisterUserCommand command,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
