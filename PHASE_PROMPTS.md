# PHASE_PROMPTS.md — Payroll Application

Gunakan prompt di bawah ini langsung ke IBM Bob per phase.
**Selalu mulai setiap sesi baru dengan membuka AGENTS.md terlebih dahulu.**

---

## Phase 1 — Solution Setup + Domain Layer

**Tujuan**: Scaffold solution .NET, buat Domain layer lengkap: aggregates, events, value objects, exceptions.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md dan semua file di .bob/rules/ sebelum mulai.

Saya membangun Payroll Application (.NET 10 + Next.js 14) dengan Clean Architecture + Event Sourcing menggunakan Marten.

Tolong lakukan Phase 1:

1. Buat solution structure sesuai AGENTS.md:
   - PayrollApp.Api
   - PayrollApp.Application
   - PayrollApp.Domain
   - PayrollApp.Engine
   - PayrollApp.Infrastructure
   - PayrollApp.Tests.Domain
   - PayrollApp.Tests.Engine

2. Domain layer — buat semua files berikut:
   Aggregates:
   - PayrollRun.cs (aggregate root, event-sourced, dengan state machine sesuai AGENTS.md)
   - Employee.cs (aggregate root dasar)
   
   Events (semua sebagai record):
   - PayrollRunCreated, PayrollCalculated, PayrollReviewStarted
   - PayrollApproved, PayrollRejected, PayrollLocked
   - PayslipGenerated, DisbursementInitiated, DisbursementConfirmed
   
   Value Objects (semua sebagai record):
   - Money (decimal, currency, arithmetic operators)
   - TaxCalculation (gross, pph21Amount, netAmount)
   - BPJSComponent (jht, jp, jkk, jkm, kesehatan — employee + employer split)
   - SalaryComponent (componentId, name, amount, type, effectiveDate)
   - EmployeeId (strongly-typed ID wrapper)
   
   Enums:
   - PayrollStatus (Draft, Calculating, Calculated, UnderReview, Approved, Locked, Disbursed)
   - SalaryComponentType (BasicSalary, FixedAllowance, VariableAllowance, Deduction)
   
   Exceptions:
   - PayrollAlreadyLockedException
   - DuplicatePayrollPeriodException
   - InvalidPayrollStateException

3. Enforce semua invariants di PayrollRun aggregate sesuai AGENTS.md state machine

Ikuti aturan di .bob/rules-code/01-dotnet.md.
Pastikan Domain layer zero external dependencies.
```

**Definition of Done:**
- [ ] Solution build tanpa error
- [ ] PayrollRun aggregate punya semua domain methods: `StartCalculation()`, `MarkCalculated()`, `StartReview()`, `Approve()`, `Reject()`, `Lock()`
- [ ] Setiap method enforce state machine invariant (throw exception kalau state salah)
- [ ] Semua events adalah `record`
- [ ] Money value object menggunakan `decimal`, punya arithmetic operators
- [ ] Zero dependency di Domain project (cek `.csproj`)

---

## Phase 2 — Calculation Engine

**Tujuan**: Implementasi pure calculation logic — PPh 21 TER, BPJS, Overtime, Prorate. Fully unit testable.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md dan .bob/rules-code/01-dotnet.md sebelum mulai.

Phase 1 sudah selesai. Lanjut Phase 2 — Calculation Engine.

Buat di project PayrollApp.Engine:

1. TaxBrackets.cs
   - Static data PTKP: TK/0=54jt, TK/1=58.5jt, K/0=58.5jt, K/1=63jt, K/2=67.5jt, K/3=72jt
   - Static data TER brackets sesuai PMK 168/2023

2. PPh21Calculator.cs
   - Method: Calculate(decimal grossAnnual, string ptkpStatus, bool hasNpwp) → TaxCalculation
   - Implementasi metode TER (Tarif Efektif Rata-rata)
   - Kalau tidak punya NPWP: tarif +20%
   - Return monthly tax amount
   - WAJIB pakai decimal, DILARANG double/float

3. BPJSCalculator.cs
   - Method: Calculate(decimal salary) → BPJSComponent
   - Kesehatan: karyawan 1%, perusahaan 4%, cap 12jt
   - JHT: karyawan 2%, perusahaan 3.7%
   - JP: karyawan 1%, perusahaan 2%, cap ~10.042.300
   - JKK: perusahaan 0.24%
   - JKM: perusahaan 0.3%

4. OvertimeCalculator.cs
   - Method: Calculate(decimal basicSalary, int overtimeHours, OvertimeType type) → Money
   - OvertimeType: Weekday, Weekend, Holiday
   - Formula sesuai UU Ketenagakerjaan Indonesia

5. ProrateCalculator.cs
   - Method: Calculate(decimal fullMonthlySalary, DateOnly joinDate, DateOnly period) → Money
   - Hitung berdasarkan hari kerja (exclude weekend)
   - Handle join di tengah bulan dan resign di tengah bulan

Buat juga unit tests lengkap di PayrollApp.Tests.Engine untuk semua calculator.
Gunakan xUnit + Shouldly.
Test harus cover edge cases: gaji di atas cap BPJS, karyawan tanpa NPWP, join di hari pertama/terakhir bulan.
```

**Definition of Done:**
- [ ] Semua calculator pure static methods atau instance tanpa dependencies
- [ ] PPh 21 calculation menghasilkan angka yang benar untuk beberapa test case
- [ ] BPJS calculation respects cap (gaji 20jt harus pakai cap, bukan gaji penuh)
- [ ] Prorate calculation akurat berdasarkan hari kerja
- [ ] Semua unit test pass
- [ ] Zero async di Engine layer
- [ ] Zero `double`/`float` di seluruh Engine layer

---

## Phase 3 — Infrastructure + Marten Setup

**Tujuan**: Setup Marten event store, projections, Hangfire, dan infrastructure layer.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md dan .bob/rules-code/01-dotnet.md sebelum mulai.

Phase 2 sudah selesai. Lanjut Phase 3 — Infrastructure layer.

1. Setup Marten di PayrollApp.Infrastructure:
   - MartenConfig.cs: konfigurasi Marten dengan PostgreSQL
   - Register semua projections
   - Enable inline projection untuk PayrollRunSummary
   - Enable async projection untuk EmployeePayslip
   - Aktifkan Marten CLI untuk migration

2. Buat Read Models di PayrollApp.ReadModels:
   - PayrollRunSummary: id, period, status, totalEmployees, totalAmount, createdAt, approvedBy
   - PayrollLineItem: payrollRunId, employeeId, employeeName, basicSalary, allowances, deductions, bpjs, pph21, takeHomePay
   - EmployeePayslip: semua data untuk generate payslip PDF
   - PayrollDisbursementSummary: payrollRunId, bankName, accountNumber, amount, status

3. Buat Projections:
   - PayrollRunSummaryProjection (SingleStreamProjection) — update dari semua PayrollRun events
   - PayrollLineItemProjection (MultiStreamProjection) — aggregate dari PayrollCalculated events
   - EmployeePayslipProjection (SingleStreamProjection) — dari PayslipGenerated event

4. Setup Hangfire:
   - HangfireConfig.cs: gunakan PostgreSQL storage
   - PayrollCalculationJob.cs: 
     - Terima payrollRunId
     - Load semua employees
     - Jalankan kalkulasi per karyawan menggunakan Engine layer
     - Dispatch PayrollCalculated event via Marten
     - Idempotent: cek dulu apakah sudah calculated
   - PayslipGenerationJob.cs:
     - Generate PDF per karyawan menggunakan QuestPDF
     - Trigger setelah PayrollLocked event

5. Setup Redis cache di SalaryComponentCache.cs:
   - Cache salary components per employee (TTL: 1 jam)
   - Cache tax brackets (TTL: 24 jam, jarang berubah)
   - Invalidate saat ada perubahan komponen gaji

Gunakan AppendOptimistic untuk semua event appending.
```

**Definition of Done:**
- [ ] `docker-compose up` menjalankan PostgreSQL + Redis
- [ ] Marten migration berjalan tanpa error
- [ ] Hangfire dashboard accessible di `/hangfire`
- [ ] PayrollRunSummary projection terupdate saat events disimpan
- [ ] PayrollCalculationJob bisa dijalankan manual via Hangfire dashboard
- [ ] Redis cache bekerja (verifikasi via Redis CLI)

---

## Phase 4 — Application Layer + API

**Tujuan**: MediatR commands/queries, pipeline behaviors, dan minimal API endpoints.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md, .bob/rules/01-architecture.md, dan .bob/rules-code/01-dotnet.md sebelum mulai.

Phase 3 sudah selesai. Lanjut Phase 4 — Application layer dan API.

1. Setup MediatR pipeline behaviors di Application layer:
   - ValidationBehavior (FluentValidation)
   - LoggingBehavior (structured logging)
   - TransactionBehavior (Marten IDocumentSession)

2. Buat Commands + Handlers:

   CreatePayrollRunCommand:
   - Input: period (month, year), createdBy
   - Validasi: period tidak boleh future, tidak ada payroll run aktif untuk period yang sama
   - Handler: buat PayrollRun aggregate → AppendOptimistic → trigger Hangfire calculation job
   
   ApprovePayrollCommand:
   - Input: payrollRunId, approvedBy, notes
   - Validasi: payrollRunId exists, status harus UnderReview
   - Handler: load aggregate → Approve() → AppendOptimistic
   
   LockPayrollCommand:
   - Input: payrollRunId, lockedBy
   - Handler: load aggregate → Lock() → AppendOptimistic → trigger PayslipGenerationJob
   
   InitiateDisbursementCommand:
   - Input: payrollRunId, bankName
   - Handler: generate bank transfer file → simpan → DisbursementInitiated event

3. Buat Queries + Handlers:
   - GetPayrollRunsQuery → List<PayrollRunSummary> (dari Marten read model)
   - GetPayrollSummaryQuery(id) → PayrollRunSummary
   - GetPayrollLineItemsQuery(id) → List<PayrollLineItem>
   - GetPayslipQuery(payrollRunId, employeeId) → EmployeePayslip

4. Buat API endpoints di Api/Endpoints:
   - POST /api/payroll/runs → CreatePayrollRunCommand
   - POST /api/payroll/runs/{id}/approve → ApprovePayrollCommand
   - POST /api/payroll/runs/{id}/lock → LockPayrollCommand
   - POST /api/payroll/runs/{id}/disburse → InitiateDisbursementCommand
   - GET /api/payroll/runs → GetPayrollRunsQuery
   - GET /api/payroll/runs/{id} → GetPayrollSummaryQuery
   - GET /api/payroll/runs/{id}/line-items → GetPayrollLineItemsQuery
   - GET /api/payroll/runs/{id}/payslip/{employeeId} → GetPayslipQuery

Semua endpoint return ProblemDetails untuk error.
Gunakan TypedResults.
```

**Definition of Done:**
- [ ] Semua endpoints bisa ditest via Swagger/HTTP file
- [ ] CreatePayrollRun → automatically trigger background calculation
- [ ] Approve + Lock enforce state machine (coba approve dari Draft → harus error)
- [ ] Query endpoints return data dari read models (bukan event stream)
- [ ] ValidationBehavior reject invalid commands sebelum sampai ke handler
- [ ] Integration test untuk happy path: Create → Calculate → Review → Approve → Lock

---

## Phase 5 — Next.js Frontend

**Tujuan**: Build dashboard payroll yang clean dan functional.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md dan .bob/rules-code/02-nextjs.md sebelum mulai.

Phase 4 sudah selesai. Backend fully working. Lanjut Phase 5 — Next.js frontend.

Setup Next.js 14 (App Router, TypeScript strict, Tailwind, Shadcn/ui).

Halaman yang perlu dibuat:

1. /payroll — Payroll Run List
   - Tabel semua payroll runs dengan status badge berwarna
   - Kolom: Period, Status, Total Employees, Total Amount, Created At, Actions
   - Button "Buat Payroll Run Baru" → dialog/modal
   - Filter by status
   - TanStack Query untuk fetching + refetch setiap 5 detik (realtime calculation progress)

2. /payroll/[id] — Payroll Run Detail
   - Header: period, status, total amount, action buttons sesuai status
   - Tab 1: Line Items — tabel per karyawan (nama, gaji pokok, tunjangan, BPJS, PPh21, take home)
   - Tab 2: Summary — pie chart breakdown komponen gaji
   - Tab 3: Timeline — history events payroll run ini
   - Action buttons kontekstual:
     - Status Calculated → "Mulai Review"
     - Status UnderReview → "Approve" / "Reject"
     - Status Approved → "Lock & Generate Payslip"
     - Status Locked → "Generate Bank File"

3. /employees — Employee List
   - Tabel karyawan dengan komponen gaji
   - Bisa add/edit salary components per karyawan

4. Komponen shared:
   - StatusBadge: warna per status (Draft=gray, Calculating=blue, Approved=green, Locked=purple, dll)
   - MoneyDisplay: format Rupiah (Rp 15.000.000)
   - PayrollPeriodDisplay: "Juli 2025"
   - ConfirmDialog: reusable confirmation modal

Desain: clean, professional, dark sidebar + light content area.
Warna aksen: biru IBM (#0043CE) atau slate biru.
```

**Definition of Done:**
- [ ] Halaman list payroll runs tampil dengan data dari backend
- [ ] Status badge berwarna sesuai status
- [ ] Bisa create payroll run baru via modal
- [ ] Detail page tampil line items per karyawan
- [ ] Action buttons hanya tampil sesuai status yang relevan
- [ ] Format rupiah konsisten di seluruh app
- [ ] Responsive di mobile dan desktop

---

## Phase 6 — Reporting & Polish

**Tujuan**: PDF payslip, Excel export, error handling, dan persiapan demo.

**Prompt untuk IBM Bob:**
```
Baca AGENTS.md sebelum mulai.

Phase 5 sudah selesai. Phase 6 — Reporting dan polish final.

1. QuestPDF Payslip:
   - PayslipDocument.cs: template payslip profesional
   - Header: logo perusahaan placeholder, nama karyawan, periode
   - Body: tabel breakdown gaji (pokok, tunjangan, potongan, BPJS, PPh21)
   - Footer: take home pay yang besar dan jelas, tanda tangan HR
   - Endpoint: GET /api/payroll/runs/{id}/payslip/{employeeId}/pdf → file download

2. ClosedXML Excel Export:
   - GET /api/payroll/runs/{id}/export/excel → file download
   - Sheet 1: Summary per karyawan
   - Sheet 2: Rekap BPJS (format yang bisa diupload ke portal BPJS)
   - Sheet 3: Rekap PPh 21

3. Bank File Generator:
   - Format BCA: fixed-width text file
   - Format Mandiri: CSV dengan format spesifik
   - Download via: GET /api/payroll/runs/{id}/bank-file?bank=bca

4. Error handling polish:
   - Global exception middleware → ProblemDetails RFC 7807
   - Frontend: toast notifications untuk semua error
   - Loading skeleton untuk semua tabel dan card

5. docker-compose.yml lengkap:
   - Services: api (.NET), frontend (Next.js), postgres, redis, hangfire
   - Health checks
   - Volume untuk PostgreSQL data

6. README.md:
   - Setup instructions
   - Demo flow step-by-step
   - Tech stack + architecture diagram (ASCII)
   - Screenshot placeholders
```

**Definition of Done:**
- [ ] PDF payslip bisa di-download dan tampil dengan benar
- [ ] Excel export berhasil dengan 3 sheets
- [ ] Bank file BCA dan Mandiri bisa di-generate
- [ ] `docker-compose up` menjalankan full stack
- [ ] Error handling konsisten backend dan frontend
- [ ] README bisa diikuti dari nol sampai demo berjalan

---

## Tips Penggunaan IBM Bob

1. **Buka AGENTS.md di awal setiap sesi** — Bob tidak ingat sesi sebelumnya
2. **Satu phase per sesi** — jangan gabung, konteks Bob terbatas
3. **Kalau Bob nyasar struktur folder**, tulis: *"Cek AGENTS.md bagian Repository Structure, taruh file di lokasi yang benar"*
4. **Kalau Bob pakai double/float untuk uang**, tulis: *"Cek .bob/rules-code/01-dotnet.md bagian Decimal & Money, ganti semua ke decimal"*
5. **Kalau Bob langsung set state di aggregate**, tulis: *"Cek .bob/rules/01-architecture.md bagian Event Sourcing, state hanya boleh diubah via Apply(event)"*
6. **Untuk iterasi kecil**, cukup: *"Baca AGENTS.md. Update [namafile] untuk [perubahan spesifik]"*
