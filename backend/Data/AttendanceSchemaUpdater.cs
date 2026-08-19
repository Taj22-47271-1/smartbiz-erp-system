using Microsoft.EntityFrameworkCore;

namespace SmartBizERP.Api.Data;

public static class AttendanceSchemaUpdater
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        const string sql = """
        CREATE TABLE IF NOT EXISTS "AttendanceSettings" (
            "Id" uuid NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            "WorkStartTime" time without time zone NOT NULL,
            "LateAfterTime" time without time zone NOT NULL,
            "WorkEndTime" time without time zone NOT NULL,
            "AutoCheckoutTime" time without time zone NOT NULL,
            "TimeZoneId" text NOT NULL,
            "WorkingDays" text NOT NULL,
            "IsAutoCheckoutEnabled" boolean NOT NULL,
            CONSTRAINT "PK_AttendanceSettings" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS "AttendanceRecords" (
            "Id" uuid NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            "UserId" uuid NOT NULL,
            "AttendanceDate" date NOT NULL,
            "CheckInAt" timestamp with time zone NOT NULL,
            "CheckOutAt" timestamp with time zone NULL,
            "Status" text NOT NULL,
            "CheckOutType" text NULL,
            "Note" text NULL,
            CONSTRAINT "PK_AttendanceRecords" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_AttendanceRecords_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendanceRecords_UserId_AttendanceDate"
            ON "AttendanceRecords" ("UserId", "AttendanceDate");
        """;

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
