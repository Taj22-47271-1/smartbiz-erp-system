import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "SmartBiz ERP",
  description: "Portfolio-ready ERP for SME business management"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
