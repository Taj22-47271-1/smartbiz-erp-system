# SmartBiz ERP

A portfolio-ready ERP system for small and medium businesses. The project demonstrates a real business workflow rather than only CRUD screens.

## Tech stack

- **Frontend:** Next.js (App Router), TypeScript, Recharts, CSS
- **Backend:** ASP.NET Core Web API (.NET 10 LTS), Entity Framework Core
- **Database:** PostgreSQL
- **Security:** JWT authentication + role-based authorization
- **API documentation:** OpenAPI
- **Infrastructure:** Docker Compose for PostgreSQL

## Modules

- Authentication
- Dashboard KPI and monthly sales/purchase chart
- Role and permission management
- User management
- Product and category management
- Customer and supplier management
- Purchases with automatic stock increase
- Sales with stock validation and automatic stock decrease
- Stock movement history
- Expense tracking
- Audit logs for write requests

## Important business flows

### Purchase
Supplier → Purchase → Purchase Items → Stock increases → Stock movement created

### Sale
Customer → Sale → Stock availability check → Sale Items → Stock decreases → Stock movement created

### Role-based access
Admin can create roles and assign permissions. Users are attached to roles, and JWT tokens carry role information.

## Demo credentials

After the first backend startup, seed data is created automatically.

- **Email:** `admin@smartbiz.local`
- **Password:** `Admin123!`

> Change the seeded password before deploying publicly.

## Local setup

### 1. Start PostgreSQL

```bash
docker compose up -d
```

### 2. Run backend

Install .NET 10 SDK, then:

```bash
cd backend
dotnet restore
dotnet run
```

### 3. Run frontend

```bash
cd frontend
npm install
cp .env.local.example .env.local
npm run dev
```

Open `http://localhost:3000`.

## Environment variables

Frontend `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Suggested CV entry

**SmartBiz ERP — Full Stack Enterprise Resource Planning System**  
Next.js, TypeScript, ASP.NET Core, PostgreSQL, REST API

Built a full-stack ERP system with inventory, sales, purchases, expenses, dashboards, JWT authentication, role-based access control, stock movement tracking, and audit logging. Implemented transactional business flows so purchases increase inventory and sales validate and reduce available stock.

## Suggested next upgrades

- Multi-branch inventory
- Invoice PDF generation
- Payroll and attendance
- Refresh tokens
- Email notifications
- Unit/integration tests
- CI/CD with GitHub Actions
- Cloud deployment
