"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ReactNode, useEffect, useState } from "react";
import { getStoredUser, getToken, logout } from "@/lib/api";

type NavItem = {
  href: string;
  label: string;
  icon: string;
  permissions: string[];
};

const nav: NavItem[] = [
  { href: "/dashboard", label: "Dashboard", icon: "◫", permissions: ["dashboard.view"] },
  { href: "/attendance", label: "Attendance", icon: "◷", permissions: ["attendance.checkin", "attendance.view"] },
  { href: "/products", label: "Products", icon: "▦", permissions: ["products.manage"] },
  { href: "/customers", label: "Customers", icon: "◎", permissions: ["customers.manage"] },
  { href: "/suppliers", label: "Suppliers", icon: "◇", permissions: ["suppliers.manage"] },
  { href: "/purchases", label: "Purchases", icon: "↓", permissions: ["purchases.manage"] },
  { href: "/sales", label: "Sales", icon: "↑", permissions: ["sales.manage"] },
  { href: "/expenses", label: "Expenses", icon: "৳", permissions: ["expenses.manage"] },
  { href: "/users", label: "Users", icon: "♙", permissions: ["users.manage"] },
  { href: "/roles", label: "Roles", icon: "⚿", permissions: ["users.manage"] }
];

export default function AppShell({ children, title }: { children: ReactNode; title: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const [user, setUser] = useState<any>(null);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    setUser(getStoredUser());
  }, [router]);

  const canSee = (item: NavItem) =>
    !user?.permissions || item.permissions.some(permission => user.permissions.includes(permission));

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">S</div>
          <div><strong>SmartBiz</strong><span>ERP Suite</span></div>
        </div>
        <nav>
          {nav.filter(canSee).map(item => (
            <Link
              key={item.href}
              href={item.href}
              className={pathname === item.href ? "nav-link active" : "nav-link"}
            >
              <span className="nav-icon">{item.icon}</span>{item.label}
            </Link>
          ))}
        </nav>
        <div className="sidebar-foot">
          <div className="user-mini">
            <div className="avatar">{user?.fullName?.[0] ?? "A"}</div>
            <div><strong>{user?.fullName ?? "Administrator"}</strong><span>{user?.role ?? "User"}</span></div>
          </div>
          <button className="ghost-button full" onClick={logout}>Sign out</button>
        </div>
      </aside>
      <main className="main">
        <header className="topbar">
          <div>
            <p className="eyebrow">Business management</p>
            <h1>{title}</h1>
          </div>
          <div className="topbar-actions">
            <span className="status-dot"></span>
            <span>System online</span>
          </div>
        </header>
        <div className="page-content">{children}</div>
      </main>
    </div>
  );
}
