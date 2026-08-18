# API Overview

Base URL: `http://localhost:5000/api`

All endpoints except login require a JWT bearer token.

| Module | Method | Endpoint |
|---|---|---|
| Auth | POST | `/auth/login` |
| Dashboard | GET | `/dashboard` |
| Categories | GET/POST | `/catalog/categories` |
| Products | GET/POST | `/catalog/products` |
| Product | PUT/DELETE | `/catalog/products/{id}` |
| Customers | GET/POST | `/parties/customers` |
| Suppliers | GET/POST | `/parties/suppliers` |
| Purchases | GET/POST | `/purchases` |
| Sales | GET/POST | `/sales` |
| Expenses | GET/POST | `/finance/expenses` |
| Stock history | GET | `/finance/stock-movements` |
| Permissions | GET | `/admin/permissions` |
| Roles | GET/POST | `/admin/roles` |
| Users | GET/POST | `/admin/users` |
| User activation | PATCH | `/admin/users/{id}/active` |
| Audit logs | GET | `/admin/audit-logs` |

## Login example

```json
{
  "email": "admin@smartbiz.local",
  "password": "Admin123!"
}
```

## Create purchase example

```json
{
  "supplierId": "SUPPLIER_GUID",
  "items": [
    {
      "productId": "PRODUCT_GUID",
      "quantity": 10,
      "unitCost": 2200
    }
  ]
}
```

## Create sale example

```json
{
  "customerId": "CUSTOMER_GUID",
  "discount": 100,
  "items": [
    {
      "productId": "PRODUCT_GUID",
      "quantity": 2,
      "unitPrice": 2990
    }
  ]
}
```
