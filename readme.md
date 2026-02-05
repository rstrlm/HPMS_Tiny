# HotelPms – Property Management System

A practical, self-hosted property management system for hotel and spa operations. Built with .NET 8, React, Keycloak, and MSSQL. Runs on-prem with Docker Compose.

## Features

- **Room reservations** with availability checks, multi-room stays, and mid-stay room changes
- **Treatment bookings** with capacity-based scheduling (multiple guests in the same sauna/pool simultaneously)
- **Housekeeping** task generation, assignment, and room status workflow
- **Billing** with folios, charges, partial payments, invoices, and PDF generation
- **Staff management** with Keycloak identity sync
- **Role-based access control** (manager, frontdesk, cleaner, maintenance, therapist)
- **Configurable branding** via environment variables (company name, address, tax ID, bank details)

## Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 8 / ASP.NET Core |
| Database | MSSQL 2022 (EF Core) |
| Auth | Keycloak 24 (OIDC / JWT) |
| Frontend | React 18, TypeScript, Tailwind CSS, Vite |
| PDF | QuestPDF |
| Infra | Docker Compose |

## Project Structure

```
src/
  Pms.Domain/           # Entities, enums, value objects (no EF dependency)
  Pms.Application/      # Interfaces, DTOs, validators, settings
  Pms.Infrastructure/   # EF Core, services, Keycloak admin client, PDF generation
  Pms.Api/              # Controllers, auth policies, Program.cs
  Pms.Web/              # React SPA (Vite + Tailwind)
tests/
  Pms.Tests/            # Unit & integration tests (xUnit)
keycloak/
  pms-realm.json        # Realm config with roles, clients, and test users
scripts/
  init-databases.sql    # Creates pms_db and keycloak_db
docs/                   # Architecture, domain, RBAC, API, workflows documentation
```

## Quick Start

### Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for frontend development)

### 1. Start infrastructure

```bash
cp .env.example .env        # review and adjust if needed
docker compose up -d mssql mssql-init keycloak
```

This starts MSSQL (port 1433) and Keycloak (port 8080). The `mssql-init` service creates the databases automatically.

### 2. Run the API

```bash
dotnet run --project src/Pms.Api
```

The API starts at `http://localhost:5134`. Swagger UI is available at `/swagger`.

### 3. Run the frontend

```bash
cd src/Pms.Web
npm install
npm run dev
```

The frontend starts at `http://localhost:5173`.

### 4. Log in

Keycloak comes pre-configured with test users. Open the frontend and log in with:

| User | Password | Role |
|---|---|---|
| `manager` | `manager` | manager |
| `frontdesk` | `frontdesk` | frontdesk |
| `cleaner` | `cleaner` | cleaner |
| `maintenance` | `maintenance` | maintenance |
| `therapist` | `therapist` | therapist |

Keycloak admin console is at `http://localhost:8080` (admin / admin).

### Full stack via Docker

To run everything in Docker, including the API:

```bash
docker compose up -d
```

The API will be available at `http://localhost:5134`.

## Branding Configuration

All branding is configurable via environment variables or `appsettings.json`. This allows deploying the same system for different properties.

| Variable | Default | Description |
|---|---|---|
| `Branding__CompanyName` | Hotel PMS | Display name in UI and invoices |
| `Branding__CompanyLegalName` | Hotel PMS Oy | Legal entity name on invoices |
| `Branding__Tagline` | Hotel PMS System | Shown in sidebar and loading screen |
| `Branding__Address` | PMS System | Company address in UI and invoices | Invoice address |
| `Branding__Email` | info@pms.example.com | Contact email on invoices |
| `Branding__Phone` | +358 3 123 4567 | Contact phone on invoices |
| `Branding__TaxId` | 1234567-8 | Tax/VAT ID on invoices |
| `Branding__BankName` | Nordea Finland | Bank name on invoices |
| `Branding__IBAN` | FI12 3456 7890 1234 56 | IBAN on invoices |
| `Branding__BIC` | NDEAFIHH | BIC/SWIFT code on invoices |

Set these in your `.env` file, `docker-compose.yml`, or as system environment variables.

## API Overview

All endpoints are under `/api/v1/`. Authentication is required unless noted.

| Resource | Endpoints | Access |
|---|---|---|
| **Customers** | CRUD | frontdesk, manager |
| **Rooms** | CRUD + state blocks | manager (CRUD), maintenance (blocks) |
| **Room Types** | CRUD | manager |
| **Reservations** | CRUD + status changes, room assignments, availability, holds | frontdesk, manager |
| **Treatment Types** | CRUD | manager |
| **Treatment Rooms** | CRUD + availability slots | manager |
| **Appointments** | CRUD + status changes | frontdesk (CRUD), therapist (status) |
| **Housekeeping** | Task CRUD, generate, assign, start, complete, skip | manager (create/generate), cleaner (workflow) |
| **Staff** | CRUD + Keycloak user creation | manager |
| **Folios** | CRUD, charges, payments, invoices, close, cancel, merge, PDF download | frontdesk, manager |
| **Branding** | GET config | public (no auth) |

Full endpoint documentation is available via Swagger at `/swagger` when the API is running.

## Domain Model

### Core Entities

- **Customer** — name, email, phone, address
- **Reservation** — date range, status (Confirmed → CheckedIn → CheckedOut), linked to one or more RoomAssignments
- **Room** — room number, type, status (Available, Occupied, NeedsCleaning, etc.)
- **RoomAssignment** — links a room to a reservation for specific dates
- **RoomStateBlock** — time-ranged maintenance or out-of-service blocks

### Treatments

- **TreatmentType** — name, duration, price, whether a therapist is required
- **TreatmentRoom** — name, capacity (e.g., sauna fits 3 people)
- **TreatmentAppointment** — scheduled treatment with capacity-aware booking

### Billing

- **Folio** — running bill for a customer (optionally linked to a reservation)
- **Charge** — line item (room night, treatment, custom) with VAT calculation
- **Payment** — partial or full payment (cash, card)
- **Invoice** — snapshot of folio at issuance time, downloadable as PDF

### Operations

- **CleaningTask** — daily housekeeping tasks with assign/start/complete workflow
- **StaffProfile** — linked to Keycloak identity, used for task assignment
- **AuditLog** — tracks important operations for compliance

## Availability Rules

**Rooms**: A room is bookable if no overlapping RoomAssignment or RoomStateBlock exists for the requested dates, and the room is active.

**Treatment rooms**: Capacity-based. Multiple overlapping appointments are allowed as long as the sum of concurrent `SeatsUsed` does not exceed the room's `Capacity`.

**Therapists**: A therapist can only have one appointment at a time (capacity = 1).

**Holds**: A 10-minute hold can be placed on a room during booking to prevent race conditions. Holds expire automatically.

## Testing

```bash
dotnet test tests/Pms.Tests
```

Tests cover:
- Room availability and overlap detection
- Treatment room capacity calculations
- Therapist double-booking prevention
- Reservation lifecycle (create, status changes, room assignments)
- Folio operations (charges, payments, invoices, close, merge)
- Housekeeping task workflow

## Building for Production

### API

```bash
dotnet publish src/Pms.Api -c Release -o ./publish
```

Or build the Docker image:

```bash
docker build -f src/Pms.Api/Dockerfile -t hotel-pms-api .
```

### Frontend

```bash
cd src/Pms.Web
npm run build
```

Output is in `src/Pms.Web/dist/`. Serve with any static file server or configure as part of the API.

## License

MIT License. See [LICENSE](LICENSE) for details.
