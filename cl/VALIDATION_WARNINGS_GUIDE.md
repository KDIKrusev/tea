# Validation Warnings - Complete Guide

This document explains all 5 validation warnings with mathematical logic and test scenarios.

---

## **Warning #1: Main Engine Overcapacity** ❌ ERROR

### **Mathematical Logic:**
```
ME_Total_Power = (Propulsion_Power × (1 + Sea_Margin/100)) + SG_Power_Actual
ME_Capacity_Total = ME_Capacity_Per_Engine × ME_Count
ME_Utilization = (ME_Total_Power / ME_Capacity_Total) × 100%

IF ME_Utilization > 100% → ERROR
```

### **Why This Happens:**
The main engines must provide:
1. Propulsion power (with safety margin)
2. Mechanical power to drive the shaft generators

If the total demand exceeds engine capacity, they will be overloaded.

### **Test Scenario:**
```
Inputs:
- Propulsion Power: 38,000 kW
- Hotel Load: 3,000 kW
- Sea Margin: 5%
- ME Capacity per Engine: 20,000 kW
- ME Count: 2
- SG Capacity per Engine: 2,000 kW

Calculation:
- SG Power Actual = min(3,000, 4,000) = 3,000 kW
- ME Propulsion = 38,000 × 1.05 = 39,900 kW
- ME Total Power = 39,900 + 3,000 = 42,900 kW
- ME Capacity = 20,000 × 2 = 40,000 kW
- ME Utilization = (42,900 / 40,000) × 100 = 107.25%

Result: ❌ ERROR - "Main engine utilization > 100%..."
```

### **How to Fix:**
- Reduce propulsion power
- Decrease sea margin
- Reduce hotel load
- Increase ME capacity or add more ME

---

## **Warning #2: Shaft Generator Capacity Exceeds Main Engine** ❌ ERROR

### **Mathematical Logic:**
```
IF SG_Capacity_Per_Engine > ME_Capacity_Per_Engine → ERROR
```

### **Why This Happens:**
Shaft generators are driven by the main engine shaft. A shaft generator cannot produce more power than the engine that drives it. This is a physical impossibility.

### **Test Scenario:**
```
Inputs:
- ME Capacity per Engine: 20,000 kW
- SG Capacity per Engine: 25,000 kW  ← IMPOSSIBLE!

Calculation:
- 25,000 > 20,000 → ERROR

Result: ❌ ERROR - "Shaft generator capacity cannot exceed main engine capacity."
```

### **How to Fix:**
- Reduce SG capacity per engine to be ≤ ME capacity
- Increase ME capacity

---

## **Warning #3: Hotel Load Exceeds Combined SG + AE Capacity** ❌ ERROR

### **Mathematical Logic:**
```
SG_Capacity_Total = SG_Capacity_Per_Engine × ME_Count
AE_Capacity_Total = AE_Capacity_Per_Engine × AE_Count
Combined_Capacity = SG_Capacity_Total + AE_Capacity_Total

IF Hotel_Load > Combined_Capacity → ERROR
```

### **Why This Happens:**
Hotel load must be supplied by:
1. First: Shaft generators (up to their capacity)
2. Second: Auxiliary engines (for the remainder)

If hotel load exceeds what BOTH can provide combined, it's impossible to meet the demand.

### **Test Scenario:**
```
Inputs:
- Hotel Load: 12,000 kW
- SG Capacity per Engine: 2,000 kW
- ME Count: 2
- AE Capacity per Engine: 2,000 kW
- AE Count: 3

Calculation:
- SG Total = 2,000 × 2 = 4,000 kW
- AE Total = 2,000 × 3 = 6,000 kW
- Combined = 4,000 + 6,000 = 10,000 kW
- Hotel Load = 12,000 kW
- 12,000 > 10,000 → ERROR

Result: ❌ ERROR - "Hotel/mission load exceeds combined capacity..."
```

### **How to Fix:**
- Reduce hotel load
- Increase SG capacity
- Increase AE capacity or add more AE

---

## **Warning #4: Auxiliary Engine Overcapacity** ❌ ERROR

### **Mathematical Logic:**
```
AE_Power_Needed = max(0, Hotel_Load - SG_Capacity_Total)
AE_Utilization = (AE_Power_Needed / AE_Capacity_Total) × 100%

IF AE_Utilization > 100% (but Hotel_Load ≤ Combined) → ERROR
```

### **Why This Happens:**
This is a special case where:
- Total capacity (SG + AE) is sufficient
- BUT auxiliary engines alone are overloaded

This happens when SG capacity is too small relative to hotel load.

### **Test Scenario:**
```
Inputs:
- Hotel Load: 9,000 kW
- SG Capacity per Engine: 2,000 kW
- ME Count: 2
- AE Capacity per Engine: 2,000 kW
- AE Count: 2

Calculation:
- SG Total = 2,000 × 2 = 4,000 kW
- AE Total = 2,000 × 2 = 4,000 kW
- Combined = 4,000 + 4,000 = 8,000 kW
- Hotel Load = 9,000 kW
- 9,000 > 8,000? YES → Would trigger Warning #3 instead

Let's adjust:
- Hotel Load: 8,500 kW
- AE Needed = 8,500 - 4,000 = 4,500 kW
- AE Utilization = (4,500 / 4,000) × 100 = 112.5%

Result: ❌ ERROR - "Auxiliary engine utilization > 100%..."
```

### **How to Fix:**
- Reduce hotel load
- Increase AE capacity or add more AE
- Increase SG capacity to reduce AE burden

---

## **Warning #5: Hotel Load Exceeds Shaft Generator Capacity** ⚠️ WARNING

### **Mathematical Logic:**
```
AE_Power_Needed = Hotel_Load - SG_Capacity_Total

IF Hotel_Load > SG_Capacity_Total AND AE_Power_Needed > 0 AND AE_Utilization ≤ 100% → WARNING
```

### **Why This Happens:**
This is an informational warning, not an error. It means:
- Hotel load is higher than what SG can provide alone
- Auxiliary engines must kick in to handle the overflow
- BUT the AE can handle it (utilization < 100%)

This is a valid configuration but you're informed that AE will be running.

### **Test Scenario:**
```
Inputs:
- Hotel Load: 5,000 kW
- SG Capacity per Engine: 2,000 kW
- ME Count: 2
- AE Capacity per Engine: 2,000 kW
- AE Count: 3

Calculation:
- SG Total = 2,000 × 2 = 4,000 kW
- AE Total = 2,000 × 3 = 6,000 kW
- Combined = 4,000 + 6,000 = 10,000 kW
- AE Needed = 5,000 - 4,000 = 1,000 kW
- AE Utilization = (1,000 / 6,000) × 100 = 16.7%
- Hotel > SG? 5,000 > 4,000? YES
- AE Utilization > 100? NO

Result: ⚠️ WARNING - "Hotel power exceeds shaft generator capacity..."
```

### **How to Fix (Optional):**
- Increase SG capacity to handle hotel load alone
- This is just an FYI - the system works fine as-is

---

## **Warning Priority Logic**

The system checks warnings in this order and only shows the most relevant one for hotel load issues:

```
1. IF Hotel > SG + AE → Show ERROR #3 (most critical) → STOP
2. ELSE IF AE Utilization > 100% → Show ERROR #4 → STOP
3. ELSE IF Hotel > SG AND AE_Needed > 0 → Show WARNING #5 → STOP
```

This prevents showing multiple overlapping warnings about the same hotel load issue.

---

## **Summary Table**

| # | Warning | Severity | Trigger Condition | Test Value |
|---|---------|----------|-------------------|------------|
| 1 | Main Engine Overcapacity | ERROR | ME Utilization > 100% | Propulsion = 38,000 kW |
| 2 | SG > ME Capacity | ERROR | SG per engine > ME per engine | SG = 25,000, ME = 20,000 |
| 3 | Hotel > SG + AE | ERROR | Hotel > Combined capacity | Hotel = 12,000 kW |
| 4 | AE Overcapacity | ERROR | AE Utilization > 100% (but combined OK) | Hotel = 8,500 kW with small AE |
| 5 | Hotel > SG (AE OK) | WARNING | Hotel > SG but AE handles it | Hotel = 5,000 kW |

---

## **Mathematical Formulas Summary**

### **Main Engine:**
- ME_Total_Power = (Propulsion × (1 + Sea_Margin%)) + min(Hotel_Load, SG_Capacity)
- ME_Utilization% = (ME_Total_Power / ME_Capacity) × 100

### **Shaft Generator:**
- SG_Power_Actual = min(Hotel_Load, SG_Capacity_Total)
- SG fuel is included in ME fuel (mechanical drive)

### **Auxiliary Engine:**
- AE_Power_Needed = max(0, Hotel_Load - SG_Capacity_Total)
- AE_Utilization% = (AE_Power_Needed / AE_Capacity_Total) × 100

### **Fuel Consumption:**
- Total_FOC = (ME_Power × ME_SFOC + AE_Power × AE_SFOC) / 1000 kg/h
- Annual_FOC = Total_FOC × Annual_Hours / 1000 tons/year

### **CO₂ Emissions:**
- CO₂ = Fuel_Consumption × 3.206 tons CO₂/ton fuel
