# K-Sail Calculator - Docker Build & Run Instructions

## Prerequisites
- Docker Desktop installed and running
- Node.js 22.9+ (for local development)

## Build Docker Image

```powershell
# Build the image
docker build -t ksail-calculator:latest .

# Build with a specific tag/version
docker build -t ksail-calculator:1.0.0 .
```

## Run Docker Container

```powershell
# Run on port 80
docker run -d -p 80:80 --name ksail-calculator ksail-calculator:latest

# Run on custom port (e.g., 8080)
docker run -d -p 8080:80 --name ksail-calculator ksail-calculator:latest

# Run with auto-restart
docker run -d -p 80:80 --restart unless-stopped --name ksail-calculator ksail-calculator:latest
```

## Access the Application

- Local: http://localhost
- Custom port: http://localhost:8080

## Docker Commands

```powershell
# View running containers
docker ps

# View logs
docker logs ksail-calculator

# Follow logs in real-time
docker logs -f ksail-calculator

# Stop container
docker stop ksail-calculator

# Remove container
docker rm ksail-calculator

# Remove image
docker rmi ksail-calculator:latest
```

## Build Script (Windows PowerShell)

```powershell
# build.ps1
Write-Host "Building K-Sail Calculator Docker image..." -ForegroundColor Green

# Build the image
docker build -t ksail-calculator:latest .

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build successful!" -ForegroundColor Green
    Write-Host "Run with: docker run -d -p 80:80 --name ksail-calculator ksail-calculator:latest" -ForegroundColor Cyan
} else {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    exit 1
}
```

## Docker Compose (Optional)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  ksail-calculator:
    build: .
    image: ksail-calculator:latest
    container_name: ksail-calculator
    ports:
      - "80:80"
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "wget", "-q", "-O", "-", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

Run with:
```powershell
docker-compose up -d
```

## Troubleshooting

### Docker Desktop not running
```
Error: The system cannot find the file specified
```
**Solution:** Start Docker Desktop and wait for it to fully initialize.

### Port already in use
```
Error: bind: address already in use
```
**Solution:** Use a different port: `docker run -d -p 8080:80 ...`

### Build fails with npm errors
**Solution:** Delete `node_modules` and `package-lock.json`, then rebuild.

## Image Size Optimization

The Dockerfile uses:
- **Multi-stage build** (Node.js for build, Nginx for serving)
- **Alpine Linux** (minimal base images)
- **Production build** (optimized and minified)

Expected final image size: ~50-70 MB

## Production Deployment

For cloud deployment (Azure, AWS, GCP):

```powershell
# Tag for registry
docker tag ksail-calculator:latest your-registry.azurecr.io/ksail-calculator:1.0.0

# Push to registry
docker push your-registry.azurecr.io/ksail-calculator:1.0.0
```
