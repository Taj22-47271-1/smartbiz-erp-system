"use client";
import { ReactNode } from "react";

export default function Modal({ open, title, onClose, children }: {
  open: boolean; title: string; onClose: () => void; children: ReactNode
}) {
  if (!open) return null;
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <div className="modal" onMouseDown={(e) => e.stopPropagation()}>
        <div className="modal-head"><h2>{title}</h2><button className="icon-button" onClick={onClose}>×</button></div>
        {children}
      </div>
    </div>
  );
}
