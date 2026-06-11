# Payroll Application

Enterprise payroll application untuk mengelola penggajian karyawan secara end-to-end: dari setup komponen gaji, kalkulasi PPh 21 / BPJS, approval workflow, hingga disbursement dan reporting.

## Tech Stack

### Backend
- **.NET 10** - Web API
- **Marten** - Event Store (PostgreSQL)
- **MediatR** - CQRS pattern
- **Hangfire** - Background jobs
- **QuestPDF** - PDF generation
- **ClosedXML** - Excel export
- **FluentValidation** - Input validation

### Frontend
- **Next.js 14** - App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **Shadcn/ui** - UI components
- **TanStack Query** - Server state management
- **React Hook Form** - Form handling
- **Zod** - Schema validation

## Architecture

Project ini menggunakan **Clean Architecture** dengan **CQRS** dan **Event Sourcing**:

```
┌─────────────────────────────────────────────────────────┐
│                         API Layer                        │
│                    (Minimal API Endpoints)               │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│              (MediatR Commands & Queries)                │
│         ValidationBehavior → LoggingBehavior             │
└─────────────────────────────────────────────────────────┘
                            │
                ┌───────────┴───────────┐
                ▼                       ▼
┌───────────────────────┐   ┌───────────────────────┐
│    Domain Layer       │   │    Engine Layer       │
│  (Aggregates, Events) │   │  (Pure Calculation)   │
│   Event Sourcing      │   │   PPh21, BPJS, etc    │
└───────────────────────┘   └───────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                     │
│    Marten (Event Store) | Projections | Jobs | PDF      │
└─────────────────────────────────────────────────────────┘
```

## Key Features

### 1. Payroll Processing
- Create payroll run per period (bulan + tahun)
- Automatic calculation via background job
- Support prorate untuk karyawan baru/resign
- Overtime calculation
- Multi-component salary (gaji pokok, tunjangan, bonus, dll)

### 2. Tax & BPJS Calculation
- **PPh 21** menggunakan Tarif Efektif Rata-rata (TER) 2024
- **BPJS Kesehatan**: Karyawan 1%, Perusahaan 4%
- **BPJS Ketenagakerjaan**: JHT, JP, JKK, JKM
- Automatic PTKP calculation based on marital status

### 3. Approval Workflow
```
Draft → Calculating → Calculated → UnderReview → Approved → Locked → Disbursed
```

### 4. Document Generation
- **PDF Payslip** per employee (QuestPDF) - professional layout
- **Excel Export** dengan 3 sheets: Summary, BPJS, PPh 21
- **Bank File Generation** untuk 4 bank: BCA, Mandiri, BNI, Permata

### 5. Event Sourcing
- Full audit trail via event store
- Aggregate reconstruction dari events
- Optimistic concurrency control
- Read models via Marten projections

## Project Structure

```
payroll-app/
├── src/
│   ├── Api/                    # Minimal API endpoints
│   ├── Application/            # MediatR handlers
│   ├── Domain/                 # Aggregates, Events, Value Objects
│   ├── Engine/                 # Pure calculation logic
│   ├── Infrastructure/         # Marten, Hangfire, QuestPDF
│   └── ReadModels/             # Projection targets
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   └── Engine.Tests/
├── frontend/                   # Next.js 14 App Router
│   ├── app/
│   ├── components/
│   └── lib/
├── .bob/                       # AI agent rules
│   ├── rules/
│   └── rules-code/
├── AGENTS.md                   # Project overview untuk AI
├── PHASE_PROMPTS.md           # Development phases
└── docker-compose.yml         # PostgreSQL + Redis
```

## Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL 16
- Redis (optional, untuk Hangfire)

### Backend Setup

1. Clone repository:
```bash
git clone https://github.com/hasankhairullahdev/PayrollApp.git
cd PayrollApp
```

2. Start PostgreSQL via Docker:
```bash
docker-compose up -d
```

3. Set environment variables:
```bash
# .env
DATABASE_URL=Host=localhost;Database=payroll_db;Username=postgres;Password=postgres
REDIS_URL=localhost:6379
JWT_SECRET=your-secret-here
```

4. Run backend:
```bash
cd src/PayrollApp.Api
dotnet run
```

Backend akan berjalan di `https://localhost:5044` (atau check launchSettings.json)

Atau gunakan PowerShell scripts:
```powershell
# Start semua services (PostgreSQL, Redis, API)
.\start.ps1

# Stop semua services
.\stop.ps1
```

### Frontend Setup

1. Install dependencies:
```bash
cd frontend
npm install
```

2. Set environment variables:
```bash
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5000
```

3. Run development server:
```bash
npm run dev
```

Frontend akan berjalan di `http://localhost:3000`

## Development Guidelines

### Domain Layer Rules
- **Zero external dependencies** - pure C# only
- State changes hanya via domain events
- Immutable events (use `record`)
- Enforce invariants di aggregate

### Engine Layer Rules
- **Pure calculation** - no side effects
- No async, no DI, no external calls
- Always use `decimal` for money calculations
- Unit testable tanpa mock

### Application Layer Rules
- One file per Command/Query + Handler
- Use `IRequest<Result<T>>` untuk commands
- Use `IRequest<T>` untuk queries
- FluentValidation untuk input validation

### Infrastructure Layer Rules
- Marten `AppendOptimistic` untuk concurrency
- Idempotent background jobs
- Projections untuk read models
- No direct event stream queries

## API Endpoints

### Payroll Management
```
POST   /api/payroll                    # Create payroll run
GET    /api/payroll                    # List payroll runs (with status filter)
GET    /api/payroll/{id}               # Get payroll run detail
POST   /api/payroll/{id}/approve       # Approve payroll
POST   /api/payroll/{id}/lock          # Lock payroll
GET    /api/payroll/{id}/line-items    # Get line items
```

### Reports & Export
```
GET    /api/reports/payroll/{id}/payslip/{employeeId}/pdf   # Download PDF payslip
GET    /api/reports/payroll/{id}/export/excel               # Download Excel export
GET    /api/reports/payroll/{id}/bank-file?bank=bca         # Generate bank file
```

Supported banks: `bca`, `mandiri`, `bni`, `permata`

### Health Check
```
GET    /health                         # API health status
```

### Hangfire Dashboard
```
GET    /hangfire                       # Background jobs dashboard
```

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/PayrollApp.Tests.Domain
dotnet test tests/PayrollApp.Tests.Engine

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Test Coverage
- **Domain Tests**: Aggregate behavior, state machine, invariants
- **Engine Tests**: PPh21 calculation, BPJS calculation, prorate logic
- **Integration Tests**: End-to-end command/query flows

## Demo Flow

1. **Start Services**
   ```powershell
   .\start.ps1
   ```

2. **Create Payroll Run**
   - Open frontend: `http://localhost:3000/payroll`
   - Click "Create Payroll Run"
   - Select period (month + year)
   - Submit

3. **Wait for Calculation**
   - Status akan berubah: Draft → Calculating → Calculated
   - Monitor di Hangfire dashboard: `https://localhost:5044/hangfire`

4. **Review & Approve**
   - Click "View Details" pada payroll run
   - Review line items
   - Click "Approve Payroll"

5. **Lock & Generate Documents**
   - Click "Lock Payroll"
   - Download PDF payslip per employee
   - Download Excel export
   - Generate bank file untuk disbursement

## Deployment

### Backend (Docker)
```bash
docker build -t payroll-api -f src/PayrollApp.Api/Dockerfile .
docker run -p 5044:5044 \
  -e DATABASE_URL="Host=postgres;Database=payroll_db;Username=postgres;Password=postgres" \
  payroll-api
```

### Frontend (Vercel)
```bash
cd frontend
vercel deploy --prod
```

### Full Stack (Docker Compose)
```bash
docker-compose up -d
```

Services:
- API: `http://localhost:5044`
- Frontend: `http://localhost:3000`
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- Hangfire: `http://localhost:5044/hangfire`

## Contributing

1. Fork repository
2. Create feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'feat: add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open Pull Request

## License

MIT License - see LICENSE file for details

## Contact

Hasan Khairullah - [@hasankhairullahdev](https://github.com/hasankhairullahdev)

Project Link: [https://github.com/hasankhairullahdev/PayrollApp](https://github.com/hasankhairullahdev/PayrollApp)