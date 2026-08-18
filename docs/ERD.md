# SmartBiz ERP — Entity Relationship Overview

```mermaid
erDiagram
  ROLE ||--o{ USER : assigns
  ROLE ||--o{ ROLE_PERMISSION : contains
  PERMISSION ||--o{ ROLE_PERMISSION : grants
  CATEGORY ||--o{ PRODUCT : groups
  SUPPLIER ||--o{ PURCHASE : receives
  PURCHASE ||--|{ PURCHASE_ITEM : contains
  PRODUCT ||--o{ PURCHASE_ITEM : purchased
  CUSTOMER ||--o{ SALE : places
  SALE ||--|{ SALE_ITEM : contains
  PRODUCT ||--o{ SALE_ITEM : sold
  PRODUCT ||--o{ STOCK_MOVEMENT : tracks
```

## Inventory rule

`Product.CurrentStock` is updated only by business transactions. Purchases create positive stock movements; sales create negative stock movements after validating availability.
