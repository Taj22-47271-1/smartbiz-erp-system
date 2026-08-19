using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBizERP.Api.Data;
using SmartBizERP.Api.Domain;
using SmartBizERP.Api.Services;

namespace SmartBizERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/attendance")]
public class AttendanceController(AppDbContext db) : ControllerBase
{
    public record UpdateAttendanceSettingsRequest(
        string WorkStartTime,
        string LateAfterTime,
        string WorkEndTime,
        string AutoCheckoutTime,
        List<string> WorkingDays,
        bool IsAutoCheckoutEnabled);

    [Authorize(Policy = "permission:attendance.checkin")]
    [HttpGet("me")]
    public async Task<IActionResult> MyAttendance()
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync();
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);
        var workingDay = IsWorkingDay(today, settings);

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AttendanceDate == today);

        var canCheckIn = record is null && workingDay && currentTime < settings.WorkEndTime;
        var canCheckOut = record is not null && record.CheckOutAt is null;

        return Ok(new
        {
            serverDate = today.ToString("yyyy-MM-dd"),
            serverTime = localNow.ToString("hh:mm:ss tt"),
            isWorkingDay = workingDay,
            canCheckIn,
            canCheckOut,
            settings = SettingsResponse(settings),
            record = record is null ? null : AttendanceResponse(record, settings)
        });
    }

    [Authorize(Policy = "permission:attendance.checkin")]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn()
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync();
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);
        var currentTime = TimeOnly.FromDateTime(localNow);

        if (!IsWorkingDay(today, settings))
            return BadRequest(new { message = "Today is configured as an office off day." });

        if (currentTime >= settings.WorkEndTime)
            return BadRequest(new { message = "Check-in is closed because office time has ended." });

        if (await db.AttendanceRecords.AnyAsync(x => x.UserId == userId && x.AttendanceDate == today))
            return Conflict(new { message = "You have already checked in today." });

        var status = currentTime > settings.LateAfterTime ? "Late" : "Present";
        var record = new AttendanceRecord
        {
            UserId = userId,
            AttendanceDate = today,
            CheckInAt = DateTime.UtcNow,
            Status = status
        };

        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = status == "Late" ? "Checked in. You are marked late." : "Checked in. You are marked present.",
            record = AttendanceResponse(record, settings)
        });
    }

    [Authorize(Policy = "permission:attendance.checkin")]
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut()
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync();
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AttendanceDate == today);

        if (record is null)
            return BadRequest(new { message = "Check in first before checking out." });

        if (record.CheckOutAt is not null)
            return Conflict(new { message = "You have already checked out today." });

        record.CheckOutAt = DateTime.UtcNow;
        record.CheckOutType = "Manual";
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = "Checked out successfully.",
            record = AttendanceResponse(record, settings)
        });
    }

    [Authorize(Policy = "permission:attendance.checkin")]
    [HttpGet("my-history")]
    public async Task<IActionResult> MyHistory([FromQuery] string? month)
    {
        var userId = CurrentUserId();
        var settings = await GetSettingsAsync();
        var (start, end, monthLabel) = ResolveMonth(month, settings);

        var rows = await db.AttendanceRecords
            .Where(x => x.UserId == userId && x.AttendanceDate >= start && x.AttendanceDate <= end)
            .OrderByDescending(x => x.AttendanceDate)
            .ToListAsync();

        return Ok(new
        {
            month = monthLabel,
            rows = rows.Select(x => AttendanceResponse(x, settings))
        });
    }

    [Authorize(Policy = "permission:attendance.view")]
    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] DateOnly? date)
    {
        var settings = await GetSettingsAsync();
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);
        var selectedDate = date ?? today;
        var users = await db.Users
            .Where(x => x.IsActive)
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        var attendance = await db.AttendanceRecords
            .Where(x => x.AttendanceDate == selectedDate)
            .ToDictionaryAsync(x => x.UserId);

        var rows = users.Select(user =>
        {
            attendance.TryGetValue(user.Id, out var record);
            var derivedStatus = record?.Status ?? DerivedMissingStatus(selectedDate, today, localNow, settings);

            return new
            {
                user.Id,
                user.FullName,
                user.Email,
                role = user.Role.Name,
                date = selectedDate.ToString("yyyy-MM-dd"),
                status = derivedStatus,
                checkInTime = record is null ? null : ToLocal(record.CheckInAt, settings).ToString("hh:mm tt"),
                checkOutTime = record?.CheckOutAt is null ? null : ToLocal(record.CheckOutAt.Value, settings).ToString("hh:mm tt"),
                checkOutType = record?.CheckOutType
            };
        });

        return Ok(new
        {
            date = selectedDate.ToString("yyyy-MM-dd"),
            isWorkingDay = IsWorkingDay(selectedDate, settings),
            rows
        });
    }

    [Authorize(Policy = "permission:attendance.view")]
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? month)
    {
        var settings = await GetSettingsAsync();
        var (monthStart, monthEnd, monthLabel) = ResolveMonth(month, settings);
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var today = DateOnly.FromDateTime(localNow);

        var users = await db.Users
            .Where(x => x.IsActive)
            .Include(x => x.Role)
            .OrderBy(x => x.FullName)
            .ToListAsync();

        var records = await db.AttendanceRecords
            .Where(x => x.AttendanceDate >= monthStart && x.AttendanceDate <= monthEnd)
            .ToListAsync();

        var completedWorkingDays = CompletedWorkingDays(monthStart, monthEnd, today, localNow, settings);

        var rows = users.Select(user =>
        {
            var userRecords = records.Where(x => x.UserId == user.Id).ToList();
            var present = userRecords.Count(x => x.Status == "Present");
            var late = userRecords.Count(x => x.Status == "Late");
            var attended = present + late;
            var absent = Math.Max(0, completedWorkingDays - attended);
            var totalMinutes = userRecords
                .Where(x => x.CheckOutAt is not null)
                .Sum(x => Math.Max(0, (x.CheckOutAt!.Value - x.CheckInAt).TotalMinutes));

            return new
            {
                user.Id,
                user.FullName,
                user.Email,
                role = user.Role.Name,
                attendedDays = attended,
                presentDays = present,
                lateDays = late,
                absentDays = absent,
                totalHours = Math.Round(totalMinutes / 60d, 1)
            };
        }).ToList();

        return Ok(new
        {
            month = monthLabel,
            completedWorkingDays,
            rows
        });
    }

    [Authorize(Policy = "permission:attendance.view")]
    [HttpGet("settings")]
    public async Task<IActionResult> Settings()
    {
        var settings = await GetSettingsAsync();
        return Ok(SettingsResponse(settings));
    }

    [Authorize(Policy = "permission:attendance.manage")]
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(UpdateAttendanceSettingsRequest request)
    {
        if (!TimeOnly.TryParse(request.WorkStartTime, out var workStart) ||
            !TimeOnly.TryParse(request.LateAfterTime, out var lateAfter) ||
            !TimeOnly.TryParse(request.WorkEndTime, out var workEnd) ||
            !TimeOnly.TryParse(request.AutoCheckoutTime, out var autoCheckout))
        {
            return BadRequest(new { message = "Invalid attendance time format." });
        }

        if (lateAfter < workStart)
            return BadRequest(new { message = "Late time cannot be earlier than office start time." });
        if (workEnd <= workStart)
            return BadRequest(new { message = "Office end time must be after office start time." });
        if (autoCheckout < workEnd)
            return BadRequest(new { message = "Auto checkout time cannot be earlier than office end time." });

        var validDays = Enum.GetNames<DayOfWeek>();
        var workingDays = request.WorkingDays
            .Where(x => validDays.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (workingDays.Count == 0)
            return BadRequest(new { message = "Select at least one working day." });

        var settings = await GetSettingsAsync();
        settings.WorkStartTime = workStart;
        settings.LateAfterTime = lateAfter;
        settings.WorkEndTime = workEnd;
        settings.AutoCheckoutTime = autoCheckout;
        settings.WorkingDays = string.Join(',', workingDays);
        settings.IsAutoCheckoutEnabled = request.IsAutoCheckoutEnabled;
        settings.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Attendance settings updated.", settings = SettingsResponse(settings) });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("User identifier is missing from token.");
        return userId;
    }

    private async Task<AttendanceSetting> GetSettingsAsync()
    {
        var settings = await db.AttendanceSettings.FirstOrDefaultAsync();
        if (settings is not null) return settings;

        settings = new AttendanceSetting();
        db.AttendanceSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static object SettingsResponse(AttendanceSetting settings) => new
    {
        workStartTime = settings.WorkStartTime.ToString("HH:mm"),
        lateAfterTime = settings.LateAfterTime.ToString("HH:mm"),
        workEndTime = settings.WorkEndTime.ToString("HH:mm"),
        autoCheckoutTime = settings.AutoCheckoutTime.ToString("HH:mm"),
        settings.TimeZoneId,
        workingDays = ParseWorkingDays(settings),
        settings.IsAutoCheckoutEnabled
    };

    private static object AttendanceResponse(AttendanceRecord record, AttendanceSetting settings) => new
    {
        record.Id,
        date = record.AttendanceDate.ToString("yyyy-MM-dd"),
        record.Status,
        checkInAt = record.CheckInAt,
        checkInTime = ToLocal(record.CheckInAt, settings).ToString("hh:mm tt"),
        checkOutAt = record.CheckOutAt,
        checkOutTime = record.CheckOutAt is null ? null : ToLocal(record.CheckOutAt.Value, settings).ToString("hh:mm tt"),
        record.CheckOutType,
        record.Note
    };

    private static DateTime ToLocal(DateTime utc, AttendanceSetting settings)
    {
        var normalized = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalized, AttendanceClock.ResolveTimeZone(settings.TimeZoneId));
    }

    private static bool IsWorkingDay(DateOnly date, AttendanceSetting settings) =>
        ParseWorkingDays(settings).Contains(date.DayOfWeek.ToString(), StringComparer.OrdinalIgnoreCase);

    private static string[] ParseWorkingDays(AttendanceSetting settings) =>
        settings.WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string DerivedMissingStatus(
        DateOnly selectedDate,
        DateOnly today,
        DateTime localNow,
        AttendanceSetting settings)
    {
        if (!IsWorkingDay(selectedDate, settings)) return "Off Day";
        if (selectedDate > today) return "Upcoming";
        if (selectedDate < today) return "Absent";
        return TimeOnly.FromDateTime(localNow) >= settings.WorkEndTime ? "Absent" : "Not Checked In";
    }

    private static int CompletedWorkingDays(
        DateOnly monthStart,
        DateOnly monthEnd,
        DateOnly today,
        DateTime localNow,
        AttendanceSetting settings)
    {
        if (monthStart > today) return 0;

        var lastDate = monthEnd < today ? monthEnd : today;
        var includeToday = lastDate < today || TimeOnly.FromDateTime(localNow) >= settings.WorkEndTime;
        var count = 0;

        for (var date = monthStart; date <= lastDate; date = date.AddDays(1))
        {
            if (date == today && !includeToday) continue;
            if (IsWorkingDay(date, settings)) count++;
        }

        return count;
    }

    private static (DateOnly Start, DateOnly End, string Label) ResolveMonth(string? month, AttendanceSetting settings)
    {
        var localNow = AttendanceClock.LocalNow(settings.TimeZoneId);
        var year = localNow.Year;
        var monthNumber = localNow.Month;

        if (!string.IsNullOrWhiteSpace(month))
        {
            var parts = month.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var parsedYear) && int.TryParse(parts[1], out var parsedMonth) &&
                parsedMonth is >= 1 and <= 12)
            {
                year = parsedYear;
                monthNumber = parsedMonth;
            }
        }

        var start = new DateOnly(year, monthNumber, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return (start, end, $"{year:D4}-{monthNumber:D2}");
    }
}
