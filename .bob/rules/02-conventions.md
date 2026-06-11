# Coding Conventions

## Naming
- Classes, records, enums: `PascalCase`
- Methods, properties: `PascalCase`
- Local variables, parameters: `camelCase`
- Private fields: `_camelCase` (underscore prefix)
- Constants: `PascalCase` (tidak pakai ALL_CAPS)
- Interfaces: prefix `I` → `IPayrollRepository`

## File Organization
- Satu class per file
- Nama file sama dengan nama class: `PayrollRun.cs` untuk class `PayrollRun`
- Folder structure mengikuti namespace

## Error Handling
- Gunakan `Result<T>` pattern untuk business errors di Application layer
- Throw domain exceptions untuk invariant violations di Domain layer
- Jangan swallow exceptions — selalu log atau propagate
- Jangan gunakan exception untuk flow control

## Async/Await
- Semua I/O operations harus async
- Selalu suffix method async dengan `Async`: `GetPayrollRunAsync()`
- Gunakan `CancellationToken` di semua public async methods
- Jangan gunakan `.Result` atau `.Wait()` — selalu `await`

## Dependency Injection
- Gunakan constructor injection
- Jangan gunakan service locator pattern
- Interface untuk semua service yang di-inject

## Testing Conventions
- Nama test: `MethodName_Scenario_ExpectedResult`
- Contoh: `Approve_WhenStatusIsUnderReview_ShouldRaisePayrollApprovedEvent`
- Gunakan Given-When-Then pattern untuk event-sourced aggregate tests
- Engine tests harus pure unit test — tidak ada mock, tidak ada DI
