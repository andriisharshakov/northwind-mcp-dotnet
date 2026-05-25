# test-mcp.ps1
# Runs API + MCP Server + Inspector in separate windows

Write-Host "Starting Northwind CRM API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", 
    "cd '$PSScriptRoot\src\NorthwindCrm.Api'; dotnet run"

Write-Host "Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "Starting MCP Server..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", 
    "cd '$PSScriptRoot\src\NorthwindCrm.Mcp'; dotnet run"

Write-Host "Waiting for MCP Server to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "Starting MCP Inspector..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "cd '$PSScriptRoot'; npm run inspector"

Write-Host ""
Write-Host "All services started!" -ForegroundColor Cyan
Write-Host "  API:      http://localhost:5185/swagger" -ForegroundColor White
Write-Host "  MCP:      http://localhost:5194/sse" -ForegroundColor White
Write-Host "  Inspector: http://localhost:6274" -ForegroundColor White