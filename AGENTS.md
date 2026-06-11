# AGENTS.md — Payroll Application

## Project Overview

Enterprise payroll application untuk mengelola penggajian karyawan secara end-to-end: dari setup komponen gaji, kalkulasi PPh 21 / BPJS, approval workflow, hingga disbursement dan reporting.

- **Backend**: .NET 10 Web API
- **Frontend**: Next.js 14 (App Router)
- **Architecture**: Clean Architecture + CQRS + Event Sourcing
- **Event Store**: Marten (PostgreSQL)
- **Background Jobs**: Hangfire
- **PDF Generation**: QuestPDF
- **Excel Export**: ClosedXML

---

## Repository Structure

```
payroll-app/
├── src/
│   ├── Api/                              # Minimal API endpoints
│   │   └── Endpoints/
│   │       ├── PayrollEndpoints.cs
│   │       ├── EmployeeEndpoints.cs
│   │       ├── DisbursementEndpoints.cs
│   │       └── ReportEndpoints.cs
│   │
│   ├── Application/                      # MediatR Commands & Queries
│   │   ├── Payroll/
│   │   │   ├── Commands/
│   │   │   │   ├── CreatePayrollRunCommand.cs
│   │   │   │   ├── TriggerCalculationCommand.cs
│   │   │   │   ├── ApprovePayrollCommand.cs
│   │   │   │   ├── LockPayrollCommand.cs
│   │   │   │   └── InitiateDisbursementCommand.cs
│   │   │   └── Queries/
│   │   │       ├── GetPayrollSummaryQuery.cs
│   │   │       ├── GetPayrollLineItemsQuery.cs
│   │   │       └── GetPayrollRunsQuery.cs
│   │   ├── Employees/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Disbursement/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   └── Behaviors/
│   │       ├── ValidationBehavior.cs
│   │       ├── LoggingBehavior.cs
│   │       └── TransactionBehavior.cs
│   │
│   ├── Domain/                           # Pure domain — NO external dependencies
│   │   ├── Aggregates/
│   │   │   ├── PayrollRun.cs             # Aggregate root
│   │   │   └── Employee.cs
│   │   ├── Events/
│   │   │   ├── PayrollRunCreated.cs
│   │   │   ├── PayrollCalculated.cs
│   │   │   ├── PayrollReviewStarted.cs
│   │   │   ├── PayrollApproved.cs
│   │   │   ├── PayrollRejected.cs
│   │   │   ├── PayrollLocked.cs
│   │   │   ├── PayslipGenerated.cs
│   │   │   ├── DisbursementInitiated.cs
│   │   │   └── DisbursementConfirmed.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── TaxCalculation.cs
│   │   │   ├── BPJSComponent.cs
│   │   │   ├── SalaryComponent.cs
│   │   │   └── EmployeeId.cs
│   │   ├── Enums/
│   │   │   ├── PayrollStatus.cs
│   │   │   └── SalaryComponentType.cs
│   │   └── Exceptions/
│   │       ├── PayrollAlreadyLockedException.cs
│   │       ├── DuplicatePayrollPeriodException.cs
│   │       └── InvalidPayrollStateException.cs
│   │
│   ├── Engine/                           # Pure calculation — zero side effects
│   │   ├── PPh21Calculator.cs
│   │   ├── BPJSCalculator.cs
│   │   ├── OvertimeCalculator.cs
│   │   ├── ProrateCalculator.cs
│   │   └── TaxBrackets.cs                # Static tax brackets TER 2024
│   │
│   ├── Infrastructure/                   # External concerns
│   │   ├── EventStore/
│   │   │   ├── MartenEventStore.cs
│   │   │   └── MartenConfig.cs
│   │   ├── Projections/
│   │   │   ├── PayrollRunSummaryProjection.cs
│   │   │   ├── EmployeePayslipProjection.cs
│   │   │   └── PayrollLineItemProjection.cs
│   │   ├── Jobs/
│   │   │   ├── PayrollCalculationJob.cs
│   │   │   └── PayslipGenerationJob.cs
│   │   ├── Documents/
│   │   │   └── PayslipDocument.cs        # QuestPDF template
│   │   ├── Export/
│   │   │   ├── BPJSExporter.cs
│   │   │   └── BankFileGenerator.cs
│   │   └── Cache/
│   │       └── SalaryComponentCache.cs
│   │
│   └── ReadModels/                       # Marten projection targets
│       ├── PayrollRunSummary.cs
│       ├── PayrollLineItem.cs
│       ├── EmployeePayslip.cs
│       └── PayrollDisbursementSummary.cs
│
├── tests/
│   ├── Domain.Tests/                     # Unit tests — aggregates & engine
│   ├── Application.Tests/                # Integration tests — MediatR handlers
│   └── Engine.Tests/                     # Unit tests — calculation logic
│
├── frontend/                             # Next.js 14 App Router
│   ├── app/
│   ├── components/
│   └── lib/
│
├── .bob/
│   ├── rules/
│   └── rules-code/
├── AGENTS.md
├── PHASE_PROMPTS.md
└── docker-compose.yml
```

---

## Architecture Overview

```
HTTP Request
    └─> Api/Endpoints
         └─> MediatR.Send(Command/Query)
              ├─> ValidationBehavior (FluentValidation)
              ├─> LoggingBehavior
              └─> Handler
                   ├─> [Command] Load Aggregate via Marten
                   │         └─> Aggregate.DoSomething()
                   │              └─> RaiseEvent(DomainEvent)
                   │                   └─> Marten AppendOptimistic → PostgreSQL
                   │
                   └─> [Query] Read from ReadModels (Marten projection tables)
```

---

## Payroll Run State Machine

```
Draft → Calculating → Calculated → UnderReview → Approved → Locked → Disbursed
          ↑                            │
          └────────────────────────────┘ (rejected → back to Draft)
```

Invariants yang WAJIB di-enforce di aggregate:
- Tidak bisa trigger calculation kalau status bukan Draft
- Tidak bisa approve kalau status bukan UnderReview
- Tidak bisa lock kalau status bukan Approved
- Setelah Locked → TIDAK ADA perubahan state apapun (kecuali Disbursed)
- Satu periode (bulan + tahun) hanya boleh ada satu PayrollRun aktif

---

## Domain Events

| Event | Trigger | Data |
|---|---|---|
| `PayrollRunCreated` | HR buat payroll run baru | period, createdBy |
| `PayrollCalculated` | Background job selesai kalkulasi | lineItems[], totalAmount |
| `PayrollReviewStarted` | HR mulai review | reviewedBy |
| `PayrollApproved` | Finance/Manager approve | approvedBy, notes |
| `PayrollRejected` | Reviewer reject | rejectedBy, reason |
| `PayrollLocked` | Setelah approved, di-lock | lockedBy |
| `PayslipGenerated` | PDF payslip selesai dibuat | employeeId, filePath |
| `DisbursementInitiated` | File bank di-generate | bankFileUrl, totalAmount |
| `DisbursementConfirmed` | HR konfirmasi transfer done | confirmedBy, confirmedAt |

---

## Calculation Engine Rules

### PPh 21 (TER Method 2024)
- Gunakan **Tarif Efektif Rata-rata (TER)** sesuai PMK 168/2023
- PTKP per status: TK/0=54jt, TK/1=58.5jt, K/0=58.5jt, K/1=63jt, K/2=67.5jt, K/3=72jt
- Karyawan tanpa NPWP: tarif lebih tinggi 20%
- **WAJIB pakai `decimal`, TIDAK BOLEH `double` atau `float`**

### BPJS Kesehatan
- Karyawan: 1% dari gaji (cap Rp 12.000.000)
- Perusahaan: 4% dari gaji (cap Rp 12.000.000)

### BPJS Ketenagakerjaan
- JHT Karyawan: 2%, Perusahaan: 3.7%
- JP Karyawan: 1%, Perusahaan: 2% (cap ~Rp 10.042.300)
- JKK Perusahaan: 0.24% (risiko rendah)
- JKM Perusahaan: 0.3%

### Prorate
- Karyawan join di tengah bulan: `(sisa hari kerja / total hari kerja) × gaji`
- Karyawan resign di tengah bulan: sama formula
- Gunakan hari kerja (excludes weekend + libur nasional)

---

## Key Technical Decisions

- **Decimal for money**: Selalu `decimal`, tidak pernah `double`/`float`
- **Value Object Money**: Selalu buat `Money` value object, jangan raw `decimal` untuk representasi uang
- **Immutable Events**: Domain events adalah record, tidak boleh diubah setelah dibuat
- **No direct DB access from Domain**: Domain layer tidak boleh punya dependency ke Marten/EF/apapun
- **Optimistic concurrency via Marten**: Gunakan `AppendOptimistic` untuk prevent race condition
- **Idempotent jobs**: Hangfire job harus idempotent — kalau dijalankan 2x hasilnya sama
- **UUID PKs**: Semua primary key menggunakan `Guid`

---

## Environment Variables

```env
# Backend
ASPNETCORE_ENVIRONMENT=Development
DATABASE_URL=Host=localhost;Database=payroll_db;Username=postgres;Password=postgres
REDIS_URL=localhost:6379
HANGFIRE_DASHBOARD_USER=admin
HANGFIRE_DASHBOARD_PASS=admin
JWT_SECRET=your-secret-here

# Frontend
NEXT_PUBLIC_API_URL=http://localhost:5000
```

---

## Notes untuk IBM Bob

- **Baca file ini sebelum generate kode apapun**
- Ikuti struktur folder di atas — jangan buat layer atau file di luar struktur tanpa alasan
- Domain layer tidak boleh punya `using` ke Infrastructure, Marten, EF, atau library eksternal apapun
- Engine layer hanya boleh punya pure C# — tidak ada DI, tidak ada async, tidak ada external calls
- Untuk money calculation, selalu `decimal` — ini non-negotiable
- Default mock employee: `EMP-001` untuk testing/development
- Kalau ada ambiguitas soal state machine, referensi ke bagian "Payroll Run State Machine" di atas
