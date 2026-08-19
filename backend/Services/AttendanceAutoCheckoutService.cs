using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;

namespace SmartBizERP.Api.Services;

public class AttendanceAutoCheckoutService(
    IServiceScopeFactory scopeFactory,
    ILogger<AttendanceAutoCheckoutService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOpenAttendanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attendance auto checkout failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessOpenAttendanceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.AttendanceSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsAutoCheckoutEnabled) return;

        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);

        var openRecords = await db.AttendanceRecords
            .Where(x => x.CheckOutAt == null && x.AttendanceDate <= today)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var record in openRecords)
        {
            var shouldClose = record.AttendanceDate < today ||
                              (record.AttendanceDate == today && currentTime >= settings.AutoCheckoutTime);
            if (!shouldClose) continue;

            record.CheckOutAt = AttendanceClock.LocalToUtc(
                record.AttendanceDate,
                settings.AutoCheckoutTime,
                settings.TimeZoneId);
            record.CheckOutType = "Auto";
            record.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed) await db.SaveChangesAsync(cancellationToken);
    }
}
