"use client";

import { useEffect, useState } from "react";
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import AppShell from "@/components/AppShell";
import { api } from "@/lib/api";

type Dashboard = {
  kpis: { totalSales:number; totalPurchases:number; totalExpenses:number; profit:number; totalProducts:number; totalCustomers:number };
  trend: { month:string; sales:number; purchases:number }[];
  lowStock: { id:string; name:string; sku:string; currentStock:number; reorderLevel:number }[];
  recentSales: { id:string; invoiceNo:string; customer:string; totalAmount:number; createdAt:string }[];
};

const money = (value:number) => new Intl.NumberFormat("en-BD", { style:"currency", currency:"BDT", maximumFractionDigits:0 }).format(value);

export default function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api<Dashboard>("/dashboard").then(setData).catch(e => setError(e.message));
  }, []);

  return (
    <AppShell title="Dashboard">
      {error && <div className="error">{error}</div>}
      <div className="grid-kpi">
        <Kpi label="Total sales" value={money(data?.kpis.totalSales ?? 0)} icon="↑" note="Last 6 months" />
        <Kpi label="Purchases" value={money(data?.kpis.totalPurchases ?? 0)} icon="↓" note="Inventory acquisition" />
        <Kpi label="Expenses" value={money(data?.kpis.totalExpenses ?? 0)} icon="৳" note="Operating expenses" />
        <Kpi label="Net profit" value={money(data?.kpis.profit ?? 0)} icon="↗" note="Sales - COGS - expenses" />
      </div>

      <div className="two-col">
        <section className="card">
          <div className="card-head"><div><h2>Sales vs purchases</h2><p>Six-month operating trend</p></div></div>
          <div className="card-body" style={{height:310}}>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={data?.trend ?? []}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="month" tick={{fontSize:11}} />
                <YAxis tick={{fontSize:11}} />
                <Tooltip formatter={(v:any) => money(Number(v))} />
                <Line type="monotone" dataKey="sales" strokeWidth={3} dot={false} />
                <Line type="monotone" dataKey="purchases" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </section>

        <section className="card">
          <div className="card-head"><div><h2>Low stock</h2><p>Needs attention</p></div></div>
          <div className="card-body">
            {(data?.lowStock ?? []).length === 0 ? <div className="empty">Stock levels are healthy.</div> :
              data!.lowStock.map(p => (
                <div key={p.id} style={{display:"flex",justifyContent:"space-between",padding:"11px 0",borderBottom:"1px solid var(--line)"}}>
                  <div><strong style={{fontSize:13}}>{p.name}</strong><div className="mono" style={{color:"var(--muted)",marginTop:3}}>{p.sku}</div></div>
                  <span className="badge warn">{p.currentStock} left</span>
                </div>
              ))
            }
          </div>
        </section>
      </div>

      <section className="card">
        <div className="card-head"><div><h2>Recent sales</h2><p>Latest customer invoices</p></div></div>
        <div className="card-body table-wrap">
          <table><thead><tr><th>Invoice</th><th>Customer</th><th>Date</th><th>Amount</th></tr></thead>
            <tbody>{(data?.recentSales ?? []).map(s => <tr key={s.id}><td className="mono">{s.invoiceNo}</td><td>{s.customer}</td><td>{new Date(s.createdAt).toLocaleDateString()}</td><td className="money">{money(s.totalAmount)}</td></tr>)}</tbody>
          </table>
        </div>
      </section>
    </AppShell>
  );
}

function Kpi({label,value,icon,note}:{label:string;value:string;icon:string;note:string}) {
  return <div className="card kpi"><div className="kpi-head"><span>{label}</span><span className="kpi-icon">{icon}</span></div><div className="kpi-value">{value}</div><div className="kpi-note">{note}</div></div>;
}
