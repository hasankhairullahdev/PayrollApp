# Start Payroll Application
Write-Host "🚀 Starting Payroll Application..." -ForegroundColor Green

# Start PostgreSQL and Redis using podman compose
Write-Host "`n📦 Starting PostgreSQL and Redis..." -ForegroundColor Cyan
podman compose up -d

# Wait for services to be healthy
Write-Host "`n⏳ Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check if services are running
$postgresRunning = podman ps --filter "name=payroll_postgres" --format "{{.Names}}"
$redisRunning = podman ps --filter "name=payroll_redis" --format "{{.Names}}"

if ($postgresRunning -and $redisRunning) {
    Write-Host "✅ PostgreSQL and Redis are running" -ForegroundColor Green
    
    # Start .NET API in new window
    Write-Host "`n🔧 Starting .NET API in new window..." -ForegroundColor Cyan
    Write-Host "API will be available at: https://localhost:5044" -ForegroundColor Yellow
    Write-Host "Swagger UI: https://localhost:5044/swagger" -ForegroundColor Yellow
    Write-Host "Hangfire Dashboard: https://localhost:5044/hangfire" -ForegroundColor Yellow
    
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD/src/PayrollApp.Api'; dotnet run"
    
    # Start Next.js Frontend in new window
    Write-Host "`n🎨 Starting Next.js Frontend in new window..." -ForegroundColor Cyan
    Write-Host "Frontend will be available at: http://localhost:3000" -ForegroundColor Yellow
    
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD/frontend'; npm run dev"
    
    Write-Host "`n✅ All services started!" -ForegroundColor Green
    Write-Host "📱 Frontend: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "🔧 Backend API: https://localhost:5044" -ForegroundColor Cyan
    Write-Host "📊 Hangfire: https://localhost:5044/hangfire" -ForegroundColor Cyan
    Write-Host "💡 Run ./stop.ps1 to stop all services" -ForegroundColor Yellow
} else {
    Write-Host "❌ Failed to start services" -ForegroundColor Red
    Write-Host "PostgreSQL: $postgresRunning" -ForegroundColor Red
    Write-Host "Redis: $redisRunning" -ForegroundColor Red
}

# Made with Bob
