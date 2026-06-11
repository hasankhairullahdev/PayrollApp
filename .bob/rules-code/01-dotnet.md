# .NET Rules (Code Mode)

## Project Setup
- Target framework: `net10.0`
- Nullable reference types: enabled (`<Nullable>enable</Nullable>`)
- Implicit usings: enabled
- Treat warnings as errors di Domain dan Engine projects

## MediatR
- Satu file per Command/Query + Handler (tidak dipisah)
- Gunakan `IRequest<Result<T>>` untuk commands, `IRequest<T>` untuk queries
- Pipeline behaviors: ValidationBehavior → LoggingBehavior → TransactionBehavior
- Validation via FluentValidation — satu validator class per command

## Marten — Event Store
- Gunakan `AppendOptimistic` untuk optimistic concurrency — TIDAK BOLEH `AppendUnrestricted`
- Load aggregate: `session.Events.AggregateStreamAsync<TAggregate>(id)`
- Projection type:
  - Single aggregate → `SingleStreamProjection<TReadModel>`
  - Cross-aggregate (multi employee dalam satu payroll run) → `MultiStreamProjection<TReadModel, Guid>`
- Snapshot: aktifkan setelah aggregate punya lebih dari 50 expected events
- Jangan query event stream langsung dari query handler — selalu via read model

## Hangfire
- Semua background job harus idempotent
- Gunakan `[DisableConcurrentExecution]` attribute untuk payroll calculation job
- Job harus bisa di-retry tanpa side effect
- Log progress ke Hangfire job detail: `IJobCancellationToken` + manual progress update

## QuestPDF
- Satu Document class per dokumen type: `PayslipDocument.cs`
- Gunakan fluent API QuestPDF — jangan mix dengan manual PDF generation
- Font: gunakan font yang tersedia di server (Roboto atau Arial)
- Selalu set `DocumentMetadata` (title, author, creation date)

## Minimal API
- Group endpoints per domain: `app.MapGroup("/api/payroll")`
- Gunakan `TypedResults` — jangan `Results<T>`  yang untyped
- Selalu validasi input sebelum send ke MediatR
- Return `ProblemDetails` untuk error responses (RFC 7807)

## FluentValidation
- Satu validator per command/request
- Gunakan `RuleFor` yang spesifik — jangan generic `Must` kalau ada built-in validator
- Async validation boleh untuk cek uniqueness ke DB

## Decimal & Money
- WAJIB `decimal` untuk semua kalkulasi keuangan
- DILARANG `double`, `float`, `int` untuk representasi uang
- Pembulatan: gunakan `Math.Round(value, 0, MidpointRounding.AwayFromZero)` untuk rupiah
- `Money` value object wajib digunakan — jangan expose raw decimal di public API

## Entity Configuration
- Jangan pakai data annotations — gunakan Marten document configuration
- UUID untuk semua Id: `Guid` type, generate di application layer bukan DB

## Logging
- Gunakan structured logging: `_logger.LogInformation("Payroll {PayrollRunId} approved by {ApprovedBy}", id, approvedBy)`
- Jangan log sensitive data (nominal gaji, NPWP, nomor rekening)
- Log level: Debug untuk detail flow, Information untuk business events, Warning untuk recoverable issues, Error untuk exceptions
