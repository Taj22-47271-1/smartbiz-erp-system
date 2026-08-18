"use client";
import { FormEvent, useEffect, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type Permission={id:string;key:string;description:string};
type Role={id:string;name:string;description:string;permissions:string[]};

export default function RolesPage(){
  const[rows,setRows]=useState<Role[]>([]);
  const[permissions,setPermissions]=useState<Permission[]>([]);
  const[open,setOpen]=useState(false);
  const[error,setError]=useState("");
  const[form,setForm]=useState({name:"",description:"",permissionKeys:[] as string[]});

  const load=()=>Promise.all([api<Role[]>("/admin/roles"),api<Permission[]>("/admin/permissions")])
    .then(([r,p])=>{setRows(r);setPermissions(p);})
    .catch(e=>setError(e.message));
  useEffect(()=>{load();},[]);

  function toggle(key:string){
    setForm({...form,permissionKeys:form.permissionKeys.includes(key)?form.permissionKeys.filter(x=>x!==key):[...form.permissionKeys,key]});
  }
  async function submit(e:FormEvent){
    e.preventDefault();setError("");
    try{
      await api("/admin/roles",{method:"POST",body:JSON.stringify(form)});
      setOpen(false);setForm({name:"",description:"",permissionKeys:[]});load();
    }catch(e){setError(e instanceof Error?e.message:"Request failed.");}
  }

  return <AppShell title="Roles & Permissions">
    {error&&<div className="error">{error}</div>}
    <div className="toolbar"><div><strong>{rows.length}</strong> access roles</div><button className="primary-button" onClick={()=>setOpen(true)}>+ Create role</button></div>
    <section className="card table-wrap"><table><thead><tr><th>Role</th><th>Description</th><th>Permissions</th></tr></thead><tbody>{rows.map(r=><tr key={r.id}><td><strong>{r.name}</strong></td><td>{r.description}</td><td><div style={{display:"flex",gap:5,flexWrap:"wrap"}}>{r.permissions.map(p=><span className="badge muted" key={p}>{p}</span>)}</div></td></tr>)}</tbody></table></section>
    <Modal open={open} title="Create role" onClose={()=>setOpen(false)}><form className="form" onSubmit={submit}>
      <div className="field"><label>Role name</label><input required value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></div>
      <div className="field"><label>Description</label><input required value={form.description} onChange={e=>setForm({...form,description:e.target.value})}/></div>
      <div className="field"><label>Permissions</label><div className="permission-grid">{permissions.map(p=><label className="check" key={p.id}><input type="checkbox" checked={form.permissionKeys.includes(p.key)} onChange={()=>toggle(p.key)}/><span><strong>{p.key}</strong><br/><span style={{color:"var(--muted)"}}>{p.description}</span></span></label>)}</div></div>
      <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Create role</button></div>
    </form></Modal>
  </AppShell>
}
