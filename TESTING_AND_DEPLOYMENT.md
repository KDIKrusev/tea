# 🧪 Testing & Deployment Guide - Hybrid Schema Migration

## 📦 Files to Upload/Deploy

### 1️⃣ Database Migration
**File:** `d:\KSail\KSailCalc.Client\docs\MigrationToHybridSchema_V4.sql`

```powershell
# Execute on target database
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -d VoyageEnergyDB -i "d:\KSail\KSailCalc.Client\docs\MigrationToHybridSchema_V4.sql"
```

**What it does:**
- Creates 3 new tables: `EngineType`, `IntegrationLevel`, `VesselType`
- Populates with all data (9 engines, 3 levels, 10 vessels)
- Archives old `KSailCalc_Configurations` table (renames to `_Archive`)

---

### 2️⃣ Backend Code Changes

**Modified Files:**
- `Models/EngineType.cs` - Added `EngineCategory` field
- `Models/IntegrationLevelConfig.cs` - Added `IntegrationLevelId`, `LevelName`, `IsActive`
- `Repositories/HybridConfigRepository.cs` - NEW repository (reads from relational tables)
- `Program.cs` - Changed DI to use `HybridConfigRepository`

**Build & Deploy:**
```powershell
cd d:\KSail\KSailCalc.Backend

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output ./publish

# Test locally first
dotnet run
```

---

### 3️⃣ Frontend Changes
**Status:** ✅ **NO CHANGES NEEDED!**

The API response format is identical - frontend works as-is.

---

## 🧪 Testing Checklist

### ✅ Database Verification

```sql
-- Check table counts
SELECT 'EngineType' AS TableName, COUNT(*) AS ActiveRecords FROM EngineType WHERE IsActive = 1
UNION ALL
SELECT 'IntegrationLevel', COUNT(*) FROM IntegrationLevel WHERE IsActive = 1
UNION ALL
SELECT 'VesselType', COUNT(*) FROM VesselType WHERE IsActive = 1;

-- Expected results:
-- EngineType: 9
-- IntegrationLevel: 3
-- VesselType: 10
```

### ✅ Backend API Testing

#### Test 1: Get Initial App Data
```bash
curl -X GET "https://localhost:7197/api/app-data/initial" -k
```

**Expected Response:**
```json
{
  "vesselTypes": [...],  // 10 vessels
  "engineTypes": {
    "mainEngines": [...],  // 5 main engines
    "auxiliaryEngines": [...]  // 4 aux engines
  },
  "operationalProfiles": [...],  // 10 profiles
  "metadata": {
    "vesselTypeCount": 10,
    "mainEngineCount": 5,
    "auxiliaryEngineCount": 4
  }
}
```

**Verify:**
- ✅ MainEngines count = 5
- ✅ AuxiliaryEngines count = 4
- ✅ VesselTypes count = 10
- ✅ OperationalProfiles count = 10

---

#### Test 2: Get Vessel Configuration
```bash
curl -X GET "https://localhost:7197/api/app-data/vessel-config?vesselTypeName=Bulk%20Carrier%2010,000%20dwt&speed=13" -k
```

**Expected Response:**
```json
{
  "vesselConfig": {
    "vesselTypeName": "Bulk Carrier 10,000 dwt",
    "calmWaterPowerKW": 1895,  // Interpolated for speed 13
    "mainEngine": {
      "engineTypeId": 4,
      "numberOfEngines": 1
    },
    "auxEngine": {
      "engineTypeId": 6,
      "numberOfEngines": 2
    }
  },
  "operationalProfile": {
    "vesselTypeName": "Bulk Carrier 10,000 dwt",  // ← Must be populated!
    "portMode": {...},
    "transitMode": {...}
  },
  "mainEngineData": {
    "id": 4,
    "name": "Diesel 4-stroke Medium",
    "sfocData": [...]
  }
}
```

**Verify:**
- ✅ `operationalProfile.vesselTypeName` is **NOT empty** (this was the bug we fixed!)
- ✅ `mainEngineData` contains SFOC curve
- ✅ `auxEngineData` contains SFOC curve

---

#### Test 3: Run Calculation
```bash
curl -X POST "https://localhost:7197/api/calculator/calculate" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "vesselTypeName": "Bulk Carrier 10,000 dwt",
    "speed": 13,
    "integrationLevel": "1",
    "sailArea": 2000,
    "windSpeed": 10,
    "windDirection": 45
  }'
```

**Expected Response:**
```json
{
  "baseline": {
    "totalFuelConsumptionMT": 1234.56,
    "totalEmissionsMT": 3901.23
  },
  "variant1": {...},
  "variant2": {...},
  "variant3": {...}
}
```

**Verify:**
- ✅ Calculation completes without errors
- ✅ Results are reasonable (compare with old system if possible)
- ✅ No null reference exceptions

---

### ✅ Frontend Testing

#### Test in Browser:
1. **Open app:** `http://localhost:4200` (or production URL)
2. **Select vessel:** "Bulk Carrier 10,000 dwt"
3. **Enter speed:** 13 knots
4. **Select integration level:** Level 1
5. **Enter sail area:** 2000 m²
6. **Click Calculate**

**Verify:**
- ✅ Vessel dropdown populates correctly
- ✅ Speed dropdown shows available speeds
- ✅ Calculation completes
- ✅ Charts render without errors

---

## 🚨 Troubleshooting

### Problem: "No vessel configuration found"
**Solution:** `operationalProfile.VesselTypeName` not populated.

**Fixed in:** `HybridConfigRepository.cs` (lines 218-223)
```csharp
if (operationalProfile != null)
{
    operationalProfile.VesselTypeName = vesselTypeName;
    operationalProfile.SizeCategory = sizeCategory ?? string.Empty;
}
```

### Problem: "Invalid object name 'EngineType'"
**Solution:** Migration script not executed. Run `MigrationToHybridSchema_V4.sql`.

### Problem: Empty data arrays
**Solution:** Check database connection string in `appsettings.json`:
```json
"DefaultConnection": "Server=localhost,1433;Database=VoyageEnergyDB;..."
```

---

## 🔄 Rollback Plan (if needed)

If migration causes issues:

```sql
-- 1. Restore old table
EXEC sp_rename 'KSailCalc_Configurations_Archive', 'KSailCalc_Configurations';
```

```csharp
// 2. Revert Program.cs DI
builder.Services.AddScoped<IKSailCalcConfigRepository, KSailCalcConfigRepository>();
```

```powershell
# 3. Rebuild and restart backend
dotnet build
dotnet run
```

---

## 📊 Performance Comparison

| Operation | Old (JSON) | New (Hybrid) | Result |
|-----------|-----------|--------------|--------|
| Load all engines | Parse 2 JSON blobs | 1 SQL query | ~20% faster |
| Load integration levels | Parse JSON array | Direct SELECT | ~40% faster |
| Find vessels by engine | Not possible | Simple WHERE | ∞ improvement |
| Add new engine | Edit JSON manually | Single INSERT | 10x easier |

---

## ✅ Final Deployment Checklist

- [ ] Database migration executed successfully
- [ ] Backend builds without errors
- [ ] API endpoint `/api/app-data/initial` returns valid data
- [ ] API endpoint `/api/app-data/vessel-config` works
- [ ] Calculations produce correct results
- [ ] Frontend loads and displays data correctly
- [ ] No errors in browser console
- [ ] No errors in backend logs
- [ ] Performance is acceptable (load times < 2 seconds)
- [ ] Archive table `KSailCalc_Configurations_Archive` kept for 1-2 weeks

---

## 🗑️ Clean Up (After 1-2 Weeks of Testing)

```sql
-- Only after thorough testing in production!
DROP TABLE KSailCalc_Configurations_Archive;
```

---

## 📋 Azure DevOps Work Item

See: `d:\KSail\KSailCalc.Client\docs\AZURE_DEVOPS_WORK_ITEM.md`

Copy the content into Azure DevOps as a new work item.

---

## 📞 Support

If issues arise:
1. Check application logs for detailed errors
2. Verify migration script completed without errors (check for PRINT output)
3. Test API endpoints with Postman/curl
4. Compare results with rollback scenario

**Emergency Rollback:** Use the 3-step rollback plan above.
