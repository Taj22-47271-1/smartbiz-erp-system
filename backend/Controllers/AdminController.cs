using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Controllers;

[Authorize(Policy = "permission:users.manage")]
[ApiController]
[Route("api/admin")]
public class AdminController(AppDbContext db) : ControllerBase
{
    public record CreateRoleRequest(string Name, string Description, List<string> PermissionKeys);
    public record CreateUserRequest(string FullName, string Email, string Password, Guid RoleId);

    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions() =>
        Ok(await db.Permissions.OrderBy(x => x.Key).Select(x => new { x.Id, x.Key, x.Description }).ToListAsync());

    [HttpGet("roles")]
    public async Task<IActionResult> Roles()
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(roles.Select(x => new
        {
            x.Id,
            x.Name,
            x.Description,
            Permissions = x.RolePermissions
                .Select(rp => rp.Permission.Key)
                .OrderBy(key => key)
                .ToList()
        }));
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request)
    {
        var name = request.Name.Trim();
        if (await db.Roles.AnyAsync(x => x.Name == name))
            return Conflict(new { message = "Role already exists." });

        var permissions = await db.Permissions
            .Where(x => request.PermissionKeys.Contains(x.Key))
            .ToListAsync();

        var role = new Role
        {
            Name = name,
            Description = request.Description.Trim()
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        db.RolePermissions.AddRange(permissions.Select(p => new RolePermission
        {
            RoleId = role.Id,
            PermissionId = p.Id
        }));

        await db.SaveChangesAsync();
        return Ok(new { role.Id, role.Name, permissions = permissions.Select(x => x.Key) });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users() =>
        Ok(await db.Users
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email,
                x.IsActive,
                Role = x.Role.Name,
                x.RoleId
            })
            .ToListAsync());

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email already exists." });

        if (!await db.Roles.AnyAsync(x => x.Id == request.RoleId))
            return BadRequest(new { message = "Role not found." });

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            RoleId = request.RoleId,
            IsActive = true
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new { user.Id, user.FullName, user.Email, user.RoleId });
    }

    [HttpPatch("users/{id:guid}/active")]
    public async Task<IActionResult> ToggleUser(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.IsActive });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs() =>
        Ok(await db.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync());
}
