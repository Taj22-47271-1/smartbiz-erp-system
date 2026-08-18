"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type Product={id:string;name:string;sku:string;purchasePrice:number;salePrice:number;currentStock:number;reorderLevel:number;category:string;categoryId:string};
type Category={id:string;name:string};
const money=(v:number)=>new Intl.NumberFormat("en-BD",{style:"currency",currency:"BDT",maximumFractionDigits:0}).format(v);

export default function ProductsPage(){
  const [products,setProducts]=useState<Product[]>([]);
  const [categories,setCategories]=useState<Category[]>([]);
  const [open,setOpen]=useState(false); const [q,setQ]=useState(""); const [error,setError]=useState("");
  const [form,setForm]=useState({name:"",sku:"",purchasePrice:0,salePrice:0,reorderLevel:5,categoryId:""});

  const load=()=>Promise.all([api<Product[]>("/catalog/products"),api<Category[]>("/catalog/categories")]).then(([p,c])=>{setProducts(p);setCategories(c); if(!form.categoryId&&c[0])setForm(f=>({...f,categoryId:c[0].id}));}).catch(e=>setError(e.message));
  useEffect(()=>{load();},[]);
  const filtered=useMemo(()=>products.filter(x=>`${x.name} ${x.sku} ${x.category}`.toLowerCase().includes(q.toLowerCase())),[products,q]);

  async function submit(e:FormEvent){e.preventDefault();setError("");try{await api("/catalog/products",{method:"POST",body:JSON.stringify(form)});setOpen(false);setForm(f=>({...f,name:"",sku:"",purchasePrice:0,salePrice:0,reorderLevel:5}));load();}catch(e){setError(e instanceof Error?e.message:"Request failed.");}}

  return <AppShell title="Products">
    {error&&<div className="error">{error}</div>}
    <div className="toolbar"><input className="search" placeholder="Search products, SKU, category..." value={q} onChange={e=>setQ(e.target.value)}/><button className="primary-button" onClick={()=>setOpen(true)}>+ Add product</button></div>
    <section className="card table-wrap"><table><thead><tr><th>Product</th><th>SKU</th><th>Category</th><th>Purchase</th><th>Sale</th><th>Stock</th><th>Status</th></tr></thead>
      <tbody>{filtered.map(p=><tr key={p.id}><td><strong>{p.name}</strong></td><td className="mono">{p.sku}</td><td>{p.category}</td><td>{money(p.purchasePrice)}</td><td className="money">{money(p.salePrice)}</td><td>{p.currentStock}</td><td><span className={p.currentStock<=p.reorderLevel?"badge warn":"badge"}>{p.currentStock<=p.reorderLevel?"Low stock":"In stock"}</span></td></tr>)}</tbody>
    </table>{!filtered.length&&<div className="empty">No products found.</div>}</section>
    <Modal open={open} title="Add product" onClose={()=>setOpen(false)}>
      <form className="form" onSubmit={submit}>
        <div className="form-grid"><div className="field"><label>Product name</label><input required value={form.name} onChange={e=>setForm({...form,name:e.target.value})}/></div><div className="field"><label>SKU</label><input required value={form.sku} onChange={e=>setForm({...form,sku:e.target.value})}/></div></div>
        <div className="field"><label>Category</label><select required value={form.categoryId} onChange={e=>setForm({...form,categoryId:e.target.value})}>{categories.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></div>
        <div className="form-grid"><div className="field"><label>Purchase price</label><input type="number" min="0" required value={form.purchasePrice} onChange={e=>setForm({...form,purchasePrice:Number(e.target.value)})}/></div><div className="field"><label>Sale price</label><input type="number" min="0" required value={form.salePrice} onChange={e=>setForm({...form,salePrice:Number(e.target.value)})}/></div></div>
        <div className="field"><label>Reorder level</label><input type="number" min="0" required value={form.reorderLevel} onChange={e=>setForm({...form,reorderLevel:Number(e.target.value)})}/></div>
        <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Save product</button></div>
      </form>
    </Modal>
  </AppShell>
}
