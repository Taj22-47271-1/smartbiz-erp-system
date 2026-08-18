using System.Security.Claims;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;

namespace SmartBizERP.Api.Middleware;

public class AuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        await next(context);

        if (!WriteMethods.Contains(context.Request.Method)) return;
        if (context.Request.Path.StartsWithSegments("/api/auth")) return;

        Guid? userId = null;
        var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idValue, out var parsed)) userId = parsed;

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserEmail = context.User.FindFirstValue(ClaimTypes.Email),
            Method = context.Request.Method,
            Path = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            IpAddress = context.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync();
    }
}
