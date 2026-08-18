"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import AppShell from "@/components/AppShell";
import Modal from "@/components/Modal";
import { api } from "@/lib/api";

type Product={id:string;name:string;sku:string;purchasePrice:number;salePrice:number;currentStock:number};
type Party={id:string;name:string};
type TxItem={productId:string;quantity:number;unitCost?:number;unitPrice?:number;product?:string;lineTotal?:number};
type Tx={id:string;purchaseNo?:string;invoiceNo?:string;supplier?:string;customer?:string;totalAmount:number;createdAt:string;items:TxItem[]};

const money=(v:number)=>new Intl.NumberFormat("en-BD",{style:"currency",currency:"BDT",maximumFractionDigits:0}).format(v);

export default function TransactionPage({kind}:{kind:"purchases"|"sales"}){
  const isPurchase=kind==="purchases";
  const title=isPurchase?"Purchases":"Sales";
  const partyPath=isPurchase?"suppliers":"customers";
  const [rows,setRows]=useState<Tx[]>([]);
  const [products,setProducts]=useState<Product[]>([]);
  const [parties,setParties]=useState<Party[]>([]);
  const [partyId,setPartyId]=useState("");
  const [items,setItems]=useState<TxItem[]>([{productId:"",quantity:1,unitCost:0,unitPrice:0}]);
  const [discount,setDiscount]=useState(0);
  const [open,setOpen]=useState(false);
  const [error,setError]=useState("");

  async function load(){
    try{
      const [r,p,party]=await Promise.all([
        api<Tx[]>(`/${kind}`),
        api<Product[]>("/catalog/products"),
        api<Party[]>(`/parties/${partyPath}`)
      ]);
      setRows(r);setProducts(p);setParties(party);
      if(!partyId&&party[0])setPartyId(party[0].id);
      setItems(current=>current.map((x,i)=>i===0&&!x.productId&&p[0]?{
        ...x,productId:p[0].id,unitCost:p[0].purchasePrice,unitPrice:p[0].salePrice
      }:x));
    }catch(e){setError(e instanceof Error?e.message:"Request failed.");}
  }
  useEffect(()=>{load();},[]);

  function updateProduct(index:number,productId:string){
    const p=products.find(x=>x.id===productId);
    setItems(items.map((x,i)=>i===index?{...x,productId,unitCost:p?.purchasePrice??0,unitPrice:p?.salePrice??0}:x));
  }
  function addLine(){
    const p=products[0];
    setItems([...items,{productId:p?.id??"",quantity:1,unitCost:p?.purchasePrice??0,unitPrice:p?.salePrice??0}]);
  }
  function removeLine(index:number){if(items.length>1)setItems(items.filter((_,i)=>i!==index));}

  async function submit(e:FormEvent){
    e.preventDefault();setError("");
    try{
      const payload=isPurchase
        ? {supplierId:partyId,items:items.map(x=>({productId:x.productId,quantity:x.quantity,unitCost:x.unitCost}))}
        : {customerId:partyId,discount,items:items.map(x=>({productId:x.productId,quantity:x.quantity,unitPrice:x.unitPrice}))};
      await api(`/${kind}`,{method:"POST",body:JSON.stringify(payload)});
      setOpen(false);setDiscount(0);setItems([{productId:products[0]?.id??"",quantity:1,unitCost:products[0]?.purchasePrice??0,unitPrice:products[0]?.salePrice??0}]);load();
    }catch(e){setError(e instanceof Error?e.message:"Request failed.");}
  }

  const total=useMemo(()=>items.reduce((sum,x)=>sum+x.quantity*(isPurchase?(x.unitCost??0):(x.unitPrice??0)),0)-(isPurchase?0:discount),[items,discount,isPurchase]);

  return <AppShell title={title}>
    {error&&<div className="error">{error}</div>}
    <div className="toolbar"><div><strong>{rows.length}</strong> transactions</div><button className="primary-button" onClick={()=>setOpen(true)}>+ New {isPurchase?"purchase":"sale"}</button></div>
    <section className="card table-wrap"><table><thead><tr><th>Reference</th><th>{isPurchase?"Supplier":"Customer"}</th><th>Date</th><th>Items</th><th>Total</th></tr></thead>
      <tbody>{rows.map(r=><tr key={r.id}><td className="mono">{r.purchaseNo??r.invoiceNo}</td><td>{r.supplier??r.customer}</td><td>{new Date(r.createdAt).toLocaleDateString()}</td><td>{r.items.length}</td><td className="money">{money(r.totalAmount)}</td></tr>)}</tbody>
    </table>{!rows.length&&<div className="empty">No {kind} yet.</div>}</section>

    <Modal open={open} title={`Create ${isPurchase?"purchase":"sale"}`} onClose={()=>setOpen(false)}>
      <form className="form" onSubmit={submit}>
        <div className="field"><label>{isPurchase?"Supplier":"Customer"}</label><select required value={partyId} onChange={e=>setPartyId(e.target.value)}>{parties.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select></div>
        <div className="line-items">
          {items.map((line,index)=><div className="line-item" key={index}>
            <div className="field"><label>Product</label><select required value={line.productId} onChange={e=>updateProduct(index,e.target.value)}>{products.map(p=><option key={p.id} value={p.id}>{p.name} ({p.currentStock} in stock)</option>)}</select></div>
            <div className="field"><label>Qty</label><input type="number" min="1" value={line.quantity} onChange={e=>setItems(items.map((x,i)=>i===index?{...x,quantity:Number(e.target.value)}:x))}/></div>
            <div className="field"><label>{isPurchase?"Unit cost":"Unit price"}</label><input type="number" min="0" value={isPurchase?line.unitCost:line.unitPrice} onChange={e=>setItems(items.map((x,i)=>i===index?{...x,[isPurchase?"unitCost":"unitPrice"]:Number(e.target.value)}:x))}/></div>
            <button type="button" className="danger-button" onClick={()=>removeLine(index)}>×</button>
          </div>)}
        </div>
        <button type="button" className="secondary-button" onClick={addLine}>+ Add line</button>
        {!isPurchase&&<div className="field"><label>Discount</label><input type="number" min="0" value={discount} onChange={e=>setDiscount(Number(e.target.value))}/></div>}
        <div style={{display:"flex",justifyContent:"space-between",fontWeight:850,fontSize:16,paddingTop:8}}><span>Total</span><span>{money(Math.max(0,total))}</span></div>
        <div className="form-actions"><button type="button" className="secondary-button" onClick={()=>setOpen(false)}>Cancel</button><button className="primary-button">Save transaction</button></div>
      </form>
    </Modal>
  </AppShell>
}
