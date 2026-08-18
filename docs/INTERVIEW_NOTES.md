# Interview Talking Points

## What problem does this project solve?
SmartBiz ERP centralizes sales, purchases, inventory, customer/supplier records, expenses, user access and reporting for a small or medium business.

## How is inventory handled?
Inventory is transaction-driven. A purchase adds stock and creates a positive stock movement. A sale first checks available stock, then decreases stock and creates a negative stock movement. Both flows use database transactions.

## How does authorization work?
Users belong to roles. Roles contain permissions. Permissions are embedded into the JWT at login and ASP.NET Core authorization policies protect module endpoints.

## How do you protect data consistency?
Sales and purchases execute inside database transactions. A sale is rejected if any requested product has insufficient stock. Products with transaction history cannot be deleted.

## What would you add next?
Multi-branch inventory, payment ledger, invoice PDF, payroll, refresh-token rotation, automated tests, CI/CD and cloud deployment.
