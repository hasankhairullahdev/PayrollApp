# Architecture Rules

## Clean Architecture — Dependency Rule
- `Domain` tidak boleh depend ke layer manapun (zero external dependencies)
- `Engine` tidak boleh depend ke layer manapun (pure C# only)
- `Application` boleh depend ke `Domain` dan `Engine` saja
- `Infrastructure` boleh depend ke `Application` dan `Domain`
- `Api` boleh depend ke `Application` saja — tidak boleh langsung ke `Domain`

## Event Sourcing
- Aggregate state HANYA boleh diubah melalui `Apply(DomainEvent)` method
- `Apply` method harus private atau protected
- Method publik di aggregate (misal `Approve()`, `Lock()`) hanya boleh memanggil `RaiseEvent()`
- Jangan pernah langsung set property di aggregate dari luar — harus lewat command → event → apply
- Events adalah append-only — tidak ada UPDATE atau DELETE ke event store

## CQRS
- Command handler: load aggregate → call domain method → save events via Marten
- Query handler: baca dari ReadModels (projection tables) — TIDAK BOLEH baca dari event stream langsung
- Jangan campur command logic dan query logic dalam satu handler

## Domain Events
- Semua domain events adalah `record` — immutable
- Nama event selalu past tense: `PayrollApproved`, bukan `ApprovePayroll`
- Event harus membawa semua data yang dibutuhkan untuk reconstruct state — jangan lazy load

## Value Objects
- `Money` selalu pakai `decimal` — tidak boleh `double` atau `float`
- Value objects adalah `record` — immutable, equality by value
- Jangan expose raw `decimal` untuk representasi uang — selalu wrap dalam `Money`

## State Machine Invariants
- Enforce invariants di dalam aggregate, bukan di handler
- Kalau invariant violated, throw domain exception (bukan generic exception)
- Contoh: `throw new InvalidPayrollStateException("Cannot approve payroll in Draft status")`
