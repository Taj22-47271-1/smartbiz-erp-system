# API Overview

Base URL: `http://localhost:5000/api`

All endpoints except login require a JWT bearer token.

| Module | Method | Endpoint |
|---|---|---|
| Auth | POST | `/auth/login` |
| Auth | POST | `/auth/change-password` |
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

## Change password

`POST /api/auth/change-password` requires a valid JWT token and changes only the signed-in user's password.

```json
{
  "currentPassword": "Admin123!",
  "newPassword": "NewSecure123!",
  "confirmPassword": "NewSecure123!"
}
```

Password rules: at least 8 characters with uppercase, lowercase, number and special character. The new password must be different from the current password.

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

## Attendance

All attendance endpoints require JWT authentication.

- `GET /api/attendance/me` — today's attendance state and office schedule (`attendance.checkin`)
- `POST /api/attendance/check-in` — mark Present/Late based on configured time (`attendance.checkin`)
- `POST /api/attendance/check-out` — manual checkout (`attendance.checkin`)
- `GET /api/attendance/my-history?month=YYYY-MM` — employee monthly history (`attendance.checkin`)
- `GET /api/attendance/daily?date=YYYY-MM-DD` — daily employee attendance (`attendance.view`)
- `GET /api/attendance/summary?month=YYYY-MM` — employee monthly attendance summary (`attendance.view`)
- `GET /api/attendance/settings` — read attendance settings (`attendance.view`)
- `PUT /api/attendance/settings` — update schedule, working days and auto checkout (`attendance.manage`)
