# Build K-Sail Calculator Docker Image
Write-Host "🚀 Building K-Sail Calculator Docker Image..." -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
try {
    docker info | Out-Null
} catch {
    Write-Host "❌ Docker is not running!" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
    exit 1
}

# Build the image
Write-Host "📦 Building image..." -ForegroundColor Green
docker build -t ksail-calculator:latest .

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To run the container:" -ForegroundColor Cyan
    Write-Host "  docker run -d -p 80:80 --name ksail-calculator ksail-calculator:latest" -ForegroundColor White
    Write-Host ""
    Write-Host "Or on a different port (e.g., 8080):" -ForegroundColor Cyan
    Write-Host "  docker run -d -p 8080:80 --name ksail-calculator ksail-calculator:latest" -ForegroundColor White
    Write-Host ""
    Write-Host "Then access at: http://localhost" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
