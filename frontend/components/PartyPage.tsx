"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type Party = { id:string; name:string; phone?:string; email?:string; address?:string };

export default function PartyPage({ kind }: { kind: "customers" | "suppliers" }) {
  const title = kind === "customers" ? "Customers" : "Suppliers";
  const [items, setItems] = useState<Party[]>([]);
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");
  const [error, setError] = useState("");
  const [form, setForm] = useState({name:"",phone:"",email:"",address:""});

  const load = () => api<Party[]>(`/parties/${kind}`).then(setItems).catch(e => setError(e.message));
  useEffect(() => { load(); }, []);

  const filtered = useMemo(() => items.filter(x => `${x.name} ${x.phone ?? ""} ${x.email ?? ""}`.toLowerCase().includes(q.toLowerCase())), [items,q]);

  async function submit(e: FormEvent) {
    e.preventDefault(); setError("");
    try {
      await api(`/parties/${kind}`, {method:"POST", body:JSON.stringify(form)});
      setOpen(false); setForm({name:"",phone:"",email:"",address:""}); load();
    } catch(e) { setError(e instanceof Error ? e.message : "Request failed."); }
  }

  return <AppShell title={title}>
    {error && <div className="error">{error}</div>}
    <div className="toolbar">
      <input className="search" placeholder={`Search ${kind}...`} value={q} onChange={e=>setQ(e.target.value)} />
      <button className="primary-button" onClick={()=>setOpen(true)}>+ Add {kind === "customers" ? "customer" : "supplier"}</button>
    </div>
    <section className="card table-wrap">
      <table><thead><tr><th>Name</th><th>Phone</th><th>Email</th><th>Address</th></tr></thead>
        <tbody>{filtered.map(x=><tr key={x.id}><td><strong>{x.name}</strong></td><td>{x.phone || "—"}</td><td>{x.email || "—"}</td><td>{x.address || "—"}</td></tr>)}</tbody>
      </table>
      {!filtered.length && <div className="empty">No {kind} found.</div>}
    </section>
    <Modal open={open} title={`Add ${kind === "customers" ? "customer" : "supplier"}`} onClose={()=>setOpen(false)}>
      <form className="form" onSubmit={submit}>
        <div className="field"><label>Name</label><input required value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></div>
        <div className="form-grid">
          <div className="field"><label>Phone</label><input value={form.phone} onChange={e=>setForm({...form,phone:e.target.value})}/></div>
          <div className="field"><label>Email</label><input type="email" value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></div>
        </div>
        <div className="field"><label>Address</label><textarea value={form.address} onChange={e=>setForm({...form,address:e.target.value})}/></div>
        <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Save</button></div>
      </form>
    </Modal>
  </AppShell>
}
