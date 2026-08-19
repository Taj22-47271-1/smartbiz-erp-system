using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;
using SmartBizERP.Api.Services;

namespace SmartBizERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtService jwtService) : ControllerBase
{
    public record LoginRequest(string Email, string Password);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users
            .Include(x => x.Role)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);

        if (user is null) return Unauthorized(new { message = "Invalid email or password." });

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var permissions = user.Role.RolePermissions.Select(x => x.Permission.Key).ToArray();
        var token = jwtService.CreateToken(user, permissions);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.FullName,
                user.Email,
                role = user.Role.Name,
                permissions
            }
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized(new { message = "Invalid authentication session." });

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user is null)
            return Unauthorized(new { message = "User account was not found or is inactive." });

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            return BadRequest(new { message = "Current password is required." });

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "New password and confirm password do not match." });

        var passwordError = ValidateNewPassword(request.NewPassword);
        if (passwordError is not null)
            return BadRequest(new { message = passwordError });

        var hasher = new PasswordHasher<User>();
        var currentPasswordResult = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.CurrentPassword);

        if (currentPasswordResult == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Current password is incorrect." });

        var samePasswordResult = hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.NewPassword);

        if (samePasswordResult != PasswordVerificationResult.Failed)
            return BadRequest(new { message = "New password must be different from the current password." });

        user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully. Please sign in again." });
    }

    private static string? ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return "New password must be at least 8 characters long.";

        if (!password.Any(char.IsUpper))
            return "New password must contain at least one uppercase letter.";

        if (!password.Any(char.IsLower))
            return "New password must contain at least one lowercase letter.";

        if (!password.Any(char.IsDigit))
            return "New password must contain at least one number.";

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "New password must contain at least one special character.";

        return null;
    }
}
