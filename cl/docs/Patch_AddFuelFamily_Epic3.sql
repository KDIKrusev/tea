-- =============================================
-- KSailCalc Patch: Add FuelFamily (and Epic 3 catalogue columns)
-- Run ONCE on any DB that was set up via MigrationToHybridSchema_V4.sql
-- =============================================
-- This patch fills the gap left by V4: the original migration did not include
-- the Epic 3 catalogue columns (FuelFamily, Maker, Series, RatedPowerKW, Rpm, NoxTier).
-- The backend HybridConfigRepository already reads these columns; the frontend
-- uses FuelFamily to drive the per-engine fuel selector dropdown.
-- =============================================

USE VoyageEnergyDB;
GO

-- =============================================
-- STEP 1: Add catalogue columns (idempotent)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'FuelFamily')
    ALTER TABLE EngineType ADD FuelFamily NVARCHAR(20) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'Maker')
    ALTER TABLE EngineType ADD Maker NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'Series')
    ALTER TABLE EngineType ADD Series NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'RatedPowerKW')
    ALTER TABLE EngineType ADD RatedPowerKW DECIMAL(10,2) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'Rpm')
    ALTER TABLE EngineType ADD Rpm INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('EngineType') AND name = 'NoxTier')
    ALTER TABLE EngineType ADD NoxTier NVARCHAR(20) NULL;

PRINT '✅ Catalogue columns ensured.';
GO

-- =============================================
-- STEP 2: Set FuelFamily for seeded engine types
-- =============================================
-- Main engines: Liquid family (standard diesel/4-stroke)
UPDATE EngineType SET FuelFamily = 'Liquid'
WHERE EngineCategory = 'Main'
  AND Name IN ('Diesel 2-stroke Large', 'Diesel 2-stroke Medium', 'Diesel 4-stroke Large', 'Diesel 4-stroke Medium')
  AND (FuelFamily IS NULL OR FuelFamily = '');

-- Main engine: DualFuel — can burn LNG in gas mode or MGO/MDO/HFO in diesel mode
-- (Generic placeholder; real DF engines use separate -MEG entries with LNG family)
-- Force-update regardless of current value (may have been set to 'Liquid' incorrectly)
UPDATE EngineType SET FuelFamily = 'DualFuel'
WHERE EngineCategory = 'Main'
  AND Name = 'Dual Fuel Engine';

-- Auxiliary engines: Liquid family
UPDATE EngineType SET FuelFamily = 'Liquid'
WHERE EngineCategory = 'Auxiliary'
  AND Name IN (
      'Auxiliary Diesel 4-stroke Small',
      'Auxiliary Diesel 4-stroke Medium',
      'Auxiliary Diesel 4-stroke Large',
      'Auxiliary Diesel 4-stroke X-Large'
  )
  AND (FuelFamily IS NULL OR FuelFamily = '');

-- Auxiliary dual-fuel generator (force-update)
UPDATE EngineType SET FuelFamily = 'DualFuel'
WHERE EngineCategory = 'Auxiliary'
  AND Name = 'Auxiliary Dual Fuel';

PRINT '✅ FuelFamily values set for all seeded engine types.';
GO

-- =============================================
-- STEP 3: Verify
-- =============================================
SELECT EngineTypeId, EngineCategory, Name, FuelFamily
FROM EngineType
ORDER BY EngineCategory, EngineTypeId;
GO
