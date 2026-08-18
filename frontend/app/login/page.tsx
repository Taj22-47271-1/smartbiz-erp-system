"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, getToken } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@smartbiz.local");
  const [password, setPassword] = useState("Admin123!");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (getToken()) router.replace("/dashboard");
  }, [router]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const result = await api<any>("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password })
      });
      localStorage.setItem("smartbiz_token", result.token);
      localStorage.setItem("smartbiz_user", JSON.stringify(result.user));
      router.replace("/dashboard");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-page">
      <section className="login-hero">
        <div className="brand"><div className="brand-mark">S</div><div><strong>SmartBiz</strong><span>ERP Suite</span></div></div>
        <h1>Run the business from one clear workspace.</h1>
        <p>Portfolio ERP demonstrating inventory, purchasing, sales, expense tracking, role-based access and operational reporting.</p>
        <div className="hero-pills">
          <span>Next.js + TypeScript</span><span>ASP.NET Core</span><span>PostgreSQL</span><span>JWT + RBAC</span>
        </div>
      </section>
      <section className="login-panel">
        <form className="login-card" onSubmit={submit}>
          <h2>Welcome back</h2>
          <p>Sign in to the SmartBiz ERP administration portal.</p>
          {error && <div className="error">{error}</div>}
          <div className="field"><label>Email address</label><input value={email} onChange={e => setEmail(e.target.value)} type="email" required /></div>
          <div className="field" style={{marginTop: 14}}><label>Password</label><input value={password} onChange={e => setPassword(e.target.value)} type="password" required /></div>
          <button className="primary-button full" style={{marginTop: 18}} disabled={loading}>{loading ? "Signing in..." : "Sign in"}</button>
          <div className="demo-box"><strong>Demo:</strong><br/>admin@smartbiz.local<br/>Admin123!</div>
        </form>
      </section>
    </div>
  );
}
