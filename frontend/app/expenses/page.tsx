"use client";
import { FormEvent, useEffect, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type Expense={id:string;category:string;description:string;amount:number;expenseDate:string};
const money=(v:number)=>new Intl.NumberFormat("en-BD",{style:"currency",currency:"BDT",maximumFractionDigits:0}).format(v);

export default function ExpensesPage(){
  const [rows,setRows]=useState<Expense[]>([]);const[open,setOpen]=useState(false);const[error,setError]=useState("");
  const [form,setForm]=useState({category:"Office",description:"",amount:0,expenseDate:new Date().toISOString().slice(0,10)});
  const load=()=>api<Expense[]>("/finance/expenses").then(setRows).catch(e=>setError(e.message));
  useEffect(()=>{load();},[]);
  async function submit(e:FormEvent){e.preventDefault();setError("");try{await api("/finance/expenses",{method:"POST",body:JSON.stringify({...form,expenseDate:new Date(form.expenseDate).toISOString()})});setOpen(false);setForm({...form,description:"",amount:0});load();}catch(e){setError(e instanceof Error?e.message:"Request failed.");}}
  return <AppShell title="Expenses">
    {error&&<div className="error">{error}</div>}
    <div className="toolbar"><div><strong>{money(rows.reduce((s,x)=>s+x.amount,0))}</strong> total recorded</div><button className="primary-button" onClick={()=>setOpen(true)}>+ Add expense</button></div>
    <section className="card table-wrap"><table><thead><tr><th>Date</th><th>Category</th><th>Description</th><th>Amount</th></tr></thead><tbody>
      {rows.map(x=><tr key={x.id}><td>{new Date(x.expenseDate).toLocaleDateString()}</td><td><span className="badge muted">{x.category}</span></td><td>{x.description}</td><td className="money">{money(x.amount)}</td></tr>)}
    </tbody></table>{!rows.length&&<div className="empty">No expenses recorded.</div>}</section>
    <Modal open={open} title="Add expense" onClose={()=>setOpen(false)}><form className="form" onSubmit={submit}>
      <div className="form-grid"><div className="field"><label>Category</label><input required value={form.category} onChange={e=>setForm({...form,category:e.target.value})}/></div><div className="field"><label>Date</label><input type="date" required value={form.expenseDate} onChange={e=>setForm({...form,expenseDate:e.target.value})}/></div></div>
      <div className="field"><label>Description</label><input required value={form.description} onChange={e=>setForm({...form,description:e.target.value})}/></div>
      <div className="field"><label>Amount</label><input type="number" min="1" required value={form.amount} onChange={e=>setForm({...form,amount:Number(e.target.value)})}/></div>
      <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Save expense</button></div>
    </form></Modal>
  </AppShell>
}
