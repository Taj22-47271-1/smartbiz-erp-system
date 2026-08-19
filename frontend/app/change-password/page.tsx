"use client";

import { FormEvent, useMemo, useState } from "react";
import AppShell from "@/components/AppShell";
import { api, getStoredUser, logout } from "@/lib/api";

function passwordChecks(password: string) {
  return {
    length: password.length >= 8,
    upper: /[A-Z]/.test(password),
    lower: /[a-z]/.test(password),
    number: /[0-9]/.test(password),
    special: /[^A-Za-z0-9]/.test(password)
  };
}

export default function ChangePasswordPage() {
  const user = getStoredUser();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPasswords, setShowPasswords] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  const checks = useMemo(() => passwordChecks(newPassword), [newPassword]);
  const isStrong = Object.values(checks).every(Boolean);
  const passwordsMatch = newPassword.length > 0 && newPassword === confirmPassword;

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (!isStrong) {
      setError("Please use a stronger password that meets all requirements.");
      return;
    }

    if (!passwordsMatch) {
      setError("New password and confirm password do not match.");
      return;
    }

    setLoading(true);
    try {
      const result = await api<{ message: string }>("/auth/change-password", {
        method: "POST",
        body: JSON.stringify({ currentPassword, newPassword, confirmPassword })
      });

      setSuccess(result.message || "Password changed successfully. Please sign in again.");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");

      window.setTimeout(() => logout(), 1400);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to change password.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <AppShell title="Change Password">
      <div className="security-layout">
        <section className="card security-card">
          <div className="card-head">
            <div>
              <h2>Account security</h2>
              <p>Update the password used to sign in to your SmartBiz ERP account.</p>
            </div>
            <div className="security-lock">⌁</div>
          </div>

          <form className="form security-form" onSubmit={submit}>
            {error && <div className="error">{error}</div>}
            {success && <div className="success">{success}</div>}

            <div className="account-strip">
              <div className="avatar security-avatar">{user?.fullName?.[0] ?? "U"}</div>
              <div>
                <strong>{user?.fullName ?? "Signed-in user"}</strong>
                <span>{user?.email ?? ""}</span>
              </div>
            </div>

            <div className="field">
              <label>Current password</label>
              <input
                type={showPasswords ? "text" : "password"}
                value={currentPassword}
                onChange={event => setCurrentPassword(event.target.value)}
                autoComplete="current-password"
                required
              />
            </div>

            <div className="field">
              <label>New password</label>
              <input
                type={showPasswords ? "text" : "password"}
                value={newPassword}
                onChange={event => setNewPassword(event.target.value)}
                autoComplete="new-password"
                required
              />
            </div>

            <div className="password-rules">
              <span className={checks.length ? "rule-ok" : ""}>8+ characters</span>
              <span className={checks.upper ? "rule-ok" : ""}>Uppercase</span>
              <span className={checks.lower ? "rule-ok" : ""}>Lowercase</span>
              <span className={checks.number ? "rule-ok" : ""}>Number</span>
              <span className={checks.special ? "rule-ok" : ""}>Special character</span>
            </div>

            <div className="field">
              <label>Confirm new password</label>
              <input
                type={showPasswords ? "text" : "password"}
                value={confirmPassword}
                onChange={event => setConfirmPassword(event.target.value)}
                autoComplete="new-password"
                required
              />
              {confirmPassword && (
                <div className={passwordsMatch ? "password-match ok" : "password-match"}>
                  {passwordsMatch ? "Passwords match" : "Passwords do not match"}
                </div>
              )}
            </div>

            <label className="check show-password-check">
              <input
                type="checkbox"
                checked={showPasswords}
                onChange={event => setShowPasswords(event.target.checked)}
              />
              Show passwords
            </label>

            <div className="security-note">
              After changing your password, this browser session will sign out automatically and you will need to sign in with the new password.
            </div>

            <div className="form-actions security-actions">
              <button
                type="submit"
                className="primary-button"
                disabled={loading || !currentPassword || !isStrong || !passwordsMatch}
              >
                {loading ? "Changing password..." : "Change password"}
              </button>
            </div>
          </form>
        </section>
      </div>
    </AppShell>
  );
}
