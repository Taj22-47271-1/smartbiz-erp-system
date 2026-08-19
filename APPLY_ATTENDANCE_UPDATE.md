# SmartBiz ERP Attendance Update — Apply on Windows

This patch is designed to be extracted over your existing `F:\smartbiz-erp` project.

## 1) Back up your project first

```powershell
cd F:\
Copy-Item -Recurse -Force smartbiz-erp smartbiz-erp-backup
```

## 2) Extract the patch

Extract the ZIP and copy the included `backend` and `frontend` folders into:

```text
F:\smartbiz-erp
```

Choose **Replace the files in the destination** when Windows asks.

The patch does NOT contain `backend/appsettings.json`, so your local PostgreSQL password/JWT configuration will not be overwritten.

## 3) Restart backend

```powershell
cd F:\smartbiz-erp\backend
dotnet restore
dotnet run
```

On startup the project automatically creates the new attendance tables if your existing database does not have them yet. Existing ERP data is kept.

## 4) Update/restart frontend

Open a second terminal:

```powershell
cd F:\smartbiz-erp\frontend
npm install
npm run dev
```

## 5) IMPORTANT — sign out and sign in again

The new attendance permissions are added to the Administrator role during backend startup, but your old JWT token does not contain them.

Sign out of SmartBiz and log in again:

```text
admin@smartbiz.local
Admin123!
```

You should now see **Attendance** in the sidebar.

## 6) Attendance permissions

Go to **Roles** and assign these to any role you want:

- `attendance.checkin` — employee can check in/check out and see own history
- `attendance.view` — can see daily employee attendance and monthly summary
- `attendance.manage` — can change office time, late time, working days and auto checkout

After changing a user's role/permissions, that user must sign out and sign in again.

## Default attendance schedule

- Office start: 09:00
- Late after: 09:15
- Office end: 17:00
- Auto checkout: 18:00
- Working days: Sunday–Thursday
- Time zone: Asia/Dhaka

All of these except timezone can be changed from the Attendance page by a role with `attendance.manage`.
