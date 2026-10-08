# Applies EF migrations and seeds demo catalog + demo users.
# Usage (from repo root):
#   powershell -ExecutionPolicy Bypass -File .\scripts\seed-db.ps1
# Optional env overrides:
#   $env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=scentmatch;User=root;Password=YOUR_PASSWORD;"
#   $env:Jwt__Key="YOUR_LONG_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARS"

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Seeding ScentMatch database..." -ForegroundColor Cyan
dotnet run --project API -- --seed-db
if ($LASTEXITCODE -ne 0) {
    throw "Seed command failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Seed completed." -ForegroundColor Green
Write-Host "Demo accounts:"
Write-Host "  Admin: admin@scentmatch.local / Admin123!"
Write-Host "  User:  user@scentmatch.local / User1234!"
