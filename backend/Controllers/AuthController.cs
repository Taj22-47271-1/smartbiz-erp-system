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

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users
            .Include(x => x.Role)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive);

        if (user is null) return Unauthorized(new { message = "Invalid email or password." });

        var result = new PasswordHasher<User>().VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

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
}
