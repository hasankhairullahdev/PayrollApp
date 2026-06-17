# CONTEXT.md - Single Source of Truth

**Last Updated**: 2026-06-15 15:26 WIB
**Project Status**: ✅ PRODUCTION READY (5 Critical Bugs Fixed)
**Purpose**: Comprehensive context untuk AI agents di new chat sessions

---

## 🎯 Project Overview

**Payroll Application** adalah enterprise payroll system untuk mengelola penggajian karyawan secara end-to-end dengan Clean Architecture, CQRS, dan Event Sourcing.

**Tech Stack:**
- Backend: .NET 10 Web API + Marten (Event Store) + Hangfire + QuestPDF + ClosedXML
- Frontend: Next.js 14 (App Router) + TypeScript + TanStack Query + Recharts
- Database: PostgreSQL 16
- Cache: Redis (optional)

---

## 📂 Complete Project Structure

```
payroll-app/
├── src/
│   ├── PayrollApp.Api/                    # Minimal API endpoints
│   │   ├── Endpoints/
│   │   │   ├── PayrollEndpoints.cs        # Payroll CRUD + actions
│   │   │   ├── EmployeeEndpoints.cs       # Employee CRUD
│   │   │   ├── ReportEndpoints.cs         # PDF, Excel, Bank files
│   │   │   └── EventEndpoints.cs          # Event timeline
│   │   ├── Program.cs                     # DI, Marten, Hangfire config
│   │   └── SeedData.cs                    # Initial employee data
│   │
│   ├── PayrollApp.Application/            # MediatR Commands & Queries
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs      # FluentValidation pipeline
│   │   │   ├── LoggingBehavior.cs         # Logging pipeline
│   │   │   └── TransactionBehavior.cs     # Transaction pipeline
│   │   ├── Common/
│   │   │   └── Result.cs                  # Result<T> pattern
│   │   ├── Payroll/
│   │   │   ├── Commands/
│   │   │   │   ├── CreatePayrollRunCommand.cs
│   │   │   │   ├── StartReviewCommand.cs
│   │   │   │   ├── ApprovePayrollCommand.cs
│   │   │   │   ├── RejectPayrollCommand.cs
│   │   │   │   └── LockPayrollCommand.cs
│   │   │   └── Queries/
│   │   │       ├── GetPayrollRunsQuery.cs
│   │   │       └── GetPayrollRunDetailQuery.cs
│   │   └── Employees/
│   │       ├── Commands/
│   │       │   ├── CreateEmployeeCommand.cs
│   │       │   └── CreateEmployeeCommandHandler.cs
│   │       └── Queries/
│   │           ├── GetEmployeesQuery.cs
│   │           └── GetEmployeesQueryHandler.cs
│   │
│   ├── PayrollApp.Domain/                 # Pure domain - NO external deps
│   │   ├── Aggregates/
│   │   │   ├── PayrollRun.cs              # Main aggregate root
│   │   │   └── Employee.cs                # Employee aggregate
│   │   ├── Events/                        # Domain events (immutable records)
│   │   │   ├── PayrollRunCreated.cs
│   │   │   ├── PayrollCalculationStarted.cs
│   │   │   ├── PayrollCalculated.cs
│   │   │   ├── PayrollReviewStarted.cs
│   │   │   ├── PayrollApproved.cs
│   │   │   ├── PayrollRejected.cs
│   │   │   ├── PayrollLocked.cs
│   │   │   ├── PayslipGenerated.cs
│   │   │   ├── DisbursementInitiated.cs
│   │   │   └── DisbursementConfirmed.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs                   # Decimal wrapper
│   │   │   ├── TaxCalculation.cs
│   │   │   ├── BPJSComponent.cs
│   │   │   ├── SalaryComponent.cs
│   │   │   ├── PayrollLineItem.cs
│   │   │   └── EmployeeId.cs
│   │   ├── Enums/
│   │   │   ├── PayrollStatus.cs
│   │   │   └── SalaryComponentType.cs
│   │   └── Exceptions/
│   │       ├── PayrollAlreadyLockedException.cs
│   │       ├── DuplicatePayrollPeriodException.cs
│   │       └── InvalidPayrollStateException.cs
│   │
│   ├── PayrollApp.Engine/                 # Pure calculation - NO side effects
│   │   ├── PPh21Calculator.cs             # TER 2024 implementation
│   │   ├── BPJSCalculator.cs              # Kesehatan + Ketenagakerjaan
│   │   ├── OvertimeCalculator.cs
│   │   ├── ProrateCalculator.cs           # Join/resign mid-month
│   │   └── TaxBrackets.cs                 # Static TER brackets
│   │
│   ├── PayrollApp.Infrastructure/
│   │   ├── EventStore/
│   │   │   ├── MartenConfig.cs            # Event store + projections
│   │   │   └── MartenEventStore.cs
│   │   ├── Projections/
│   │   │   ├── PayrollRunSummaryProjection.cs
│   │   │   └── PayrollLineItemProjection.cs
│   │   ├── ReadModels/
│   │   │   ├── PayrollRunSummary.cs
│   │   │   ├── PayrollLineItem.cs
│   │   │   ├── EmployeePayslip.cs
│   │   │   └── PayrollDisbursementSummary.cs
│   │   ├── Jobs/
│   │   │   ├── HangfireConfig.cs
│   │   │   ├── PayrollCalculationJob.cs   # Background calculation
│   │   │   └── PayslipGenerationJob.cs    # PDF generation
│   │   ├── Documents/
│   │   │   └── PayslipDocument.cs         # QuestPDF template
│   │   ├── Export/
│   │   │   ├── PayrollExcelExporter.cs    # ClosedXML - 3 sheets
│   │   │   └── BankFileGenerator.cs       # 4 bank formats
│   │   └── Repositories/
│   │       └── EmployeeRepository.cs
│   │
│   └── tests/
│       ├── PayrollApp.Tests.Domain/
│       ├── PayrollApp.Tests.Engine/
│       └── PayrollApp.Tests.Application/
│
├── frontend/                              # Next.js 14 App Router
│   ├── app/
│   │   ├── layout.tsx                     # Root layout
│   │   ├── page.tsx                       # Home page
│   │   ├── providers.tsx                  # TanStack Query provider
│   │   ├── employees/
│   │   │   └── page.tsx                   # Employee list + CRUD
│   │   └── payroll/
│   │       ├── page.tsx                   # Payroll list
│   │       └── [id]/
│   │           └── page.tsx               # Payroll detail (3 tabs)
│   ├── components/
│   │   ├── ui/                            # Shadcn components
│   │   ├── Sidebar.tsx                    # Navigation
│   │   ├── StatusBadge.tsx                # Status display
│   │   ├── MoneyDisplay.tsx               # Currency formatter
│   │   ├── PayrollPeriodDisplay.tsx       # Period formatter
│   │   ├── EmployeeDialog.tsx             # Employee CRUD dialog
│   │   ├── CreatePayrollRunDialog.tsx     # Create payroll dialog
│   │   ├── PayrollActionDialog.tsx        # Universal action dialog
│   │   ├── PayrollTimeline.tsx            # Event timeline
│   │   ├── PayrollSummaryCharts.tsx       # 3 pie charts (Recharts)
│   │   └── ConfirmDialog.tsx              # Confirmation dialog
│   ├── lib/
│   │   ├── api.ts                         # API client (axios)
│   │   └── utils.ts                       # Utility functions
│   └── package.json                       # Dependencies
│
├── .bob/                                  # AI agent rules
│   ├── rules/
│   │   ├── 01-architecture.md
│   │   └── 02-conventions.md
│   └── skills/
│       ├── frontend-design/
│       └── vercel-react-best-practices/
│
├── AGENTS.md                              # Project overview
├── PHASE_PROMPTS.md                       # Development phases
├── CONTEXT.md                             # This file (single source of truth)
├── README.md                              # User documentation
├── docker-compose.yml                     # PostgreSQL + Redis
├── start.ps1                              # Start all services
└── stop.ps1                               # Stop all services
```

---

## 🔄 Payroll State Machine

```
Draft → Calculating → Calculated → UnderReview → Approved → Locked → Disbursed
         (auto)        (manual)      (manual)     (manual)   (auto)
                                         ↓
                                      Rejected
                                         ↓
                                      Draft
```

**State Transitions:**
1. **Draft → Calculating**: Automatic via Hangfire job trigger
2. **Calculating → Calculated**: Automatic when job completes
3. **Calculated → UnderReview**: Manual via `StartReviewCommand`
4. **UnderReview → Approved**: Manual via `ApprovePayrollCommand` (with notes)
5. **UnderReview → Rejected**: Manual via `RejectPayrollCommand` (with reason) → back to Draft
6. **Approved → Locked**: Manual via `LockPayrollCommand` (triggers PayslipGenerationJob)
7. **Locked → Disbursed**: Manual confirmation (future feature)

**Invariants (enforced in PayrollRun aggregate):**
- Cannot start review if status != Calculated
- Cannot approve/reject if status != UnderReview
- Cannot lock if status != Approved
- After Locked, NO state changes allowed (except Disbursed)
- One period (month + year) can only have ONE active PayrollRun

---

## 💾 Database Schema (Marten Projections)

### PayrollRunSummary (Read Model)
```csharp
{
    Id: Guid,
    Month: int,
    Year: int,
    Status: PayrollStatus,
    TotalAmount: decimal,
    EmployeeCount: int,
    CreatedAt: DateTime,
    CreatedBy: string,
    ApprovedAt: DateTime?,
    ApprovedBy: string?,
    LockedAt: DateTime?,
    LockedBy: string?
}
```

### PayrollLineItem (Read Model)
```csharp
{
    Id: Guid,
    PayrollRunId: Guid,
    EmployeeId: Guid,              // ✅ FIXED: Changed from string to Guid
    EmployeeCode: string,          // ✅ FIXED: Added separate field for employee code
    EmployeeName: string,
    BasicSalary: decimal,
    Allowances: decimal,
    Overtime: decimal,
    GrossSalary: decimal,
    BpjsKesehatan: decimal,
    BpjsKetenagakerjaan: decimal,
    Pph21: decimal,
    Deductions: decimal,
    TakeHomePay: decimal
}
```

### Employee (Aggregate)
```csharp
{
    Id: Guid,
    EmployeeCode: string,
    FullName: string,
    Email: string,
    Npwp: string?,
    PtkpStatus: string,  // TK/0, K/1, K/2, K/3
    JoinDate: DateTime,
    ResignDate: DateTime?,
    IsActive: bool,
    SalaryComponents: List<SalaryComponent>
}
```

---

## 🧮 Calculation Rules

### PPh 21 (TER Method 2024)
- Tarif Efektif Rata-rata sesuai PMK 168/2023
- PTKP: TK/0=54jt, TK/1=58.5jt, K/0=58.5jt, K/1=63jt, K/2=67.5jt, K/3=72jt
- Non-NPWP: tarif +20%
- **ALWAYS use `decimal`, NEVER `double` or `float`**

### BPJS Kesehatan
- Karyawan: 1% (cap Rp 12.000.000)
- Perusahaan: 4% (cap Rp 12.000.000)

### BPJS Ketenagakerjaan
- JHT: Karyawan 2%, Perusahaan 3.7%
- JP: Karyawan 1%, Perusahaan 2% (cap ~Rp 10.042.300)
- JKK: Perusahaan 0.24% (risiko rendah)
- JKM: Perusahaan 0.3%

### Prorate
- Formula: `(sisa hari kerja / total hari kerja) × gaji`
- Gunakan hari kerja (exclude weekend + libur nasional)

---

## 🎨 Frontend Architecture

### Pages & Routes
1. **`/`** - Home page (redirect to /payroll)
2. **`/employees`** - Employee list with CRUD
3. **`/payroll`** - Payroll list with filters
4. **`/payroll/[id]`** - Payroll detail with 3 tabs:
   - **Line Items**: Employee salary breakdown table
   - **Summary**: Financial summary + 3 pie charts
   - **Timeline**: Event history visualization

### Key Components

**EmployeeDialog.tsx**
- Universal dialog for Create/Edit employee
- Form validation with Zod
- Salary components management
- PTKP status selection

**PayrollActionDialog.tsx**
- Universal dialog for all payroll actions
- Dynamic config per action type:
  - `start-review`: Simple confirmation
  - `approve`: With notes input
  - `reject`: With reason input (required)
  - `lock`: Confirmation with warning

**PayrollTimeline.tsx**
- Fetches events from `/api/events/payroll/{id}`
- Event-specific icons and colors
- Formatted timestamps (Indonesian locale)
- Event data parsing and display

**PayrollSummaryCharts.tsx**
- 3 interactive pie charts using Recharts:
  1. Income Breakdown (Basic, Allowances, Overtime)
  2. Deduction Breakdown (BPJS, PPh 21)
  3. Net vs Deductions
- Tooltips with formatted currency
- Percentage calculations
- Responsive design

### API Client (lib/api.ts)

**Payroll APIs:**
```typescript
payrollApi.getPayrollRuns(status?, page?, pageSize?)
payrollApi.getPayrollRunDetail(id)
payrollApi.getPayrollLineItems(id)
payrollApi.createPayrollRun(data)
payrollApi.startReviewPayrollRun(id, data)
payrollApi.approvePayrollRun(id, data)
payrollApi.rejectPayrollRun(id, data)
payrollApi.lockPayrollRun(id, data)
payrollApi.downloadPayslipPdf(payrollRunId, employeeId): Blob
payrollApi.exportPayrollExcel(payrollRunId): Blob
payrollApi.generateBankFile(payrollRunId, bank): Blob
payrollApi.getPayrollEvents(payrollRunId): PayrollEvent[]
```

**Employee APIs:**
```typescript
employeeApi.getEmployees(search?, page?, pageSize?)
employeeApi.createEmployee(data)
employeeApi.updateEmployee(id, data)
employeeApi.deleteEmployee(id)
```

### TanStack Query Configuration
```typescript
{
  staleTime: 5 * 60 * 1000,  // 5 minutes
  refetchOnWindowFocus: false,
  refetchOnMount: false,
  // NO refetchInterval - prevents continuous polling
}
```

### Performance Optimizations Applied
- ✅ `useMemo` for derived state (totals, filtered data)
- ✅ `useCallback` for stable event handlers
- ✅ Proper dependency arrays
- ✅ Conditional queries (`enabled` flag)
- ✅ Optimistic updates for mutations
- ✅ Proper error boundaries

---

## 🔌 API Endpoints Reference

### Payroll Management
```
POST   /api/payroll                         # Create payroll run
GET    /api/payroll?status=&page=&pageSize= # List with filters
GET    /api/payroll/{id}                    # Get detail
GET    /api/payroll/{id}/line-items         # Get line items
POST   /api/payroll/{id}/start-review       # Start review
POST   /api/payroll/{id}/approve            # Approve (with notes)
POST   /api/payroll/{id}/reject             # Reject (with reason)
POST   /api/payroll/{id}/lock               # Lock & generate payslips
```

### Employee Management
```
GET    /api/employees?search=&page=&pageSize=  # List with filters
POST   /api/employees                          # Create
GET    /api/employees/{id}                     # Get detail
PUT    /api/employees/{id}                     # Update
DELETE /api/employees/{id}                     # Delete
```

### Reports & Export
```
GET    /api/reports/payroll/{id}/payslip/{employeeId}/pdf  # PDF payslip
GET    /api/reports/payroll/{id}/export/excel              # Excel (3 sheets)
GET    /api/reports/payroll/{id}/bank-file?bank=bca        # Bank file
```

### Event Timeline
```
GET    /api/events/payroll/{id}  # Get event history
```

### Health & Monitoring
```
GET    /health                   # API health check
GET    /hangfire                 # Background jobs dashboard
```

---

## 🏗️ Architecture Principles

### Clean Architecture Rules
1. **Domain Layer**: Zero external dependencies, pure C#
2. **Engine Layer**: Pure calculation, no side effects, no async
3. **Application Layer**: MediatR handlers, FluentValidation
4. **Infrastructure Layer**: Marten, Hangfire, QuestPDF, ClosedXML
5. **API Layer**: Minimal API endpoints only

### CQRS Pattern
- **Commands**: Modify state, return `Result<T>`
- **Queries**: Read data, return DTOs
- **Handlers**: One handler per command/query
- **Behaviors**: Validation → Logging → Transaction

### Event Sourcing
- Events are append-only (NO UPDATE/DELETE)
- Aggregate state reconstructed from events
- Optimistic concurrency via Marten `AppendOptimistic`
- Projections for read models

### Domain Events (all are `record`)
```csharp
PayrollRunCreated(Guid Id, int Month, int Year, string CreatedBy)
PayrollCalculationStarted(Guid Id, DateTime StartedAt)
PayrollCalculated(Guid Id, int TotalEmployees, decimal TotalAmount)
PayrollReviewStarted(Guid Id, string ReviewedBy)
PayrollApproved(Guid Id, string ApprovedBy, string? Notes)
PayrollRejected(Guid Id, string RejectedBy, string Reason)
PayrollLocked(Guid Id, string LockedBy)
PayslipGenerated(Guid Id, string EmployeeId, string FilePath)
DisbursementInitiated(Guid Id, string BankFileUrl, decimal TotalAmount)
DisbursementConfirmed(Guid Id, string ConfirmedBy, DateTime ConfirmedAt)
```

---

## 🎯 Common Tasks for AI Agents

### Adding New Command
1. Create `XxxCommand.cs` in `Application/Payroll/Commands/`
2. Create `XxxCommandHandler.cs` implementing `IRequestHandler<XxxCommand, Result<T>>`
3. Add validator `XxxCommandValidator.cs` with FluentValidation
4. Load aggregate via Marten, call domain method, save events
5. Add endpoint in `PayrollEndpoints.cs`
6. Add API client function in `frontend/lib/api.ts`
7. Add UI button/action in frontend

### Adding New Query
1. Create `XxxQuery.cs` in `Application/Payroll/Queries/`
2. Create `XxxQueryHandler.cs` implementing `IRequestHandler<XxxQuery, T>`
3. Query from read models (projections), NOT event stream
4. Add endpoint in `PayrollEndpoints.cs`
5. Add API client function in `frontend/lib/api.ts`
6. Use TanStack Query in frontend component

### Adding New Domain Event
1. Create `XxxEvent.cs` as `record` in `Domain/Events/`
2. Add `Apply(XxxEvent e)` method in aggregate
3. Call `RaiseEvent(new XxxEvent(...))` from domain method
4. Update projection if needed in `Infrastructure/Projections/`
5. Update timeline display in `PayrollTimeline.tsx`

### Modifying UI
1. Check existing components in `frontend/components/`
2. Follow design system (Tailwind + custom colors)
3. Use TanStack Query for data fetching
4. Apply performance optimizations (useMemo, useCallback)
5. Ensure responsive design (mobile to desktop)
6. Test build: `npm run build`

---

## 🐛 Common Issues & Solutions

### ✅ FIXED: Bug #1 - Update Employee Salary Components (Reflection Anti-Pattern)
**Problem**: UpdateEmployeeCommand menggunakan reflection untuk update Employee properties, melanggar domain encapsulation.
**Solution**: Tambah domain methods `UpdateBasicInfo()` dan `UpdateSalaryComponents()` di Employee aggregate. Handler sekarang call domain methods.
**Files Changed**:
- `src/PayrollApp.Domain/Aggregates/Employee.cs` - Added domain methods
- `src/PayrollApp.Application/Employees/Commands/UpdateEmployeeCommand.cs` - Removed reflection

### ✅ FIXED: Bug #2 - Form Create Employee Terisi Data Edit
**Problem**: useEffect di EmployeeDialog tidak reset form saat switch dari mode edit ke create.
**Solution**: Tambah `isOpen` ke dependency array dan call `resetForm()` untuk mode create.
**Files Changed**: `frontend/components/EmployeeDialog.tsx`

### ✅ FIXED: Bug #3 - ValidationBehavior Reflection Error
**Problem**: `AmbiguousMatchException` saat resolve generic Validate method di FluentValidation.
**Solution**: Gunakan `GetMethods().Where().FirstOrDefault()` untuk proper generic method resolution.
**Files Changed**: `src/PayrollApp.Application/Behaviors/ValidationBehavior.cs`

### ✅ FIXED: Bug #4 - Employee Tidak Muncul di GET
**Problem**: Employee aggregate tidak di-register sebagai Marten document, sehingga tidak bisa di-query.
**Solution**: Tambah `opts.RegisterDocumentType<Employee>()` di MartenConfig.
**Files Changed**: `src/PayrollApp.Infrastructure/EventStore/MartenConfig.cs`

### ✅ FIXED: Bug #5 - EmployeeId Mismatch di PayrollLineItem
**Problem**: Domain PayrollLineItem hanya punya `EmployeeId` (string), tidak ada `EmployeeCode`. Projection salah map Guid.ToString() sebagai employee code, sehingga employee code tidak match.
**Solution**:
- Tambah field `EmployeeCode` (string) di domain PayrollLineItem
- Ubah `EmployeeId` dari string ke Guid
- Update constructor untuk accept kedua parameter
- PayrollCalculationJob pass `employee.Id` dan `employee.EmployeeCode`
- Projection map dengan benar
**Files Changed**:
- `src/PayrollApp.Domain/ValueObjects/PayrollLineItem.cs` - Added EmployeeCode field, changed EmployeeId to Guid
- `src/PayrollApp.Infrastructure/Jobs/PayrollCalculationJob.cs` - Pass both ID and code, removed manual read model creation
- `src/PayrollApp.Infrastructure/Projections/PayrollLineItemProjection.cs` - Fixed mapping
- `src/PayrollApp.Infrastructure/Jobs/PayslipGenerationJob.cs` - Fixed Guid to string conversion

### Issue: "Cannot approve payroll in Draft status"
**Solution**: Check state machine. Must be in UnderReview status. Use StartReviewCommand first.

### Issue: "Duplicate payroll period"
**Solution**: Check if payroll run for same month+year already exists. Delete or use different period.

### Issue: TypeScript error in Recharts
**Solution**: Use `(value) => formatCurrency(Number(value))` for formatter, `(percent || 0)` for label.

### Issue: Continuous API refetch
**Solution**: Remove `refetchInterval`, use `staleTime: 5 * 60 * 1000` instead.

### Issue: Money calculation precision
**Solution**: ALWAYS use `decimal`, NEVER `double` or `float`. Use `Money` value object.

### Issue: Event not appearing in timeline
**Solution**: Check if event is raised in aggregate, check projection, check EventEndpoints.

---

## 📦 Dependencies

### Backend (.NET 10)
```xml
<PackageReference Include="Marten" Version="7.x" />
<PackageReference Include="MediatR" Version="12.x" />
<PackageReference Include="FluentValidation" Version="11.x" />
<PackageReference Include="Hangfire" Version="1.8.x" />
<PackageReference Include="QuestPDF" Version="2024.x" />
<PackageReference Include="ClosedXML" Version="0.102.x" />
```

### Frontend (Next.js 14)
```json
{
  "@tanstack/react-query": "^5.x",
  "axios": "^1.x",
  "recharts": "^2.x",
  "react-hook-form": "^7.x",
  "zod": "^3.x",
  "tailwindcss": "^3.x"
}
```

---

## 🚀 Quick Commands

```bash
# Backend
dotnet build src/PayrollApp.Api/PayrollApp.Api.csproj
dotnet run --project src/PayrollApp.Api
dotnet test

# Frontend
cd frontend
npm install
npm run dev
npm run build

# Docker
docker-compose up -d
docker-compose down

# PowerShell Scripts
.\start.ps1   # Start all services
.\stop.ps1    # Stop all services
```

---

## 📝 Important Notes for AI Agents

1. **ALWAYS read this file first** before making any changes
2. **Follow Clean Architecture rules** - no shortcuts
3. **Use decimal for money** - this is non-negotiable
4. **Events are immutable** - use `record`, never modify
5. **State machine is strict** - enforce invariants in aggregate
6. **Frontend uses TanStack Query** - proper caching and optimistic updates
7. **Performance matters** - use useMemo, useCallback appropriately
8. **Test builds** - both backend and frontend before completion
9. **Update this file** if you add major features
10. **Read AGENTS.md and PHASE_PROMPTS.md** for additional context

---

## 🎓 Learning Resources

- Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- CQRS: https://martinfowler.com/bliki/CQRS.html
- Event Sourcing: https://martinfowler.com/eaaDev/EventSourcing.html
- Marten: https://martendb.io/
- Next.js 14: https://nextjs.org/docs
- TanStack Query: https://tanstack.com/query/latest

---

**END OF CONTEXT.md**

This file should be the FIRST thing any AI agent reads when starting work on this project.