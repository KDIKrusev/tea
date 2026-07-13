-- =============================================
-- REFACTORED KSailCalc Configuration Setup V2.0
-- =============================================
-- This script sets up ALL application configurations with NEW STRUCTURE:
-- 1. Main Engine Types (unchanged)
-- 2. Auxiliary Engine Types (unchanged)
-- 3. iEMS Integration Levels (unchanged)
-- 4. Weather Conditions (NEW - Beaufort Scale with True/Apparent Wind for WAPS)
-- 5. Vessel Types (REFACTORED - merged with Operational Mode + DP Mode support)
--
-- ✨ KEY CHANGES:
-- - Operational Mode is now EMBEDDED in VesselType
-- - Speed/Power is now an ARRAY (speedPowerCurve) instead of separate records
-- - DP Mode support for offshore vessels
-- - Weather Conditions with Beaufort Scale (True/Apparent Wind) for WAPS
-- - 27 records → 10 records (63% reduction)
-- - Hotel load auto-calculated from operational profile in frontend
-- =============================================

USE KSailCalc_Configurations;
GO

-- =============================================
-- STEP 1: Clean existing configurations
-- =============================================
PRINT '🧹 Cleaning existing configurations...';

DELETE FROM KSailCalc_Configurations WHERE ConfigType IN ('MainEngine', 'AuxiliaryEngine', 'IntegrationLevel', 'WeatherCondition', 'OperationalMode', 'VesselType');
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
-- STEP 4.5: Insert Weather Conditions Configuration (Beaufort Scale)
-- =============================================
PRINT '🌊 Inserting Weather Conditions (Beaufort Scale with True/Apparent Wind)...';

INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'WeatherCondition',
    'Beaufort Scale Weather Conditions',
    '[
        {
            "beaufortNumber": 0,
            "name": "Calm",
            "description": "Sea like a mirror",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 1,
            "name": "Light Air",
            "description": "Ripples without crests",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 2,
            "name": "Light Breeze",
            "description": "Small wavelets, crests of glassy appearance",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 3,
            "name": "Gentle Breeze",
            "description": "Large wavelets, crests begin to break",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 4,
            "name": "Moderate Breeze",
            "description": "Small waves, fairly frequent white horses",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 5,
            "name": "Fresh Breeze",
            "description": "Moderate waves, many white horses",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 6,
            "name": "Strong Breeze",
            "description": "Large waves, extensive white foam crests",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 7,
            "name": "Near Gale",
            "description": "Sea heaps up, white foam from breaking waves",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 8,
            "name": "Gale",
            "description": "Moderately high waves, breaking crests forming spindrift",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 9,
            "name": "Strong Gale",
            "description": "High waves, dense foam, wave crests start to roll over",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 10,
            "name": "Storm",
            "description": "Very high waves, sea surface white with foam",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 11,
            "name": "Violent Storm",
            "description": "Exceptionally high waves, sea covered with foam",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        },
        {
            "beaufortNumber": 12,
            "name": "Hurricane",
            "description": "Air filled with foam and spray, sea completely white",
            "trueWind": {
                "fromDirectionDegrees": 270
            }
        }
    ]',
    'Beaufort Scale (0-12) weather conditions with wind direction only. Wind speeds (knots) calculated dynamically from Beaufort number using WMO standard scale. Apparent wind calculated dynamically based on vessel speed and course using vector mathematics. Minimal database footprint - only Beaufort number and direction stored. For WAPS (Wind Assisted Propulsion System) optimization - NOT YET USED IN CALCULATIONS.'
);

PRINT '✅ Weather Conditions (Beaufort Scale) inserted successfully!';
GO

-- =============================================
-- STEP 5: Insert REFACTORED Vessel Types Configuration
-- =============================================
PRINT '⚙️ Inserting REFACTORED Vessel Types (with embedded operational profile)...';

INSERT INTO KSailCalc_Configurations (ConfigType, ConfigName, ConfigJson, Description)
VALUES (
    'VesselType',
    'IMO Fourth GHG Study 2020 - Refactored Structure V2.0',
    '[
        {
            "id": 1,
            "vesselTypeName": "Bulk Carrier 10,000 dwt",
            "sizeCategory": "10000-34999",
            "category": "Bulk Carrier",
            "unit": "dwt",
            "description": "Small bulk carrier from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 4,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 200
            },
            "auxEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 2
            },
            "speedPowerCurve": [
                {"speedKnots": 12.0, "calmWaterPowerKW": 1365},
                {"speedKnots": 13.0, "calmWaterPowerKW": 1895},
                {"speedKnots": 14.0, "calmWaterPowerKW": 2663}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 110,
                    "annualHours": 2592,
                    "percentageOfYear": 29.59
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 180,
                    "annualHours": 451,
                    "percentageOfYear": 5.15
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 190,
                    "propulsionPowerKW": 400,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 165,
                    "annualHours": 5717,
                    "percentageOfYear": 65.26
                },
                "dpMode": null
            }
        },
        {
            "id": 2,
            "vesselTypeName": "Bulk Carrier 35,000 dwt",
            "sizeCategory": "35000-59999",
            "category": "Bulk Carrier",
            "unit": "dwt",
            "description": "Medium bulk carrier from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 400
            },
            "auxEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 12.0, "calmWaterPowerKW": 2945},
                {"speedKnots": 13.0, "calmWaterPowerKW": 3801},
                {"speedKnots": 14.0, "calmWaterPowerKW": 4902}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 130,
                    "annualHours": 2592,
                    "percentageOfYear": 29.59
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 230,
                    "annualHours": 451,
                    "percentageOfYear": 5.15
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 250,
                    "propulsionPowerKW": 800,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 230,
                    "annualHours": 5717,
                    "percentageOfYear": 65.26
                },
                "dpMode": null
            }
        },
        {
            "id": 3,
            "vesselTypeName": "Bulk Carrier 63,000 dwt",
            "sizeCategory": "60000-99999",
            "category": "Bulk Carrier",
            "unit": "dwt",
            "description": "Large bulk carrier from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 600
            },
            "auxEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 12.0, "calmWaterPowerKW": 3865},
                {"speedKnots": 13.0, "calmWaterPowerKW": 4941},
                {"speedKnots": 14.0, "calmWaterPowerKW": 6294},
                {"speedKnots": 15.0, "calmWaterPowerKW": 8032}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 240,
                    "annualHours": 1920,
                    "percentageOfYear": 21.92
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 400,
                    "annualHours": 797,
                    "percentageOfYear": 9.10
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 410,
                    "propulsionPowerKW": 1200,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 372,
                    "annualHours": 6043,
                    "percentageOfYear": 68.98
                },
                "dpMode": null
            }
        },
        {
            "id": 4,
            "vesselTypeName": "Bulk Carrier 100,000 dwt",
            "sizeCategory": "100000-199999",
            "category": "Bulk Carrier",
            "unit": "dwt",
            "description": "Very large bulk carrier from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 800
            },
            "auxEngine": {
                "engineTypeId": 3,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 13.0, "calmWaterPowerKW": 2055},
                {"speedKnots": 14.0, "calmWaterPowerKW": 2777},
                {"speedKnots": 15.0, "calmWaterPowerKW": 3755}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 230,
                    "annualHours": 1776,
                    "percentageOfYear": 20.27
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 380,
                    "annualHours": 1094,
                    "percentageOfYear": 12.49
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 380,
                    "propulsionPowerKW": 500,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 362,
                    "annualHours": 5890,
                    "percentageOfYear": 67.24
                },
                "dpMode": null
            }
        },
        {
            "id": 5,
            "vesselTypeName": "Bulk Carrier 180,000 dwt",
            "sizeCategory": "100000-199999",
            "category": "Bulk Carrier",
            "unit": "dwt",
            "description": "Cape-size bulk carrier from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 1200
            },
            "auxEngine": {
                "engineTypeId": 3,
                "numberOfEngines": 4
            },
            "speedPowerCurve": [
                {"speedKnots": 14.0, "calmWaterPowerKW": 11204},
                {"speedKnots": 15.0, "calmWaterPowerKW": 13820}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 230,
                    "annualHours": 1776,
                    "percentageOfYear": 20.27
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 380,
                    "annualHours": 1094,
                    "percentageOfYear": 12.49
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 380,
                    "propulsionPowerKW": 2500,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 362,
                    "annualHours": 5890,
                    "percentageOfYear": 67.24
                },
                "dpMode": null
            }
        },
        {
            "id": 6,
            "vesselTypeName": "Tanker 10,000 dwt",
            "sizeCategory": "10000-19999",
            "category": "Oil Tanker",
            "unit": "dwt",
            "description": "Small tanker from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 4,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 200
            },
            "auxEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 2
            },
            "speedPowerCurve": [
                {"speedKnots": 11.0, "calmWaterPowerKW": 1029},
                {"speedKnots": 12.0, "calmWaterPowerKW": 1378},
                {"speedKnots": 13.0, "calmWaterPowerKW": 1850}
            ],
            "seaMarginPercent": 18.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 360,
                    "annualHours": 1752,
                    "percentageOfYear": 20.0
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 690,
                    "annualHours": 804,
                    "percentageOfYear": 9.18
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 560,
                    "propulsionPowerKW": 300,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 520,
                    "annualHours": 6204,
                    "percentageOfYear": 70.82
                },
                "dpMode": null
            }
        },
        {
            "id": 7,
            "vesselTypeName": "Tanker 50,000 dwt",
            "sizeCategory": "20000-59999",
            "category": "Oil Tanker",
            "unit": "dwt",
            "description": "Medium tanker from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 3,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 500
            },
            "auxEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 13.0, "calmWaterPowerKW": 4021},
                {"speedKnots": 14.0, "calmWaterPowerKW": 5151},
                {"speedKnots": 15.0, "calmWaterPowerKW": 6629}
            ],
            "seaMarginPercent": 18.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 410,
                    "annualHours": 1752,
                    "percentageOfYear": 20.0
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 590,
                    "annualHours": 804,
                    "percentageOfYear": 9.18
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 600,
                    "propulsionPowerKW": 1100,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 565,
                    "annualHours": 6204,
                    "percentageOfYear": 70.82
                },
                "dpMode": null
            }
        },
        {
            "id": 8,
            "vesselTypeName": "Tanker 105,000 dwt",
            "sizeCategory": "60000-119999",
            "category": "Oil Tanker",
            "unit": "dwt",
            "description": "Large tanker (Aframax) from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 800
            },
            "auxEngine": {
                "engineTypeId": 3,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 13.0, "calmWaterPowerKW": 6167},
                {"speedKnots": 14.0, "calmWaterPowerKW": 8392},
                {"speedKnots": 15.0, "calmWaterPowerKW": 9575}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 520,
                    "annualHours": 1752,
                    "percentageOfYear": 20.0
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 870,
                    "annualHours": 804,
                    "percentageOfYear": 9.18
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 760,
                    "propulsionPowerKW": 1600,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 715,
                    "annualHours": 6204,
                    "percentageOfYear": 70.82
                },
                "dpMode": null
            }
        },
        {
            "id": 9,
            "vesselTypeName": "Tanker 300,000 dwt",
            "sizeCategory": "120000+",
            "category": "Oil Tanker",
            "unit": "dwt",
            "description": "Very large tanker (VLCC) from IMO Fourth GHG Study 2020",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 1,
                "numberOfEngines": 1,
                "shaftGeneratorMaxCapacityKW": 1500
            },
            "auxEngine": {
                "engineTypeId": 3,
                "numberOfEngines": 4
            },
            "speedPowerCurve": [
                {"speedKnots": 13.0, "calmWaterPowerKW": 10969},
                {"speedKnots": 14.0, "calmWaterPowerKW": 13603},
                {"speedKnots": 15.0, "calmWaterPowerKW": 16688}
            ],
            "seaMarginPercent": 20.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 680,
                    "annualHours": 1752,
                    "percentageOfYear": 20.0
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 1190,
                    "annualHours": 804,
                    "percentageOfYear": 9.18
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 1150,
                    "propulsionPowerKW": 2800,
                    "annualHours": 175,
                    "percentageOfYear": 2.0
                },
                "transitMode": {
                    "hotelLoadPowerKW": 1063,
                    "annualHours": 6204,
                    "percentageOfYear": 70.82
                },
                "dpMode": null
            }
        },
        {
            "id": 10,
            "vesselTypeName": "Offshore Support Vessel",
            "sizeCategory": "5000-10000",
            "category": "Offshore Support",
            "unit": "dwt",
            "description": "Offshore Support Vessel with Dynamic Positioning capabilities",
            "isActive": true,
            "mainEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 2,
                "shaftGeneratorMaxCapacityKW": 1000
            },
            "auxEngine": {
                "engineTypeId": 2,
                "numberOfEngines": 3
            },
            "speedPowerCurve": [
                {"speedKnots": 10.0, "calmWaterPowerKW": 1500},
                {"speedKnots": 11.0, "calmWaterPowerKW": 1950},
                {"speedKnots": 12.0, "calmWaterPowerKW": 2500},
                {"speedKnots": 13.0, "calmWaterPowerKW": 3200}
            ],
            "seaMarginPercent": 15.0,
            "operationalProfile": {
                "portMode": {
                    "hotelLoadPowerKW": 150,
                    "annualHours": 1200,
                    "percentageOfYear": 13.70
                },
                "anchorMode": {
                    "hotelLoadPowerKW": 200,
                    "annualHours": 800,
                    "percentageOfYear": 9.13
                },
                "maneuveringMode": {
                    "hotelLoadPowerKW": 250,
                    "propulsionPowerKW": 500,
                    "annualHours": 400,
                    "percentageOfYear": 4.57
                },
                "transitMode": {
                    "hotelLoadPowerKW": 220,
                    "annualHours": 4000,
                    "percentageOfYear": 45.66
                },
                "dpMode": {
                    "hotelLoadPowerKW": 300,
                    "annualHours": 2360,
                    "percentageOfYear": 26.94,
                    "requiredDPPowerKW": 3500,
                    "weatherConditions": [
                        {
                            "condition": "Calm",
                            "thrustDemandFactor": 1.0,
                            "minAverageThrustPowerKW": 3500
                        },
                        {
                            "condition": "Moderate",
                            "thrustDemandFactor": 1.3,
                            "minAverageThrustPowerKW": 4550
                        },
                        {
                            "condition": "Rough",
                            "thrustDemandFactor": 1.7,
                            "minAverageThrustPowerKW": 5950
                        }
                    ]
                }
            }
        }
    ]',
    'IMO Fourth GHG Study 2020 - REFACTORED V2.0: 10 vessel types with embedded operational profiles and speed/power curves. Includes Offshore Support Vessel with DP Mode capabilities. Hotel load auto-calculated from operational profile weighted average in frontend. OperationalMode config is DEPRECATED.'
);

PRINT '✅ REFACTORED Vessel Types configuration inserted successfully!';
GO

-- =============================================
-- FINAL SUMMARY
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ REFACTORED CONFIGURATION V2.0 COMPLETE!';
PRINT '========================================';
PRINT '';
PRINT '📊 Configuration Summary:';
PRINT '  ✅ Main Engine Types: 5 engines';
PRINT '  ✅ Auxiliary Engine Types: 4 engines';
PRINT '  ✅ iEMS Integration Levels: 3 levels';
PRINT '  ✅ Weather Conditions: 13 Beaufort scale levels (0-12) with True/Apparent Wind';
PRINT '  ✅ Vessel Types: 10 vessel types (REFACTORED - includes 1 with DP Mode)';
PRINT '  ❌ Operational Mode: REMOVED (merged into VesselType)';
PRINT '';
PRINT '🔥 NEW STRUCTURE FEATURES:';
PRINT '  ✨ Speed/Power as array (speedPowerCurve)';
PRINT '  ✨ Operational profile embedded in vessel type';
PRINT '  ✨ DP Mode support for offshore vessels';
PRINT '  ✨ Weather conditions with True/Apparent Wind for WAPS';
PRINT '  ✨ 27 records → 10 records (63% reduction)';
PRINT '  ✨ Better data organization and maintainability';
PRINT '  ✨ Percentage & hours both available';
PRINT '';
PRINT '⚙️ HOTEL LOAD FEATURE:';
PRINT '  Hotel load is AUTO-CALCULATED from operational profile weighted average.';
PRINT '  Formula: (portKW × portHours + anchorKW × anchorHours + maneuveringKW × maneuveringHours + transitKW × transitHours) / annualHours';
PRINT '';
PRINT '🌊 WEATHER CONDITIONS:';
PRINT '  Beaufort Scale 0-12 with wind speed ranges only (sea state data not included).';
PRINT '  True Wind: Meteorological forecast wind (relative to earth/water).';
PRINT '  Apparent Wind: CALCULATED DYNAMICALLY using vessel speed & course.';
PRINT '  Formula: ApparentWind = f(TrueWind, VesselSpeed, VesselCourse, TrueWindDirection)';
PRINT '  Future use: WAPS (Wind Assisted Propulsion System) optimization and weather routing.';
PRINT '';
PRINT '🚀 Ready to start application with new structure!';
PRINT '========================================';
GO
