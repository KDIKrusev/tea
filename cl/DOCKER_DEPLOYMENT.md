# 🐳 KSailCalc Docker Deployment - Quick Start

Step-by-step guide to run KSailCalc in Docker.

---

## Step 1: Clone Repositories

```powershell
git clone https://kognifai.visualstudio.com/Kongsberg%20Maritime/_git/KSailCalc.Client
git clone https://kognifai.visualstudio.com/Kongsberg%20Maritime/_git/KSailCalc.Backend
```

---

## Step 2: Start SQL Server

Start SQL Server using the VEA docker-compose file.

---

## Step 3: Setup Database Configuration

1. Open **SQL Server Management Studio** or **Azure Data Studio**
2. Connect to:
   - Server: `localhost,1433`
   - Username: `sa`
   - Password: `YourStrong!Passw0rd`
3. Click **New Query**
4. Open and execute the script: `<your-path>\KSailCalc.Client\docs\SetupKSailCalcConfiguration.sql`

This script will:
- Create the `KSailCalc_Configurations` table (if it doesn't exist)
- Insert **Main Engine Types** configuration
- Insert **Auxiliary Engine Types** configuration
- Insert **Vessel Types** configuration (with speed-dependent power requirements)

---

## Step 4: Build Docker Images

```powershell
# Build Backend
cd KSailCalc.Backend
docker build -t ksail-backend:latest .

# Build Frontend
cd ..\KSailCalc.Client
docker build -f Dockerfile.local -t ksail-frontend:latest .
```

---

## Step 5: Start Application

```powershell
cd KSailCalc.Client
docker-compose up -d
```

---

## Step 6: Open Application

Open browser: **http://localhost:8080**

---

## ✅ Verify

- **Vessel Type dropdown** should show different vessel types (Container Ship, Tanker, Cruise Ship, etc.)
- **Speed dropdown** should show available speeds when vessel type is selected
- **Engine dropdowns** should show main and auxiliary engine types from database
- **Auto-population** should work when selecting vessel type and speed
- Calculator should work end-to-end

---

**That's it! 🎉**
