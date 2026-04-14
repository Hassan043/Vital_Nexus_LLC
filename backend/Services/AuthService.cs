using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NutrientInsight.Api.Data;
using NutrientInsight.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace NutrientInsight.Api.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool success, string? token, string? error)> Register(string email, string password)
    {
        // Validate password requirements
        var passwordValidation = ValidatePassword(password);
        if (!passwordValidation.isValid)
        {
            return (false, null, passwordValidation.error);
        }

        // Check if user exists (case-insensitive email)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (existingUser != null)
            return (false, null, "Email already registered");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email, // Store as entered (preserve case)
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return (true, token, null);
    }

    public async Task<(bool success, string? token, Guid? userId, string? error)> Login(string email, string password)
    {
        // Case-insensitive email lookup
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
            return (false, null, null, "Invalid email or password");

        // Password is case-sensitive
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, null, null, "Invalid email or password");

        var token = GenerateJwtToken(user);
        return (true, token, user.Id, null);
    }

    private (bool isValid, string? error) ValidatePassword(string password)
    {
        if (password.Length < 8)
            return (false, "Password must be at least 8 characters long");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return (false, "Password must contain at least one uppercase letter");

        if (!Regex.IsMatch(password, @"[a-z]"))
            return (false, "Password must contain at least one lowercase letter");

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
            return (false, "Password must contain at least one special character");

        return (true, null);
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopmentOnly123456"));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}