-- =============================================
-- KSailCalc Database Migration to Hybrid Schema V4.0
-- =============================================
-- Migration from JSON blob configuration to hybrid relational schema
--
-- NEW STRUCTURE (3 tables):
--   1. EngineType - Combined Main+Auxiliary engines (9 rows)
--   2. IntegrationLevel - Pure relational table (3 rows)
--   3. VesselType - Vessel data with FK to engines + JSON profiles (10 rows)
--
-- BENEFITS:
--   - Easy to add new engines (just INSERT)
--   - FK integrity for engine references
--   - IntegrationLevel is foreach-friendly (no JSON parsing)
--   - JSON only where consumed as unit (SFOC curves, operational profiles)
--
-- REMOVED:
--   - WeatherCondition table (not used - UI passes wind speed/direction directly)
-- =============================================

USE VoyageEnergyDB;
GO

-- =============================================
-- STEP 0: Backup current data (optional but recommended)
-- =============================================
PRINT '📋 Creating backup of current configuration...';

-- Uncomment to create backup table:
-- SELECT * INTO KSailCalc_Configurations_Backup_20260302 FROM KSailCalc_Configurations;

PRINT '✅ Backup step ready (uncomment if needed).';
GO

-- =============================================
-- STEP 1: Drop existing hybrid tables if they exist
-- =============================================
PRINT '🧹 Cleaning up existing hybrid tables...';

IF OBJECT_ID('VesselType', 'U') IS NOT NULL
    DROP TABLE VesselType;

IF OBJECT_ID('IntegrationLevel', 'U') IS NOT NULL
    DROP TABLE IntegrationLevel;

IF OBJECT_ID('EngineType', 'U') IS NOT NULL
    DROP TABLE EngineType;

PRINT '✅ Old hybrid tables dropped.';
GO

-- =============================================
-- STEP 2: Create EngineType Table
-- =============================================
PRINT '⚙️  Creating EngineType table...';

CREATE TABLE EngineType (
    EngineTypeId INT IDENTITY(1,1) PRIMARY KEY,
    EngineCategory NVARCHAR(20) NOT NULL,  -- 'Main' or 'Auxiliary'
    Name NVARCHAR(100) NOT NULL,
    MaxCapacityKW DECIMAL(10,2) NOT NULL,
    ShaftGeneratorMaxCapacityKW DECIMAL(10,2) NULL,  -- NULL for Auxiliary engines
    Description NVARCHAR(500) NULL,
    SfocDataJson NVARCHAR(MAX) NOT NULL,  -- JSON: [{"load": 0.25, "sfoc": 176.14}, ...]
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,
    
    CONSTRAINT CHK_EngineCategory CHECK (EngineCategory IN ('Main', 'Auxiliary')),
    CONSTRAINT CHK_SfocData_IsJson CHECK (ISJSON(SfocDataJson) = 1)
);

CREATE INDEX IX_EngineType_Category ON EngineType(EngineCategory);
CREATE INDEX IX_EngineType_Active ON EngineType(IsActive);

PRINT '✅ EngineType table created.';
GO

-- =============================================
-- STEP 3: Populate EngineType - Main Engines (5)
-- =============================================
PRINT '⚙️  Inserting Main Engine Types...';

SET IDENTITY_INSERT EngineType ON;

INSERT INTO EngineType (EngineTypeId, EngineCategory, Name, MaxCapacityKW, ShaftGeneratorMaxCapacityKW, Description, SfocDataJson)
VALUES 
(1, 'Main', 'Diesel 2-stroke Large', 25000, 3000, 'Large marine diesel 2-stroke engine', 
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 176.14}, {"load": 0.5, "sfoc": 172.28}, {"load": 0.75, "sfoc": 168.85}, {"load": 0.9, "sfoc": 167.69}, {"load": 1.0, "sfoc": 169.04}]'),

(2, 'Main', 'Diesel 2-stroke Medium', 15000, 2000, 'Medium marine diesel 2-stroke engine',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 178.50}, {"load": 0.5, "sfoc": 174.20}, {"load": 0.75, "sfoc": 170.30}, {"load": 0.9, "sfoc": 169.10}, {"load": 1.0, "sfoc": 170.80}]'),

(3, 'Main', 'Diesel 4-stroke Large', 20000, 2500, 'Large 4-stroke marine engine',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 185.20}, {"load": 0.5, "sfoc": 180.45}, {"load": 0.75, "sfoc": 175.60}, {"load": 0.9, "sfoc": 174.20}, {"load": 1.0, "sfoc": 176.30}]'),

(4, 'Main', 'Diesel 4-stroke Medium', 12000, 1500, 'Medium 4-stroke marine engine',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 188.75}, {"load": 0.5, "sfoc": 183.90}, {"load": 0.75, "sfoc": 178.40}, {"load": 0.9, "sfoc": 177.15}, {"load": 1.0, "sfoc": 179.50}]'),

(5, 'Main', 'Dual Fuel Engine', 22000, 2800, 'LNG/Diesel dual fuel engine',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 165.40}, {"load": 0.5, "sfoc": 162.15}, {"load": 0.75, "sfoc": 159.30}, {"load": 0.9, "sfoc": 158.80}, {"load": 1.0, "sfoc": 160.20}]');

PRINT '✅ Main Engine Types inserted (5 records).';
GO

-- =============================================
-- STEP 4: Populate EngineType - Auxiliary Engines (4)
-- =============================================
PRINT '⚙️  Inserting Auxiliary Engine Types...';

INSERT INTO EngineType (EngineTypeId, EngineCategory, Name, MaxCapacityKW, ShaftGeneratorMaxCapacityKW, Description, SfocDataJson)
VALUES 
(6, 'Auxiliary', 'Auxiliary Diesel 4-stroke Small', 500, NULL, 'Small auxiliary diesel generator',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 230.50}, {"load": 0.5, "sfoc": 215.40}, {"load": 0.75, "sfoc": 208.60}, {"load": 0.9, "sfoc": 206.20}, {"load": 1.0, "sfoc": 208.75}]'),

(7, 'Auxiliary', 'Auxiliary Diesel 4-stroke Medium', 1000, NULL, 'Medium auxiliary diesel generator',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 220.36}, {"load": 0.5, "sfoc": 202.81}, {"load": 0.75, "sfoc": 196.22}, {"load": 0.9, "sfoc": 194.04}, {"load": 1.0, "sfoc": 195.91}]'),

(8, 'Auxiliary', 'Auxiliary Diesel 4-stroke Large', 2000, NULL, 'Large auxiliary diesel generator',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 215.80}, {"load": 0.5, "sfoc": 198.45}, {"load": 0.75, "sfoc": 192.15}, {"load": 0.9, "sfoc": 190.30}, {"load": 1.0, "sfoc": 192.60}]'),

(9, 'Auxiliary', 'Auxiliary Dual Fuel', 1500, NULL, 'Dual fuel auxiliary generator',
 '[{"load": 0, "sfoc": 0}, {"load": 0.25, "sfoc": 205.75}, {"load": 0.5, "sfoc": 188.90}, {"load": 0.75, "sfoc": 182.40}, {"load": 0.9, "sfoc": 180.85}, {"load": 1.0, "sfoc": 183.20}]');

SET IDENTITY_INSERT EngineType OFF;

PRINT '✅ Auxiliary Engine Types inserted (4 records).';
PRINT '✅ Total EngineType records: 9 (5 Main + 4 Auxiliary)';
GO

-- =============================================
-- STEP 5: Create IntegrationLevel Table
-- =============================================
PRINT '⚙️  Creating IntegrationLevel table...';

CREATE TABLE IntegrationLevel (
    IntegrationLevelId INT PRIMARY KEY,  -- 1, 2, 3
    LevelName NVARCHAR(50) NOT NULL,
    BaseEfficiencyFactor DECIMAL(5,4) NOT NULL,
    IemsPriceNOK DECIMAL(12,2) NOT NULL,
    CommissioningNOK DECIMAL(12,2) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

PRINT '✅ IntegrationLevel table created.';
GO

-- =============================================
-- STEP 6: Populate IntegrationLevel (3)
-- =============================================
PRINT '⚙️  Inserting iEMS Integration Levels...';

INSERT INTO IntegrationLevel (IntegrationLevelId, LevelName, BaseEfficiencyFactor, IemsPriceNOK, CommissioningNOK, Description)
VALUES 
(1, 'Level 1', 0.9700, 1000000, 100000, '3% FOC reduction - Entry level integration'),
(2, 'Level 2', 0.9550, 1205000, 200000, '4.5% FOC reduction - Mid-tier integration with enhanced optimization'),
(3, 'Level 3', 0.9400, 1800000, 300000, '6% FOC reduction - Premium integration with advanced AI-driven optimization');

PRINT '✅ IntegrationLevel records inserted (3 records).';
GO

-- =============================================
-- STEP 7: Create VesselType Table
-- =============================================
PRINT '⚙️  Creating VesselType table...';

CREATE TABLE VesselType (
    VesselTypeId INT IDENTITY(1,1) PRIMARY KEY,
    VesselTypeName NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NOT NULL,        -- 'Bulk Carrier', 'Oil Tanker', 'Offshore Support'
    SizeCategory NVARCHAR(50) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,             -- 'dwt', 'GT'
    Description NVARCHAR(500) NULL,
    
    -- Main Engine Reference
    MainEngineTypeId INT NOT NULL,
    NumberOfMainEngines INT NOT NULL DEFAULT 1,
    MainEngineShaftGeneratorKW DECIMAL(10,2) NULL,  -- Override per vessel
    
    -- Auxiliary Engine Reference  
    AuxEngineTypeId INT NOT NULL,
    NumberOfAuxEngines INT NOT NULL DEFAULT 2,
    
    -- Speed/Power Curve (JSON - always consumed as unit for interpolation)
    SpeedPowerCurveJson NVARCHAR(MAX) NOT NULL,
    SeaMarginPercent DECIMAL(5,2) NOT NULL,
    
    -- Operational Profile (JSON - whole profile consumed together)
    OperationalProfileJson NVARCHAR(MAX) NOT NULL,
    
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ModifiedDate DATETIME2 NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_VesselType_MainEngine FOREIGN KEY (MainEngineTypeId) 
        REFERENCES EngineType(EngineTypeId),
    CONSTRAINT FK_VesselType_AuxEngine FOREIGN KEY (AuxEngineTypeId) 
        REFERENCES EngineType(EngineTypeId),
    
    -- JSON Validation
    CONSTRAINT CHK_SpeedPowerCurve_IsJson CHECK (ISJSON(SpeedPowerCurveJson) = 1),
    CONSTRAINT CHK_OperationalProfile_IsJson CHECK (ISJSON(OperationalProfileJson) = 1)
);

CREATE INDEX IX_VesselType_Category ON VesselType(Category);
CREATE INDEX IX_VesselType_MainEngine ON VesselType(MainEngineTypeId);
CREATE INDEX IX_VesselType_AuxEngine ON VesselType(AuxEngineTypeId);

PRINT '✅ VesselType table created.';
GO

-- =============================================
-- STEP 8: Populate VesselType (10 vessels)
-- =============================================
PRINT '⚙️  Inserting Vessel Types...';

SET IDENTITY_INSERT VesselType ON;

-- Vessel 1: Bulk Carrier 10,000 dwt
INSERT INTO VesselType (VesselTypeId, VesselTypeName, Category, SizeCategory, Unit, Description,
    MainEngineTypeId, NumberOfMainEngines, MainEngineShaftGeneratorKW,
    AuxEngineTypeId, NumberOfAuxEngines,
    SpeedPowerCurveJson, SeaMarginPercent, OperationalProfileJson)
VALUES 
(1, 'Bulk Carrier 10,000 dwt', 'Bulk Carrier', '10000-34999', 'dwt', 'Small bulk carrier from IMO Fourth GHG Study 2020',
 4, 1, 200,   -- Main: Diesel 4-stroke Medium (id=4)
 6, 2,        -- Aux: Auxiliary Diesel 4-stroke Small (id=6)
 '[{"speedKnots": 12.0, "calmWaterPowerKW": 1365}, {"speedKnots": 13.0, "calmWaterPowerKW": 1895}, {"speedKnots": 14.0, "calmWaterPowerKW": 2663}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 110, "annualHours": 2592, "percentageOfYear": 29.59}, "anchorMode": {"hotelLoadPowerKW": 180, "annualHours": 451, "percentageOfYear": 5.15}, "maneuveringMode": {"hotelLoadPowerKW": 190, "propulsionPowerKW": 400, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 165, "annualHours": 5717, "percentageOfYear": 65.26}, "dpMode": null}'),

-- Vessel 2: Bulk Carrier 35,000 dwt
(2, 'Bulk Carrier 35,000 dwt', 'Bulk Carrier', '35000-59999', 'dwt', 'Medium bulk carrier from IMO Fourth GHG Study 2020',
 2, 1, 400,   -- Main: Diesel 2-stroke Medium (id=2)
 7, 3,        -- Aux: Auxiliary Diesel 4-stroke Medium (id=7)
 '[{"speedKnots": 12.0, "calmWaterPowerKW": 2945}, {"speedKnots": 13.0, "calmWaterPowerKW": 3801}, {"speedKnots": 14.0, "calmWaterPowerKW": 4902}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 130, "annualHours": 2592, "percentageOfYear": 29.59}, "anchorMode": {"hotelLoadPowerKW": 230, "annualHours": 451, "percentageOfYear": 5.15}, "maneuveringMode": {"hotelLoadPowerKW": 250, "propulsionPowerKW": 800, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 230, "annualHours": 5717, "percentageOfYear": 65.26}, "dpMode": null}'),

-- Vessel 3: Bulk Carrier 63,000 dwt
(3, 'Bulk Carrier 63,000 dwt', 'Bulk Carrier', '60000-99999', 'dwt', 'Large bulk carrier from IMO Fourth GHG Study 2020',
 1, 1, 600,   -- Main: Diesel 2-stroke Large (id=1)
 7, 3,        -- Aux: Auxiliary Diesel 4-stroke Medium (id=7)
 '[{"speedKnots": 12.0, "calmWaterPowerKW": 3865}, {"speedKnots": 13.0, "calmWaterPowerKW": 4941}, {"speedKnots": 14.0, "calmWaterPowerKW": 6294}, {"speedKnots": 15.0, "calmWaterPowerKW": 8032}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 240, "annualHours": 1920, "percentageOfYear": 21.92}, "anchorMode": {"hotelLoadPowerKW": 400, "annualHours": 797, "percentageOfYear": 9.10}, "maneuveringMode": {"hotelLoadPowerKW": 410, "propulsionPowerKW": 1200, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 372, "annualHours": 6043, "percentageOfYear": 68.98}, "dpMode": null}'),

-- Vessel 4: Bulk Carrier 100,000 dwt
(4, 'Bulk Carrier 100,000 dwt', 'Bulk Carrier', '100000-199999', 'dwt', 'Very large bulk carrier from IMO Fourth GHG Study 2020',
 1, 1, 800,   -- Main: Diesel 2-stroke Large (id=1)
 8, 3,        -- Aux: Auxiliary Diesel 4-stroke Large (id=8)
 '[{"speedKnots": 13.0, "calmWaterPowerKW": 2055}, {"speedKnots": 14.0, "calmWaterPowerKW": 2777}, {"speedKnots": 15.0, "calmWaterPowerKW": 3755}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 230, "annualHours": 1776, "percentageOfYear": 20.27}, "anchorMode": {"hotelLoadPowerKW": 380, "annualHours": 1094, "percentageOfYear": 12.49}, "maneuveringMode": {"hotelLoadPowerKW": 380, "propulsionPowerKW": 500, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 362, "annualHours": 5890, "percentageOfYear": 67.24}, "dpMode": null}'),

-- Vessel 5: Bulk Carrier 180,000 dwt
(5, 'Bulk Carrier 180,000 dwt', 'Bulk Carrier', '100000-199999', 'dwt', 'Cape-size bulk carrier from IMO Fourth GHG Study 2020',
 1, 1, 1200,  -- Main: Diesel 2-stroke Large (id=1)
 8, 4,        -- Aux: Auxiliary Diesel 4-stroke Large (id=8)
 '[{"speedKnots": 14.0, "calmWaterPowerKW": 11204}, {"speedKnots": 15.0, "calmWaterPowerKW": 13820}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 230, "annualHours": 1776, "percentageOfYear": 20.27}, "anchorMode": {"hotelLoadPowerKW": 380, "annualHours": 1094, "percentageOfYear": 12.49}, "maneuveringMode": {"hotelLoadPowerKW": 380, "propulsionPowerKW": 2500, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 362, "annualHours": 5890, "percentageOfYear": 67.24}, "dpMode": null}'),

-- Vessel 6: Tanker 10,000 dwt
(6, 'Tanker 10,000 dwt', 'Oil Tanker', '10000-19999', 'dwt', 'Small tanker from IMO Fourth GHG Study 2020',
 4, 1, 200,   -- Main: Diesel 4-stroke Medium (id=4)
 6, 2,        -- Aux: Auxiliary Diesel 4-stroke Small (id=6)
 '[{"speedKnots": 11.0, "calmWaterPowerKW": 1029}, {"speedKnots": 12.0, "calmWaterPowerKW": 1378}, {"speedKnots": 13.0, "calmWaterPowerKW": 1850}]',
 18.0,
 '{"portMode": {"hotelLoadPowerKW": 360, "annualHours": 1752, "percentageOfYear": 20.0}, "anchorMode": {"hotelLoadPowerKW": 690, "annualHours": 804, "percentageOfYear": 9.18}, "maneuveringMode": {"hotelLoadPowerKW": 560, "propulsionPowerKW": 300, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 520, "annualHours": 6204, "percentageOfYear": 70.82}, "dpMode": null}'),

-- Vessel 7: Tanker 50,000 dwt
(7, 'Tanker 50,000 dwt', 'Oil Tanker', '20000-59999', 'dwt', 'Medium tanker from IMO Fourth GHG Study 2020',
 3, 1, 500,   -- Main: Diesel 4-stroke Large (id=3)
 7, 3,        -- Aux: Auxiliary Diesel 4-stroke Medium (id=7)
 '[{"speedKnots": 13.0, "calmWaterPowerKW": 4021}, {"speedKnots": 14.0, "calmWaterPowerKW": 5151}, {"speedKnots": 15.0, "calmWaterPowerKW": 6629}]',
 18.0,
 '{"portMode": {"hotelLoadPowerKW": 410, "annualHours": 1752, "percentageOfYear": 20.0}, "anchorMode": {"hotelLoadPowerKW": 590, "annualHours": 804, "percentageOfYear": 9.18}, "maneuveringMode": {"hotelLoadPowerKW": 600, "propulsionPowerKW": 1100, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 565, "annualHours": 6204, "percentageOfYear": 70.82}, "dpMode": null}'),

-- Vessel 8: Tanker 105,000 dwt (Aframax)
(8, 'Tanker 105,000 dwt', 'Oil Tanker', '60000-119999', 'dwt', 'Large tanker (Aframax) from IMO Fourth GHG Study 2020',
 1, 1, 800,   -- Main: Diesel 2-stroke Large (id=1)
 8, 3,        -- Aux: Auxiliary Diesel 4-stroke Large (id=8)
 '[{"speedKnots": 13.0, "calmWaterPowerKW": 6167}, {"speedKnots": 14.0, "calmWaterPowerKW": 8392}, {"speedKnots": 15.0, "calmWaterPowerKW": 9575}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 520, "annualHours": 1752, "percentageOfYear": 20.0}, "anchorMode": {"hotelLoadPowerKW": 870, "annualHours": 804, "percentageOfYear": 9.18}, "maneuveringMode": {"hotelLoadPowerKW": 760, "propulsionPowerKW": 1600, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 715, "annualHours": 6204, "percentageOfYear": 70.82}, "dpMode": null}'),

-- Vessel 9: Tanker 300,000 dwt (VLCC)
(9, 'Tanker 300,000 dwt', 'Oil Tanker', '120000+', 'dwt', 'Very large tanker (VLCC) from IMO Fourth GHG Study 2020',
 1, 1, 1500,  -- Main: Diesel 2-stroke Large (id=1)
 8, 4,        -- Aux: Auxiliary Diesel 4-stroke Large (id=8)
 '[{"speedKnots": 13.0, "calmWaterPowerKW": 10969}, {"speedKnots": 14.0, "calmWaterPowerKW": 13603}, {"speedKnots": 15.0, "calmWaterPowerKW": 16688}]',
 20.0,
 '{"portMode": {"hotelLoadPowerKW": 680, "annualHours": 1752, "percentageOfYear": 20.0}, "anchorMode": {"hotelLoadPowerKW": 1190, "annualHours": 804, "percentageOfYear": 9.18}, "maneuveringMode": {"hotelLoadPowerKW": 1150, "propulsionPowerKW": 2800, "annualHours": 175, "percentageOfYear": 2.0}, "transitMode": {"hotelLoadPowerKW": 1063, "annualHours": 6204, "percentageOfYear": 70.82}, "dpMode": null}'),

-- Vessel 10: Offshore Support Vessel (with DP Mode)
(10, 'Offshore Support Vessel', 'Offshore Support', '5000-10000', 'dwt', 'Offshore Support Vessel with Dynamic Positioning capabilities',
 2, 2, 1000,  -- Main: Diesel 2-stroke Medium (id=2) x2
 7, 3,        -- Aux: Auxiliary Diesel 4-stroke Medium (id=7)
 '[{"speedKnots": 10.0, "calmWaterPowerKW": 1500}, {"speedKnots": 11.0, "calmWaterPowerKW": 1950}, {"speedKnots": 12.0, "calmWaterPowerKW": 2500}, {"speedKnots": 13.0, "calmWaterPowerKW": 3200}]',
 15.0,
 '{"portMode": {"hotelLoadPowerKW": 150, "annualHours": 1200, "percentageOfYear": 13.70}, "anchorMode": {"hotelLoadPowerKW": 200, "annualHours": 800, "percentageOfYear": 9.13}, "maneuveringMode": {"hotelLoadPowerKW": 250, "propulsionPowerKW": 500, "annualHours": 400, "percentageOfYear": 4.57}, "transitMode": {"hotelLoadPowerKW": 220, "annualHours": 4000, "percentageOfYear": 45.66}, "dpMode": {"hotelLoadPowerKW": 300, "annualHours": 2360, "percentageOfYear": 26.94, "requiredDPPowerKW": 3500, "weatherConditions": [{"condition": "Calm", "thrustDemandFactor": 1.0, "minAverageThrustPowerKW": 3500}, {"condition": "Moderate", "thrustDemandFactor": 1.3, "minAverageThrustPowerKW": 4550}, {"condition": "Rough", "thrustDemandFactor": 1.7, "minAverageThrustPowerKW": 5950}]}}');

SET IDENTITY_INSERT VesselType OFF;

PRINT '✅ Vessel Types inserted (10 records).';
GO

-- =============================================
-- STEP 9: Verification Queries
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ MIGRATION TO HYBRID SCHEMA V4.0 COMPLETE!';
PRINT '========================================';
PRINT '';
PRINT '📊 Record Count Summary:';
PRINT '  ✅ EngineType: 9 rows (5 Main + 4 Auxiliary)';
PRINT '  ✅ IntegrationLevel: 3 rows';
PRINT '  ✅ VesselType: 10 rows';
PRINT '';

-- Verify counts
SELECT 'EngineType' AS TableName, COUNT(*) AS RecordCount FROM EngineType
UNION ALL
SELECT 'IntegrationLevel', COUNT(*) FROM IntegrationLevel
UNION ALL
SELECT 'VesselType', COUNT(*) FROM VesselType;

PRINT '';
PRINT '🔍 Sample Queries:';
PRINT '';

-- Show all Main engines
PRINT '-- Get all Main engines:';
SELECT EngineTypeId, Name, MaxCapacityKW, ShaftGeneratorMaxCapacityKW
FROM EngineType 
WHERE EngineCategory = 'Main' AND IsActive = 1
ORDER BY EngineTypeId;

PRINT '';

-- Show all Auxiliary engines
PRINT '-- Get all Auxiliary engines:';
SELECT EngineTypeId, Name, MaxCapacityKW
FROM EngineType 
WHERE EngineCategory = 'Auxiliary' AND IsActive = 1
ORDER BY EngineTypeId;

PRINT '';

-- Show Integration Levels
PRINT '-- Get all Integration Levels:';
SELECT IntegrationLevelId, LevelName, BaseEfficiencyFactor, IemsPriceNOK, CommissioningNOK
FROM IntegrationLevel 
WHERE IsActive = 1
ORDER BY IntegrationLevelId;

PRINT '';

-- Show Vessel with engine details
PRINT '-- Get vessel with engine details (JOIN example):';
SELECT TOP 3
    v.VesselTypeId,
    v.VesselTypeName,
    v.Category,
    me.Name AS MainEngineName,
    me.MaxCapacityKW AS MainEngineKW,
    v.NumberOfMainEngines,
    ae.Name AS AuxEngineName,
    ae.MaxCapacityKW AS AuxEngineKW,
    v.NumberOfAuxEngines
FROM VesselType v
INNER JOIN EngineType me ON v.MainEngineTypeId = me.EngineTypeId
INNER JOIN EngineType ae ON v.AuxEngineTypeId = ae.EngineTypeId
WHERE v.IsActive = 1
ORDER BY v.VesselTypeId;

PRINT '';

-- =============================================
-- STEP 10: Archive Old Configuration Table
-- =============================================
PRINT '========================================';
PRINT '📦 ARCHIVING OLD CONFIGURATION TABLE...';
PRINT '========================================';

-- Verify new tables have data before archiving old table
DECLARE @EngineCount INT, @LevelCount INT, @VesselCount INT;

SELECT @EngineCount = COUNT(*) FROM EngineType WHERE IsActive = 1;
SELECT @LevelCount = COUNT(*) FROM IntegrationLevel WHERE IsActive = 1;
SELECT @VesselCount = COUNT(*) FROM VesselType WHERE IsActive = 1;

PRINT 'Verifying migration data:';
PRINT '  - EngineType: ' + CAST(@EngineCount AS VARCHAR) + ' active records';
PRINT '  - IntegrationLevel: ' + CAST(@LevelCount AS VARCHAR) + ' active records';
PRINT '  - VesselType: ' + CAST(@VesselCount AS VARCHAR) + ' active records';

IF @EngineCount < 9 OR @LevelCount < 3 OR @VesselCount < 10
BEGIN
    RAISERROR('⚠️  WARNING: New tables have incomplete data! Review migration before archiving old table.', 16, 1);
END
ELSE
BEGIN
    PRINT '✅ All data migrated successfully!';
    PRINT '';
    
    -- Archive old table (rename, don't drop for safety)
    IF OBJECT_ID('KSailCalc_Configurations', 'U') IS NOT NULL
    BEGIN
        PRINT '📦 Archiving KSailCalc_Configurations table...';
        
        -- Drop old archive if exists
        IF OBJECT_ID('KSailCalc_Configurations_Archive', 'U') IS NOT NULL
        BEGIN
            PRINT '   Removing previous archive...';
            DROP TABLE KSailCalc_Configurations_Archive;
        END
        
        -- Rename current table to archive
        EXEC sp_rename 'KSailCalc_Configurations', 'KSailCalc_Configurations_Archive';
        
        PRINT '✅ Old table archived as: KSailCalc_Configurations_Archive';
        PRINT '';
        PRINT '⚠️  IMPORTANT: Test backend thoroughly before permanently deleting!';
        PRINT '   To restore if needed:';
        PRINT '   EXEC sp_rename ''KSailCalc_Configurations_Archive'', ''KSailCalc_Configurations'';';
        PRINT '';
        PRINT '   To permanently delete after testing (1-2 weeks):';
        PRINT '   DROP TABLE KSailCalc_Configurations_Archive;';
    END
    ELSE
    BEGIN
        PRINT 'ℹ️  KSailCalc_Configurations table not found (already archived or removed).';
    END
END

PRINT '';
PRINT '========================================';
PRINT '🎯 NEXT STEPS:';
PRINT '  1. ✅ New tables created and populated';
PRINT '  2. ✅ Old table archived (not deleted)';
PRINT '  3. 🔄 Update backend to use HybridConfigRepository';
PRINT '  4. 🧪 Test all API endpoints thoroughly';
PRINT '  5. 🗑️  Drop archive table after 1-2 weeks of testing';
PRINT '';
PRINT '🚀 Database migration to hybrid schema COMPLETE!';
PRINT '========================================';
GO
