namespace VitalNexus.Application.Auth.RegisterUser;

public interface IRegisterUserService
{
    Task<RegisterUserResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken);
}
