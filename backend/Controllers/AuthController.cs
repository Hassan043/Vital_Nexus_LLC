using Microsoft.AspNetCore.Mvc;
using NutrientInsight.Api.DTOs;
using NutrientInsight.Api.Services;
using NutrientInsight.Api.Data;
using NutrientInsight.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace NutrientInsight.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService, 
        AppDbContext context, 
        EmailService emailService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, token, error) = await _authService.Register(request.Email, request.Password);
        
        if (!success)
            return BadRequest(new { error });

        return Ok(new { token, email = request.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, token, userId, error) = await _authService.Login(request.Email, request.Password);
        
        if (!success)
            return Unauthorized(new { error });

        return Ok(new { token, userId });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user != null)
        {
            var rawToken = GenerateSecureToken();
            var tokenHash = HashToken(rawToken);

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            try
            {
                await _emailService.SendPasswordResetEmail(user.Email, rawToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
            }
        }

        return Ok(new { message = "If an account exists, we sent a reset link." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var tokenHash = HashToken(request.Token);

        var resetToken = await _context.PasswordResetTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => 
                rt.TokenHash == tokenHash && 
                rt.ExpiresAt > DateTime.UtcNow && 
                rt.UsedAt == null);

        if (resetToken == null)
            return BadRequest(new { error = "Invalid or expired reset token" });

        var user = resetToken.User;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successful for user");

        return Ok(new { message = "Password reset successful" });
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);