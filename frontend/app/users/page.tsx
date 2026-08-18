"use client";
import { FormEvent, useEffect, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type User={id:string;fullName:string;email:string;isActive:boolean;role:string;roleId:string};
type Role={id:string;name:string;description:string;permissions:string[]};

export default function UsersPage(){
  const[rows,setRows]=useState<User[]>([]);const[roles,setRoles]=useState<Role[]>([]);const[open,setOpen]=useState(false);const[error,setError]=useState("");
  const[form,setForm]=useState({fullName:"",email:"",password:"",roleId:""});
  const load=()=>Promise.all([api<User[]>("/admin/users"),api<Role[]>("/admin/roles")]).then(([u,r])=>{setRows(u);setRoles(r);if(!form.roleId&&r[0])setForm(f=>({...f,roleId:r[0].id}));}).catch(e=>setError(e.message));
  useEffect(()=>{load();},[]);
  async function submit(e:FormEvent){e.preventDefault();setError("");try{await api("/admin/users",{method:"POST",body:JSON.stringify(form)});setOpen(false);setForm(f=>({...f,fullName:"",email:"",password:""}));load();}catch(e){setError(e instanceof Error?e.message:"Request failed.");}}
  async function toggle(id:string){try{await api(`/admin/users/${id}/active`,{method:"PATCH"});load();}catch(e){setError(e instanceof Error?e.message:"Request failed.");}}
  return <AppShell title="Users">
    {error&&<div className="error">{error}</div>}
    <div className="toolbar"><div><strong>{rows.length}</strong> system users</div><button className="primary-button" onClick={()=>setOpen(true)}>+ Add user</button></div>
    <section className="card table-wrap"><table><thead><tr><th>User</th><th>Email</th><th>Role</th><th>Status</th><th></th></tr></thead><tbody>{rows.map(x=><tr key={x.id}><td><strong>{x.fullName}</strong></td><td>{x.email}</td><td>{x.role}</td><td><span className={x.isActive?"badge":"badge muted"}>{x.isActive?"Active":"Disabled"}</span></td><td><button className="secondary-button" onClick={()=>toggle(x.id)}>{x.isActive?"Disable":"Enable"}</button></td></tr>)}</tbody></table></section>
    <Modal open={open} title="Add user" onClose={()=>setOpen(false)}><form className="form" onSubmit={submit}>
      <div className="field"><label>Full name</label><input required value={form.fullName} onChange={e=>setForm({...form,fullName:e.target.value})}/></div>
      <div className="form-grid"><div className="field"><label>Email</label><input type="email" required value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/></div><div className="field"><label>Temporary password</label><input type="password" minLength={8} required value={form.password} onChange={e=>setForm({...form,password:e.target.value})}/></div></div>
      <div className="field"><label>Role</label><select required value={form.roleId} onChange={e=>setForm({...form,roleId:e.target.value})}>{roles.map(r=><option key={r.id} value={r.id}>{r.name}</option>)}</select></div>
      <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Create user</button></div>
    </form></Modal>
  </AppShell>
}
