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
- PDF payslip per employee (QuestPDF)
- Excel export untuk BPJS
- Bank file generation untuk disbursement

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
cd src/Api
dotnet run
```

Backend akan berjalan di `http://localhost:5000`

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

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Domain.Tests
dotnet test tests/Engine.Tests
dotnet test tests/Application.Tests
```

## Deployment

### Backend (Docker)
```bash
docker build -t payroll-api .
docker run -p 5000:5000 payroll-api
```

### Frontend (Vercel)
```bash
cd frontend
vercel deploy
```

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