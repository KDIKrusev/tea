-- =============================================
-- COMPLETE KSailCalc Configuration Setup (AUTO-CALCULATED HOTEL LOAD)
-- =============================================
-- This script sets up ALL application configurations:
-- 1. Main Engine Types
-- 2. Auxiliary Engine Types  
-- 3. iEMS Integration Levels
-- 4. Vessel Operational Mode Profiles
-- 5. Vessel Types (WITHOUT hotelPowerKW - auto-calculated from operational profile)
--
-- Hotel load formula: (portKW × portHours + anchorKW × anchorHours + maneuveringKW × maneuveringHours + transitKW × transitHours + dpKW × dpHours) / annualHours
-- =============================================

USE KSailCalc_Configurations;
GO

-- =============================================
-- STEP 1: Clean existing configurations
-- =============================================
PRINT '🧹 Cleaning existing configurations...';

DELETE FROM KSailCalc_Configurations WHERE ConfigType IN ('MainEngine', 'AuxiliaryEngine', 'IntegrationLevel', 'OperationalMode', 'VesselType');
GO

PRINT '✅ Old configurations deleted.';
GO

-- =============================================
-- STEP 2: Insert Main Engine Types Configuration
-- =============================================
PRINT '⚙️ Inserting Main Engine Types...';

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
            "description": "Large marine diesel 2-stroke engine",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 176.14},
                {"load": 0.5, "sfoc": 172.28},
                {"load": 0.75, "sfoc": 168.85},
                {"load": 0.9, "sfoc": 167.69},
                {"load": 1.0, "sfoc": 169.04}
            ]
        },
        {
            "id": 2,
            "name": "Diesel 2-stroke Medium",
            "maxCapacityKW": 15000,
            "shaftGeneratorMaxCapacityKW": 2000,
            "description": "Medium marine diesel 2-stroke engine",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 178.50},
                {"load": 0.5, "sfoc": 174.20},
                {"load": 0.75, "sfoc": 170.30},
                {"load": 0.9, "sfoc": 169.10},
                {"load": 1.0, "sfoc": 170.80}
            ]
        },
        {
            "id": 3,
            "name": "Diesel 4-stroke Large",
            "maxCapacityKW": 20000,
            "shaftGeneratorMaxCapacityKW": 2500,
            "description": "Large 4-stroke marine engine",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 185.20},
                {"load": 0.5, "sfoc": 180.45},
                {"load": 0.75, "sfoc": 175.60},
                {"load": 0.9, "sfoc": 174.20},
                {"load": 1.0, "sfoc": 176.30}
            ]
        },
        {
            "id": 4,
            "name": "Diesel 4-stroke Medium",
            "maxCapacityKW": 12000,
            "shaftGeneratorMaxCapacityKW": 1500,
            "description": "Medium 4-stroke marine engine",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 188.75},
                {"load": 0.5, "sfoc": 183.90},
                {"load": 0.75, "sfoc": 178.40},
                {"load": 0.9, "sfoc": 177.15},
                {"load": 1.0, "sfoc": 179.50}
            ]
        },
        {
            "id": 5,
            "name": "Dual Fuel Engine",
            "maxCapacityKW": 22000,
            "shaftGeneratorMaxCapacityKW": 2800,
            "description": "LNG/Diesel dual fuel engine",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 165.40},
                {"load": 0.5, "sfoc": 162.15},
                {"load": 0.75, "sfoc": 159.30},
                {"load": 0.9, "sfoc": 158.80},
                {"load": 1.0, "sfoc": 160.20}
            ]
        }
    ]',
    'Configuration for main engine types dropdown with SFOC curves'
);

PRINT '✅ Main Engine Types inserted successfully!';
GO

-- =============================================
-- STEP 3: Insert Auxiliary Engine Types Configuration
-- =============================================
PRINT '⚙️ Inserting Auxiliary Engine Types...';

INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'AuxiliaryEngine',
    'Auxiliary Engine Types Configuration',
    '[
        {
            "id": 1,
            "name": "Auxiliary Diesel 4-stroke Small",
            "maxCapacityKW": 500,
            "description": "Small auxiliary diesel generator",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 230.50},
                {"load": 0.5, "sfoc": 215.40},
                {"load": 0.75, "sfoc": 208.60},
                {"load": 0.9, "sfoc": 206.20},
                {"load": 1.0, "sfoc": 208.75}
            ]
        },
        {
            "id": 2,
            "name": "Auxiliary Diesel 4-stroke Medium",
            "maxCapacityKW": 1000,
            "description": "Medium auxiliary diesel generator",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 220.36},
                {"load": 0.5, "sfoc": 202.81},
                {"load": 0.75, "sfoc": 196.22},
                {"load": 0.9, "sfoc": 194.04},
                {"load": 1.0, "sfoc": 195.91}
            ]
        },
        {
            "id": 3,
            "name": "Auxiliary Diesel 4-stroke Large",
            "maxCapacityKW": 2000,
            "description": "Large auxiliary diesel generator",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 215.80},
                {"load": 0.5, "sfoc": 198.45},
                {"load": 0.75, "sfoc": 192.15},
                {"load": 0.9, "sfoc": 190.30},
                {"load": 1.0, "sfoc": 192.60}
            ]
        },
        {
            "id": 4,
            "name": "Auxiliary Dual Fuel",
            "maxCapacityKW": 1500,
            "description": "Dual fuel auxiliary generator",
            "sfocData": [
                {"load": 0, "sfoc": 0},
                {"load": 0.25, "sfoc": 205.75},
                {"load": 0.5, "sfoc": 188.90},
                {"load": 0.75, "sfoc": 182.40},
                {"load": 0.9, "sfoc": 180.85},
                {"load": 1.0, "sfoc": 183.20}
            ]
        }
    ]',
    'Configuration for auxiliary engine types dropdown with SFOC curves'
);

PRINT '✅ Auxiliary Engine Types inserted successfully!';
GO

-- =============================================
-- STEP 4: Insert iEMS Integration Levels Configuration
-- =============================================
PRINT '⚙️ Inserting iEMS Integration Levels...';

INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'IntegrationLevel',
    'iEMS Integration Levels',
    '[
        {
            "level": "1",
            "baseEfficiencyFactor": 0.97,
            "iemsPriceNOK": 1000000,
            "commissioningNOK": 100000,
            "description": "3% FOC reduction - Entry level integration"
        },
        {
            "level": "2",
            "baseEfficiencyFactor": 0.955,
            "iemsPriceNOK": 1205000,
            "commissioningNOK": 200000,
            "description": "4.5% FOC reduction - Mid-tier integration with enhanced optimization"
        },
        {
            "level": "3",
            "baseEfficiencyFactor": 0.94,
            "iemsPriceNOK": 1800000,
            "commissioningNOK": 300000,
            "description": "6% FOC reduction - Premium integration with advanced AI-driven optimization"
        }
    ]',
    'iEMS Integration Levels (Advanced, Pro, Premium) with efficiency factors, pricing, and commissioning costs. BaseEfficiencyFactor determines FOC reduction multiplier.'
);

PRINT '✅ iEMS Integration Levels inserted successfully!';
GO

-- =============================================
-- STEP 5: Insert Operational Mode Profiles Configuration
-- =============================================
PRINT '⚙️ Inserting Operational Mode Profiles...';

INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'OperationalMode',
    'Vessel Operational Mode Profiles - IMO Data',
    '[
        {
            "vesselTypeName": "Bulk Carrier 10,000 dwt",
            "sizeCategory": "10000-34999",
            "port": {
                "hotelMissionPowerKW": 110,
                "hoursPerYear": 2592
            },
            "anchor": {
                "hotelMissionPowerKW": 180,
                "hoursPerYear": 451
            },
            "maneuvering": {
                "hotelMissionPowerKW": 190,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 165,
                "hoursPerYear": 5717
            },
            "dp": null
        },
        {
            "vesselTypeName": "Bulk Carrier 35,000 dwt",
            "sizeCategory": "35000-59999",
            "port": {
                "hotelMissionPowerKW": 130,
                "hoursPerYear": 2592
            },
            "anchor": {
                "hotelMissionPowerKW": 230,
                "hoursPerYear": 451
            },
            "maneuvering": {
                "hotelMissionPowerKW": 250,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 230,
                "hoursPerYear": 5717
            },
            "dp": null
        },
        {
            "vesselTypeName": "Bulk Carrier 63,000 dwt",
            "sizeCategory": "60000-99999",
            "port": {
                "hotelMissionPowerKW": 240,
                "hoursPerYear": 1920
            },
            "anchor": {
                "hotelMissionPowerKW": 400,
                "hoursPerYear": 797
            },
            "maneuvering": {
                "hotelMissionPowerKW": 410,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 372,
                "hoursPerYear": 6043
            },
            "dp": null
        },
        {
            "vesselTypeName": "Bulk Carrier 100,000 dwt",
            "sizeCategory": "100000-199999",
            "port": {
                "hotelMissionPowerKW": 230,
                "hoursPerYear": 1776
            },
            "anchor": {
                "hotelMissionPowerKW": 380,
                "hoursPerYear": 1094
            },
            "maneuvering": {
                "hotelMissionPowerKW": 380,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 362,
                "hoursPerYear": 5890
            },
            "dp": null
        },
        {
            "vesselTypeName": "Bulk Carrier 180,000 dwt",
            "sizeCategory": "100000-199999",
            "port": {
                "hotelMissionPowerKW": 230,
                "hoursPerYear": 1776
            },
            "anchor": {
                "hotelMissionPowerKW": 380,
                "hoursPerYear": 1094
            },
            "maneuvering": {
                "hotelMissionPowerKW": 380,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 362,
                "hoursPerYear": 5890
            },
            "dp": null
        },
        {
            "vesselTypeName": "Tanker 10,000 dwt",
            "sizeCategory": "10000-19999",
            "port": {
                "hotelMissionPowerKW": 360,
                "hoursPerYear": 1752
            },
            "anchor": {
                "hotelMissionPowerKW": 690,
                "hoursPerYear": 804
            },
            "maneuvering": {
                "hotelMissionPowerKW": 560,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 520,
                "hoursPerYear": 6204
            },
            "dp": null
        },
        {
            "vesselTypeName": "Tanker 50,000 dwt",
            "sizeCategory": "20000-59999",
            "port": {
                "hotelMissionPowerKW": 410,
                "hoursPerYear": 1752
            },
            "anchor": {
                "hotelMissionPowerKW": 590,
                "hoursPerYear": 804
            },
            "maneuvering": {
                "hotelMissionPowerKW": 600,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 565,
                "hoursPerYear": 6204
            },
            "dp": null
        },
        {
            "vesselTypeName": "Tanker 105,000 dwt",
            "sizeCategory": "60000-119999",
            "port": {
                "hotelMissionPowerKW": 520,
                "hoursPerYear": 1752
            },
            "anchor": {
                "hotelMissionPowerKW": 870,
                "hoursPerYear": 804
            },
            "maneuvering": {
                "hotelMissionPowerKW": 760,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 715,
                "hoursPerYear": 6204
            },
            "dp": null
        },
        {
            "vesselTypeName": "Tanker 300,000 dwt",
            "sizeCategory": "120000+",
            "port": {
                "hotelMissionPowerKW": 680,
                "hoursPerYear": 1752
            },
            "anchor": {
                "hotelMissionPowerKW": 1190,
                "hoursPerYear": 804
            },
            "maneuvering": {
                "hotelMissionPowerKW": 1150,
                "hoursPerYear": 0
            },
            "transit": {
                "hotelMissionPowerKW": 1063,
                "hoursPerYear": 6204
            },
            "dp": null
        }
    ]',
    'Operational mode power profiles and time distribution per vessel type from IMO Fourth GHG Study 2020. Includes port, anchor, maneuvering, transit, and DP modes with hourly breakdown.'
);

PRINT '✅ Operational Mode Profiles inserted successfully!';
GO

-- =============================================
-- STEP 6: Insert Vessel Types Configuration (WITHOUT hotelPowerKW)
-- =============================================
PRINT '⚙️ Inserting Vessel Types (auto-calculated hotel load)...';
INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'VesselType',
    'IMO Fourth GHG Study 2020 - Real Data (Bulk Carriers & Tankers) - AUTO-CALCULATED HOTEL LOAD',
    '[
        {"id": 1, "vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 12.0, "calmWaterPowerKW": 1365, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        {"id": 2, "vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 13.0, "calmWaterPowerKW": 1895, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        {"id": 3, "vesselTypeName": "Bulk Carrier 10,000 dwt", "speed": 14.0, "calmWaterPowerKW": 2663, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        
        {"id": 4, "vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 12.0, "calmWaterPowerKW": 2945, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 2, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 400}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 5, "vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 13.0, "calmWaterPowerKW": 3801, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 2, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 400}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 6, "vesselTypeName": "Bulk Carrier 35,000 dwt", "speed": 14.0, "calmWaterPowerKW": 4902, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 2, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 400}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        
        {"id": 7, "vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 12.0, "calmWaterPowerKW": 3865, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 12 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 600}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 8, "vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 13.0, "calmWaterPowerKW": 4941, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 600}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 9, "vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 14.0, "calmWaterPowerKW": 6294, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 600}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 10, "vesselTypeName": "Bulk Carrier 63,000 dwt", "speed": 15.0, "calmWaterPowerKW": 8032, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 600}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        
        {"id": 11, "vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 13.0, "calmWaterPowerKW": 2055, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        {"id": 12, "vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 14.0, "calmWaterPowerKW": 2777, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        {"id": 13, "vesselTypeName": "Bulk Carrier 100,000 dwt", "speed": 15.0, "calmWaterPowerKW": 3755, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        
        {"id": 14, "vesselTypeName": "Bulk Carrier 180,000 dwt", "speed": 14.0, "calmWaterPowerKW": 11204, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 1200}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 4}},
        {"id": 15, "vesselTypeName": "Bulk Carrier 180,000 dwt", "speed": 15.0, "calmWaterPowerKW": 13820, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_DryBulk.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 1200}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 4}},
        
        {"id": 16, "vesselTypeName": "Tanker 10,000 dwt", "speed": 11.0, "calmWaterPowerKW": 1029, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 11 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        {"id": 17, "vesselTypeName": "Tanker 10,000 dwt", "speed": 12.0, "calmWaterPowerKW": 1378, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 12 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        {"id": 18, "vesselTypeName": "Tanker 10,000 dwt", "speed": 13.0, "calmWaterPowerKW": 1850, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 4, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 200}, "auxEngine": {"engineTypeId": 1, "numberOfEngines": 2}},
        
        {"id": 19, "vesselTypeName": "Tanker 50,000 dwt", "speed": 13.0, "calmWaterPowerKW": 4021, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 3, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 500}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 20, "vesselTypeName": "Tanker 50,000 dwt", "speed": 14.0, "calmWaterPowerKW": 5151, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 3, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 500}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        {"id": 21, "vesselTypeName": "Tanker 50,000 dwt", "speed": 15.0, "calmWaterPowerKW": 6629, "seaMargin": 18.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 3, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 500}, "auxEngine": {"engineTypeId": 2, "numberOfEngines": 3}},
        
        {"id": 22, "vesselTypeName": "Tanker 105,000 dwt", "speed": 13.0, "calmWaterPowerKW": 6167, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        {"id": 23, "vesselTypeName": "Tanker 105,000 dwt", "speed": 14.0, "calmWaterPowerKW": 8392, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        {"id": 24, "vesselTypeName": "Tanker 105,000 dwt", "speed": 15.0, "calmWaterPowerKW": 9575, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 800}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 3}},
        
        {"id": 25, "vesselTypeName": "Tanker 300,000 dwt", "speed": 13.0, "calmWaterPowerKW": 10969, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 13 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 1500}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 4}},
        {"id": 26, "vesselTypeName": "Tanker 300,000 dwt", "speed": 14.0, "calmWaterPowerKW": 13603, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 14 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 1500}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 4}},
        {"id": 27, "vesselTypeName": "Tanker 300,000 dwt", "speed": 15.0, "calmWaterPowerKW": 16688, "seaMargin": 20.0, "description": "ME from IMO_SpeedPower_Tankers.xlsx at 15 knots. Hotel load auto-calculated from operational profile.", "mainEngine": {"engineTypeId": 1, "numberOfEngines": 1, "shaftGeneratorMaxCapacityKW": 1500}, "auxEngine": {"engineTypeId": 3, "numberOfEngines": 4}}
    ]',
    'Real IMO Fourth GHG Study 2020 data - 27 speed configurations across 9 vessel types. Hotel load is AUTO-CALCULATED from operational profile weighted average in frontend. Engine configurations included.'
);

PRINT '✅ Clean Vessel Types configuration (without hotelPowerKW) inserted successfully!';
GO

-- =============================================
-- FINAL SUMMARY
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ ALL CONFIGURATIONS INSERTED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT '📊 Configuration Summary:';
PRINT '  ✅ Main Engine Types: 5 engines';
PRINT '  ✅ Auxiliary Engine Types: 4 engines';
PRINT '  ✅ iEMS Integration Levels: 3 levels';
PRINT '  ✅ Operational Mode Profiles: 9 vessel types';
PRINT '  ✅ Vessel Types: 27 speed configurations (9 vessel types)';
PRINT '';
PRINT '⚙️ HOTEL LOAD FEATURE:';
PRINT '  Hotel load is AUTO-CALCULATED from operational profile weighted average.';
PRINT '  Formula: (portKW × portHours + anchorKW × anchorHours + maneuveringKW × maneuveringHours + transitKW × transitHours + dpKW × dpHours) / annualHours';
PRINT '';
PRINT '🚀 Ready to start application!';
PRINT '========================================';
GO
