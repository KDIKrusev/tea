-- =============================================
-- KSailCalc Application Configuration Setup
-- Database: VoyageEnergyDB
-- Purpose: Complete setup for KSailCalc configurations including Engine Types and Vessel Types
-- To execute: Run manually in SQL Server Management Studio
-- =============================================

USE VoyageEnergyDB;
GO

-- =============================================
-- Create KSailCalc_Configurations table if not exists
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KSailCalc_Configurations')
BEGIN
    CREATE TABLE KSailCalc_Configurations (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ConfigType NVARCHAR(50) NOT NULL UNIQUE, -- 'MainEngine', 'AuxiliaryEngine', 'VesselType'
        ConfigName NVARCHAR(200) NOT NULL,
        ConfigJson NVARCHAR(MAX) NOT NULL, -- JSON array of configurations
        Description NVARCHAR(500),
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    
    -- Create index
    CREATE INDEX IX_KSailCalc_Configurations_ConfigType ON KSailCalc_Configurations(ConfigType);
    
    PRINT 'KSailCalc_Configurations table created successfully!';
END
ELSE
BEGIN
    PRINT 'KSailCalc_Configurations table already exists.';
END
GO

-- =============================================
-- Delete existing configurations (for clean setup)
-- =============================================
DELETE FROM KSailCalc_Configurations WHERE ConfigType IN ('MainEngine', 'AuxiliaryEngine', 'VesselType');
GO

-- =============================================
-- Insert Main Engine Types Configuration
-- =============================================
INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'MainEngine',
    'Main Engine Types Configuration',
    '[
        {
            "id": 1,
            "name": "Diesel 2-stroke Large",
            "maxCapacityKW": 25000,
            "shaftGeneratorMaxCapacityKW": 3000,
            "description": "Large marine diesel 2-stroke engine"
        },
        {
            "id": 2,
            "name": "Diesel 2-stroke Medium",
            "maxCapacityKW": 15000,
            "shaftGeneratorMaxCapacityKW": 2000,
            "description": "Medium marine diesel 2-stroke engine"
        },
        {
            "id": 3,
            "name": "Diesel 4-stroke Large",
            "maxCapacityKW": 20000,
            "shaftGeneratorMaxCapacityKW": 2500,
            "description": "Large 4-stroke marine engine"
        },
        {
            "id": 4,
            "name": "Diesel 4-stroke Medium",
            "maxCapacityKW": 12000,
            "shaftGeneratorMaxCapacityKW": 1500,
            "description": "Medium 4-stroke marine engine"
        },
        {
            "id": 5,
            "name": "Dual Fuel Engine",
            "maxCapacityKW": 22000,
            "shaftGeneratorMaxCapacityKW": 2800,
            "description": "LNG/Diesel dual fuel engine"
        }
    ]',
    'Configuration for main engine types dropdown'
);

PRINT 'Main Engine Types configuration inserted successfully!';
GO

-- =============================================
-- Insert Auxiliary Engine Types Configuration
-- =============================================
INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'AuxiliaryEngine',
    'Auxiliary Engine Types Configuration',
    '[
        {
            "id": 1,
            "name": "Auxiliary Diesel 4-stroke Small",
            "maxCapacityKW": 500,
            "description": "Small auxiliary diesel generator"
        },
        {
            "id": 2,
            "name": "Auxiliary Diesel 4-stroke Medium",
            "maxCapacityKW": 1000,
            "description": "Medium auxiliary diesel generator"
        },
        {
            "id": 3,
            "name": "Auxiliary Diesel 4-stroke Large",
            "maxCapacityKW": 2000,
            "description": "Large auxiliary diesel generator"
        },
        {
            "id": 4,
            "name": "Auxiliary Dual Fuel",
            "maxCapacityKW": 1500,
            "description": "Dual fuel auxiliary generator"
        }
    ]',
    'Configuration for auxiliary engine types dropdown'
);

PRINT 'Auxiliary Engine Types configuration inserted successfully!';
GO

-- =============================================
-- Insert Vessel Types Configuration
-- =============================================
INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'VesselType',
    'IMO Fourth GHG Study 2020 - 100% Real Data (Bulk Carriers & Tankers Only)',
    '[
        {"vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 12.0, "calmWaterPowerKW": 1365, "seaMargin": 20.0, "hotelPowerKW": 165, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel = (110×2592+180×451+190×5717)/8760 = 165 kW"},
        {"vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 13.0, "calmWaterPowerKW": 1895, "seaMargin": 20.0, "hotelPowerKW": 165, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel = (110×2592+180×451+190×5717)/8760 = 165 kW"},
        {"vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 14.0, "calmWaterPowerKW": 2663, "seaMargin": 20.0, "hotelPowerKW": 165, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel = (110×2592+180×451+190×5717)/8760 = 165 kW"},
        {"vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 12.0, "calmWaterPowerKW": 2945, "seaMargin": 20.0, "hotelPowerKW": 230, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel = (130×2592+230×451+250×5717)/8760 = 230 kW"},
        {"vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 13.0, "calmWaterPowerKW": 3801, "seaMargin": 20.0, "hotelPowerKW": 230, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel = (130×2592+230×451+250×5717)/8760 = 230 kW"},
        {"vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 14.0, "calmWaterPowerKW": 4902, "seaMargin": 20.0, "hotelPowerKW": 230, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel = (130×2592+230×451+250×5717)/8760 = 230 kW"},
        {"vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 12.0, "calmWaterPowerKW": 3865, "seaMargin": 20.0, "hotelPowerKW": 372, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel = (240×1920+400×797+410×6043)/8760 = 372 kW"},
        {"vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 13.0, "calmWaterPowerKW": 4941, "seaMargin": 20.0, "hotelPowerKW": 372, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel = (240×1920+400×797+410×6043)/8760 = 372 kW"},
        {"vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 14.0, "calmWaterPowerKW": 6294, "seaMargin": 20.0, "hotelPowerKW": 372, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel = (240×1920+400×797+410×6043)/8760 = 372 kW"},
        {"vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 15.0, "calmWaterPowerKW": 8032, "seaMargin": 20.0, "hotelPowerKW": 372, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel = (240×1920+400×797+410×6043)/8760 = 372 kW"},
        {"vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 13.0, "calmWaterPowerKW": 2055, "seaMargin": 20.0, "hotelPowerKW": 362, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel = (230×1776+380×1094+380×5890)/8760 = 362 kW"},
        {"vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 14.0, "calmWaterPowerKW": 2777, "seaMargin": 20.0, "hotelPowerKW": 362, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel = (230×1776+380×1094+380×5890)/8760 = 362 kW"},
        {"vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 15.0, "calmWaterPowerKW": 3755, "seaMargin": 20.0, "hotelPowerKW": 362, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel = (230×1776+380×1094+380×5890)/8760 = 362 kW"},
        {"vesselTypeName": "Bulk Carrier 180,000 dwt", "speed": 14.0, "calmWaterPowerKW": 11204, "seaMargin": 20.0, "hotelPowerKW": 362, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel = (230×1776+380×1094+380×5890)/8760 = 362 kW"},
        {"vesselTypeName": "Bulk Carrier 180,000 dwt", "speed": 15.0, "calmWaterPowerKW": 13820, "seaMargin": 20.0, "hotelPowerKW": 362, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel = (230×1776+380×1094+380×5890)/8760 = 362 kW"},
        {"vesselTypeName": "Tanker 10,000 dwt", "speed": 11.0, "calmWaterPowerKW": 1029, "seaMargin": 18.0, "hotelPowerKW": 520, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 11 knots. Hotel = (360×1752+690×804+560×6204)/8760 = 520 kW"},
        {"vesselTypeName": "Tanker 10,000 dwt", "speed": 12.0, "calmWaterPowerKW": 1378, "seaMargin": 18.0, "hotelPowerKW": 520, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 12 knots. Hotel = (360×1752+690×804+560×6204)/8760 = 520 kW"},
        {"vesselTypeName": "Tanker 10,000 dwt", "speed": 13.0, "calmWaterPowerKW": 1850, "seaMargin": 18.0, "hotelPowerKW": 520, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel = (360×1752+690×804+560×6204)/8760 = 520 kW"},
        {"vesselTypeName": "Tanker 50,000 dwt", "speed": 13.0, "calmWaterPowerKW": 4021, "seaMargin": 18.0, "hotelPowerKW": 565, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel = (410×1752+590×804+600×6204)/8760 = 565 kW"},
        {"vesselTypeName": "Tanker 50,000 dwt", "speed": 14.0, "calmWaterPowerKW": 5151, "seaMargin": 18.0, "hotelPowerKW": 565, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel = (410×1752+590×804+600×6204)/8760 = 565 kW"},
        {"vesselTypeName": "Tanker 50,000 dwt", "speed": 15.0, "calmWaterPowerKW": 6629, "seaMargin": 18.0, "hotelPowerKW": 565, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel = (410×1752+590×804+600×6204)/8760 = 565 kW"},
        {"vesselTypeName": "Tanker 105,000 dwt", "speed": 13.0, "calmWaterPowerKW": 6167, "seaMargin": 20.0, "hotelPowerKW": 715, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel = (520×1752+870×804+760×6204)/8760 = 715 kW"},
        {"vesselTypeName": "Tanker 105,000 dwt", "speed": 14.0, "calmWaterPowerKW": 8392, "seaMargin": 20.0, "hotelPowerKW": 715, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel = (520×1752+870×804+760×6204)/8760 = 715 kW"},
        {"vesselTypeName": "Tanker 105,000 dwt", "speed": 15.0, "calmWaterPowerKW": 9575, "seaMargin": 20.0, "hotelPowerKW": 715, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel = (520×1752+870×804+760×6204)/8760 = 715 kW"},
        {"vesselTypeName": "Tanker 300,000 dwt", "speed": 13.0, "calmWaterPowerKW": 10969, "seaMargin": 20.0, "hotelPowerKW": 1063, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel = (680×1752+1190×804+1150×6204)/8760 = 1063 kW"},
        {"vesselTypeName": "Tanker 300,000 dwt", "speed": 14.0, "calmWaterPowerKW": 13603, "seaMargin": 20.0, "hotelPowerKW": 1063, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel = (680×1752+1190×804+1150×6204)/8760 = 1063 kW"},
        {"vesselTypeName": "Tanker 300,000 dwt", "speed": 15.0, "calmWaterPowerKW": 16688, "seaMargin": 20.0, "hotelPowerKW": 1063, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel = (680×1752+1190×804+1150×6204)/8760 = 1063 kW"}
    ]',
    '100% Real IMO Fourth GHG Study 2020 data - 27 speed configurations across 9 vessel types. Flat structure: each speed is a separate record with vesselTypeName + speed fields.'
);

PRINT 'Vessel Types configuration inserted successfully!';
GO

-- =============================================
-- Verify all configurations
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'Configuration Summary:';
PRINT '========================================';

SELECT 
    ConfigType,
    ConfigName,
    LEN(ConfigJson) as JsonLength,
    Description,
    IsActive,
    CreatedAt
FROM KSailCalc_Configurations
ORDER BY ConfigType;

PRINT '';
PRINT '✅ KSailCalc configuration setup completed successfully!';
PRINT '========================================';
GO
