# Stop Payroll Application
Write-Host "🛑 Stopping Payroll Application..." -ForegroundColor Red

# Stop .NET API (if running)
Write-Host "`n⏹️  Stopping .NET API..." -ForegroundColor Yellow
$dotnetProcess = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*PayrollApp.Api*" }
if ($dotnetProcess) {
    Stop-Process -Id $dotnetProcess.Id -Force
    Write-Host "✅ .NET API stopped" -ForegroundColor Green
} else {
    Write-Host "ℹ️  .NET API is not running" -ForegroundColor Gray
}

# Stop PostgreSQL and Redis using podman compose
Write-Host "`n📦 Stopping PostgreSQL and Redis..." -ForegroundColor Yellow
podman compose down

Write-Host "`n✅ All services stopped" -ForegroundColor Green
Write-Host "💡 To remove volumes, run: podman compose down -v" -ForegroundColor Cyan

# Made with Bob
