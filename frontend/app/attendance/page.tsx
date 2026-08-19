"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import AppShell from "@/components/AppShell";
import { api, getStoredUser } from "@/lib/api";

type AttendanceSettings = {
  workStartTime: string;
  lateAfterTime: string;
  workEndTime: string;
  autoCheckoutTime: string;
  timeZoneId: string;
  workingDays: string[];
  isAutoCheckoutEnabled: boolean;
};

type AttendanceRecord = {
  id: string;
  date: string;
  status: string;
  checkInTime: string;
  checkOutTime: string | null;
  checkOutType: string | null;
};

type MyAttendance = {
  serverDate: string;
  serverTime: string;
  isWorkingDay: boolean;
  canCheckIn: boolean;
  canCheckOut: boolean;
  settings: AttendanceSettings;
  record: AttendanceRecord | null;
};

type DailyRow = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  date: string;
  status: string;
  checkInTime: string | null;
  checkOutTime: string | null;
  checkOutType: string | null;
};

type DailyResponse = {
  date: string;
  isWorkingDay: boolean;
  rows: DailyRow[];
};

type SummaryRow = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  attendedDays: number;
  presentDays: number;
  lateDays: number;
  absentDays: number;
  totalHours: number;
};

type SummaryResponse = {
  month: string;
  completedWorkingDays: number;
  rows: SummaryRow[];
};

type HistoryResponse = {
  month: string;
  rows: AttendanceRecord[];
};

const dayOptions = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

function statusClass(status: string) {
  if (status === "Late") return "badge warn";
  if (status === "Absent") return "badge danger";
  if (status === "Present") return "badge";
  return "badge muted";
}

function currentMonth() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
}

function currentDate() {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
}

export default function AttendancePage() {
  const [user, setUser] = useState<any>(null);
  const [myAttendance, setMyAttendance] = useState<MyAttendance | null>(null);
  const [history, setHistory] = useState<AttendanceRecord[]>([]);
  const [daily, setDaily] = useState<DailyResponse | null>(null);
  const [summary, setSummary] = useState<SummaryResponse | null>(null);
  const [settings, setSettings] = useState<AttendanceSettings | null>(null);
  const [selectedDate, setSelectedDate] = useState(currentDate());
  const [selectedMonth, setSelectedMonth] = useState(currentMonth());
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setUser(getStoredUser());
  }, []);

  const permissions: string[] = useMemo(() => user?.permissions ?? [], [user]);
  const canCheckIn = permissions.includes("attendance.checkin");
  const canView = permissions.includes("attendance.view");
  const canManage = permissions.includes("attendance.manage");

  async function loadSelf() {
    if (!canCheckIn) return;
    const [today, monthHistory] = await Promise.all([
      api<MyAttendance>("/attendance/me"),
      api<HistoryResponse>(`/attendance/my-history?month=${selectedMonth}`)
    ]);
    setMyAttendance(today);
    setHistory(monthHistory.rows);
    if (today.serverDate) setSelectedDate(today.serverDate);
  }

  async function loadAdmin() {
    if (!canView) return;
    const [dailyRows, summaryRows, currentSettings] = await Promise.all([
      api<DailyResponse>(`/attendance/daily?date=${selectedDate}`),
      api<SummaryResponse>(`/attendance/summary?month=${selectedMonth}`),
      api<AttendanceSettings>("/attendance/settings")
    ]);
    setDaily(dailyRows);
    setSummary(summaryRows);
    setSettings(currentSettings);
  }

  useEffect(() => {
    if (!user) return;
    setError("");
    Promise.all([loadSelf(), loadAdmin()]).catch(e => setError(e instanceof Error ? e.message : "Failed to load attendance."));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  useEffect(() => {
    if (!user || !canView) return;
    api<DailyResponse>(`/attendance/daily?date=${selectedDate}`)
      .then(setDaily)
      .catch(e => setError(e instanceof Error ? e.message : "Failed to load daily attendance."));
  }, [selectedDate, user, canView]);

  useEffect(() => {
    if (!user) return;
    const requests: Promise<unknown>[] = [];
    if (canView) requests.push(api<SummaryResponse>(`/attendance/summary?month=${selectedMonth}`).then(setSummary));
    if (canCheckIn) requests.push(api<HistoryResponse>(`/attendance/my-history?month=${selectedMonth}`).then(x => setHistory(x.rows)));
    Promise.all(requests).catch(e => setError(e instanceof Error ? e.message : "Failed to load monthly attendance."));
  }, [selectedMonth, user, canView, canCheckIn]);

  async function attendanceAction(type: "check-in" | "check-out") {
    setBusy(true);
    setError("");
    setSuccess("");
    try {
      const result = await api<{ message: string }>(`/attendance/${type}`, { method: "POST" });
      setSuccess(result.message);
      await Promise.all([loadSelf(), loadAdmin()]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Attendance action failed.");
    } finally {
      setBusy(false);
    }
  }

  function toggleWorkingDay(day: string) {
    if (!settings) return;
    const exists = settings.workingDays.includes(day);
    setSettings({
      ...settings,
      workingDays: exists ? settings.workingDays.filter(x => x !== day) : [...settings.workingDays, day]
    });
  }

  async function saveSettings(e: FormEvent) {
    e.preventDefault();
    if (!settings) return;
    setBusy(true);
    setError("");
    setSuccess("");
    try {
      const result = await api<{ message: string; settings: AttendanceSettings }>("/attendance/settings", {
        method: "PUT",
        body: JSON.stringify({
          workStartTime: settings.workStartTime,
          lateAfterTime: settings.lateAfterTime,
          workEndTime: settings.workEndTime,
          autoCheckoutTime: settings.autoCheckoutTime,
          workingDays: settings.workingDays,
          isAutoCheckoutEnabled: settings.isAutoCheckoutEnabled
        })
      });
      setSettings(result.settings);
      setSuccess(result.message);
      await Promise.all([loadSelf(), loadAdmin()]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not save attendance settings.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell title="Attendance Management">
      {error && <div className="error">{error}</div>}
      {success && <div className="success">{success}</div>}

      {canCheckIn && myAttendance && (
        <section className="attendance-hero card">
          <div>
            <p className="eyebrow">My attendance today</p>
            <div className="attendance-status-row">
              <h2>{myAttendance.record ? myAttendance.record.status : myAttendance.isWorkingDay ? "Not Checked In" : "Off Day"}</h2>
              <span className={statusClass(myAttendance.record?.status ?? (myAttendance.isWorkingDay ? "Not Checked In" : "Off Day"))}>
                {myAttendance.record?.status ?? (myAttendance.isWorkingDay ? "Waiting" : "Off Day")}
              </span>
            </div>
            <div className="attendance-meta">
              <span><strong>Office:</strong> {myAttendance.settings.workStartTime} - {myAttendance.settings.workEndTime}</span>
              <span><strong>Late after:</strong> {myAttendance.settings.lateAfterTime}</span>
              <span><strong>Auto checkout:</strong> {myAttendance.settings.autoCheckoutTime}</span>
              <span><strong>Office time:</strong> {myAttendance.serverTime}</span>
            </div>
            {myAttendance.record && (
              <div className="attendance-meta secondary-meta">
                <span><strong>Check in:</strong> {myAttendance.record.checkInTime}</span>
                <span><strong>Check out:</strong> {myAttendance.record.checkOutTime ?? "Running"}</span>
                {myAttendance.record.checkOutType && <span><strong>Checkout type:</strong> {myAttendance.record.checkOutType}</span>}
              </div>
            )}
          </div>
          <div className="attendance-actions">
            {myAttendance.canCheckIn && (
              <button className="primary-button attendance-main-button" disabled={busy} onClick={() => attendanceAction("check-in")}>Check In</button>
            )}
            {myAttendance.canCheckOut && (
              <button className="primary-button attendance-main-button" disabled={busy} onClick={() => attendanceAction("check-out")}>Check Out</button>
            )}
            {!myAttendance.canCheckIn && !myAttendance.canCheckOut && (
              <span className="badge muted">Attendance action closed</span>
            )}
          </div>
        </section>
      )}

      {canCheckIn && (
        <section className="card table-wrap attendance-section">
          <div className="card-head">
            <div><h2>My monthly history</h2><p>Your check-in and check-out records.</p></div>
            <input className="date-control" type="month" value={selectedMonth} onChange={e => setSelectedMonth(e.target.value)} />
          </div>
          <div className="card-body compact-card-body">
            <table>
              <thead><tr><th>Date</th><th>Status</th><th>Check in</th><th>Check out</th><th>Checkout type</th></tr></thead>
              <tbody>
                {history.map(row => (
                  <tr key={row.id}>
                    <td>{row.date}</td>
                    <td><span className={statusClass(row.status)}>{row.status}</span></td>
                    <td>{row.checkInTime}</td>
                    <td>{row.checkOutTime ?? "Running"}</td>
                    <td>{row.checkOutType ?? "-"}</td>
                  </tr>
                ))}
                {history.length === 0 && <tr><td colSpan={5} className="empty">No attendance records for this month.</td></tr>}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {canView && (
        <>
          <div className="section-title-row">
            <div><p className="eyebrow">Admin / authorized role</p><h2>Employee attendance overview</h2></div>
          </div>

          <section className="grid-kpi attendance-kpis">
            <div className="card kpi"><div className="kpi-head"><span>Employees</span><span className="kpi-icon">♙</span></div><div className="kpi-value">{summary?.rows.length ?? 0}</div><div className="kpi-note">Active system users</div></div>
            <div className="card kpi"><div className="kpi-head"><span>Working days</span><span className="kpi-icon">◫</span></div><div className="kpi-value">{summary?.completedWorkingDays ?? 0}</div><div className="kpi-note">Completed days this month</div></div>
            <div className="card kpi"><div className="kpi-head"><span>Late arrivals</span><span className="kpi-icon">◷</span></div><div className="kpi-value">{summary?.rows.reduce((sum, row) => sum + row.lateDays, 0) ?? 0}</div><div className="kpi-note">Across all employees</div></div>
            <div className="card kpi"><div className="kpi-head"><span>Absences</span><span className="kpi-icon">!</span></div><div className="kpi-value">{summary?.rows.reduce((sum, row) => sum + row.absentDays, 0) ?? 0}</div><div className="kpi-note">Calculated from working days</div></div>
          </section>

          {canManage && settings && (
            <section className="card attendance-section">
              <div className="card-head"><div><h2>Attendance settings</h2><p>Set office hours, late threshold, working days and automatic checkout.</p></div></div>
              <form className="form" onSubmit={saveSettings}>
                <div className="form-grid attendance-time-grid">
                  <div className="field"><label>Office start</label><input type="time" required value={settings.workStartTime} onChange={e => setSettings({ ...settings, workStartTime: e.target.value })} /></div>
                  <div className="field"><label>Late after</label><input type="time" required value={settings.lateAfterTime} onChange={e => setSettings({ ...settings, lateAfterTime: e.target.value })} /></div>
                  <div className="field"><label>Office end</label><input type="time" required value={settings.workEndTime} onChange={e => setSettings({ ...settings, workEndTime: e.target.value })} /></div>
                  <div className="field"><label>Auto checkout</label><input type="time" required value={settings.autoCheckoutTime} onChange={e => setSettings({ ...settings, autoCheckoutTime: e.target.value })} /></div>
                </div>
                <div className="field">
                  <label>Working days</label>
                  <div className="working-days">
                    {dayOptions.map(day => (
                      <label className="check day-check" key={day}>
                        <input type="checkbox" checked={settings.workingDays.includes(day)} onChange={() => toggleWorkingDay(day)} />
                        <span>{day.slice(0, 3)}</span>
                      </label>
                    ))}
                  </div>
                </div>
                <label className="check auto-checkout-check">
                  <input type="checkbox" checked={settings.isAutoCheckoutEnabled} onChange={e => setSettings({ ...settings, isAutoCheckoutEnabled: e.target.checked })} />
                  <span><strong>Automatic checkout enabled</strong><br /><span style={{ color: "var(--muted)" }}>Open attendance will be closed automatically at the configured time.</span></span>
                </label>
                <div className="form-actions"><button className="primary-button" disabled={busy}>Save attendance settings</button></div>
              </form>
            </section>
          )}

          <section className="card table-wrap attendance-section">
            <div className="card-head">
              <div><h2>Daily attendance</h2><p>Present, late, absent and checkout status for every employee.</p></div>
              <input className="date-control" type="date" value={selectedDate} onChange={e => setSelectedDate(e.target.value)} />
            </div>
            <div className="card-body compact-card-body">
              <table>
                <thead><tr><th>Employee</th><th>Role</th><th>Status</th><th>Check in</th><th>Check out</th><th>Type</th></tr></thead>
                <tbody>
                  {daily?.rows.map(row => (
                    <tr key={row.id}>
                      <td><strong>{row.fullName}</strong><br /><span className="table-subtext">{row.email}</span></td>
                      <td>{row.role}</td>
                      <td><span className={statusClass(row.status)}>{row.status}</span></td>
                      <td>{row.checkInTime ?? "-"}</td>
                      <td>{row.checkOutTime ?? (row.checkInTime ? "Running" : "-")}</td>
                      <td>{row.checkOutType ?? "-"}</td>
                    </tr>
                  ))}
                  {(!daily || daily.rows.length === 0) && <tr><td colSpan={6} className="empty">No employees found.</td></tr>}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card table-wrap attendance-section">
            <div className="card-head">
              <div><h2>Monthly employee summary</h2><p>See exactly how many days each employee attended, was late or absent.</p></div>
              <input className="date-control" type="month" value={selectedMonth} onChange={e => setSelectedMonth(e.target.value)} />
            </div>
            <div className="card-body compact-card-body">
              <table>
                <thead><tr><th>Employee</th><th>Role</th><th>Attended</th><th>On time</th><th>Late</th><th>Absent</th><th>Total hours</th></tr></thead>
                <tbody>
                  {summary?.rows.map(row => (
                    <tr key={row.id}>
                      <td><strong>{row.fullName}</strong><br /><span className="table-subtext">{row.email}</span></td>
                      <td>{row.role}</td>
                      <td><strong>{row.attendedDays}</strong></td>
                      <td><span className="badge">{row.presentDays}</span></td>
                      <td><span className="badge warn">{row.lateDays}</span></td>
                      <td><span className="badge danger">{row.absentDays}</span></td>
                      <td>{row.totalHours}h</td>
                    </tr>
                  ))}
                  {(!summary || summary.rows.length === 0) && <tr><td colSpan={7} className="empty">No employee summary available.</td></tr>}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}

      {!canCheckIn && !canView && (
        <div className="error">Your role does not have attendance access. Ask an administrator to assign an attendance permission.</div>
      )}
    </AppShell>
  );
}
