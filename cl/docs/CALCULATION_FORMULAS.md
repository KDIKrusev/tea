# iEMS Savings Calculator - How It Works

This document explains **how the iEMS Savings Calculator calculates fuel savings, CO2 reductions, and payback period** based on your vessel's data.

## Quick Summary

The calculator compares your vessel's **current fuel consumption** (based on your engine configuration and usage) with the **estimated fuel consumption after installing iEMS**. The difference is your savings!

---

## What You'll See (With Default Values)

When you use the default values in the calculator, you'll get these results:

| Metric | Value | What It Means |
|--------|-------|---------------|
| **Total Annual Fuel Cost Savings** | **$1,449,626** | Money saved per year on fuel |
| **Total Annual Fuel Savings** | **1,812.03 MT** | Tons of fuel saved per year |
| **Annual CO2 Emission Reduction** | **5,809.38 MT** | Tons of CO2 emissions reduced per year |
| **Simple Payback Period** | **0.1 years** | Time to recover iEMS investment (~1.2 months) |
| **iEMS Price** | **$210,000** | Total cost (system + commissioning) |

---

## Step 1: What Information Do You Provide?

You enter information about your vessel in the calculator. Here's what each input means:

### Your Vessel's Power Requirements

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **Avg Propulsion Power** | How much power your propellers need on average | 20,000 kW |
| **Avg Hotel/Mission Load** | Power for lights, HVAC, equipment, etc. | 3,000 kW |
| **Sea Margin** | Extra power buffer for rough seas | 5% |

**→ This tells the calculator your vessel needs about 24,000 kW total** (20,000 × 1.05 + 3,000)

### Your Main Engines

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **Main Engine Capacity Per Engine** | Maximum power ONE main engine can produce | 20,000 kW |
| **Number of Main Engines** | How many main engines installed | 2 |

**→ Total main engine capacity: 20,000 × 2 = 40,000 kW**

### Your Shaft Generators

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **Shaft Generator Capacity Per Engine** | Power ONE shaft generator can produce (per main engine) | 2,000 kW |

**→ Total shaft generator capacity: 2,000 × 2 = 4,000 kW**  
**→ Shaft generator provides up to 3,000 kW** (limited by hotel load demand)

### Your Auxiliary Engines

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **Aux Engine Capacity Per Engine** | Maximum power ONE auxiliary engine can produce | 2,000 kW |
| **No. Aux Engines (Installed)** | How many aux engines you have | 3 |

**→ Total aux engine capacity: 2,000 × 3 = 6,000 kW**  
**→ In the default scenario, aux engines aren't needed** (shaft generator covers all hotel loads)

### Optional Systems

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **SAIL Installed** | Do you have SAIL system? | No |
| **Battery Capacity** | How much battery storage do you have? | 0 kWh |

**→ These give additional fuel savings (0.5% each) if installed**

### Economic Information

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **Fuel Price** | What you pay per ton of fuel | $800/ton |
| **Annual Operating Hours** | How many hours per year your vessel operates | 8,000 hours |

### iEMS System Selection

| What You Enter | What It Means | Default Value |
|----------------|---------------|---------------|
| **iEMS Variant** | Which iEMS system you want (Advanced/Pro/Premium) | Premium |

Each variant gives different fuel savings:
- **Advanced**: 3% fuel reduction, costs $110,000
- **Pro**: 4.5% fuel reduction, costs $140,500
- **Premium**: 6% fuel reduction, costs $210,000

---

## Step 2: How Does the Calculator Figure Out Your Current Fuel Consumption?

The calculator needs to know how much fuel you're burning NOW (before iEMS).

### Understanding SFOC (Specific Fuel Oil Consumption)

**SFOC** tells us how many grams of fuel an engine burns to produce 1 kWh of power. It varies based on how hard the engine is working.

**Main Engine SFOC** (at different load levels):
- At 25% load: 176.14 g/kWh (inefficient - engine running too light)
- At 50% load: 172.28 g/kWh (better)
- At 75% load: 168.85 g/kWh (most efficient range)
- At 100% load: 169.04 g/kWh (slightly less efficient at max)

**Auxiliary Engine SFOC** (generally less efficient than main engines):
- At 25% load: 220.36 g/kWh
- At 50% load: 202.81 g/kWh
- At 75% load: 196.22 g/kWh
- At 100% load: 195.91 g/kWh

### Calculating Your Main Engine Fuel Consumption

With default values:
1. **Propulsion power needed**: 20,000 × 1.05 = 21,000 kW
2. **Shaft generator power**: Min(3,000, 4,000) = 3,000 kW (limited by hotel demand)
3. **Total Main Engine power**: 21,000 + 3,000 = 24,000 kW
4. **Main Engine load percentage**: 24,000 ÷ 40,000 = 60%
5. **SFOC at 60% load**: ~170.8 g/kWh (calculator interpolates between 50% and 75%)
6. **Fuel consumption rate**: 24,000 kW × 170.8 g/kWh = 4,099,200 grams/hour = **4,099.2 kg/hour**

### Calculating Your Auxiliary Engine Fuel Consumption

With default values:
- **Hotel loads**: 3,000 kW
- **Shaft generator provides**: 3,000 kW (enough to cover all hotel loads)
- **Aux engines needed**: 3,000 - 3,000 = 0 kW
- **Fuel consumption**: 0 kg/hour

### Your Total Current Fuel Consumption

- **Per hour**: 4,099.2 kg/hour
- **Per year**: 4,099.2 kg/h × 8,000 hours = 32,793,600 kg = **32,793.6 tons/year**

**This is how much fuel you're burning NOW (without iEMS).**

---

## Step 3: How Does iEMS Reduce Your Fuel Consumption?

**iEMS optimizes your power management system** to use fuel more efficiently. Each variant provides different savings:

| iEMS Variant | Fuel Reduction | How It Works |
|--------------|----------------|--------------|
| **Advanced** | 3% | Basic optimization algorithms |
| **Pro** | 4.5% | Advanced load balancing |
| **Premium** | 6% | Full optimization suite with predictive algorithms |

### With Default Values (Premium iEMS):

**Current consumption**: 30,201.6 tons/year  
**iEMS efficiency factor**: 0.94 (saves 6%)  
**New consumption with iEMS**: 30,201.6 × 0.94 = **28,389.5 tons/year**

### Bonus Savings from Optional Systems:

If you have these systems, you get additional savings:
- **SAIL installed**: Extra 0.5% reduction (multiply by 0.995)
- **Battery installed**: Extra 0.5% reduction (multiply by 0.995)

---

## Step 4: Calculating Your Savings

Now we compare BEFORE and AFTER to see your savings:

### Fuel Savings

**Before iEMS**: 30,201.6 tons/year  
**After iEMS**: 28,389.5 tons/year  
**Fuel Saved**: 30,201.6 - 28,389.5 = **1,812.1 tons/year**

### Cost Savings

**Fuel saved**: 1,812.1 tons/year  
**Fuel price**: $800/ton  
**Money saved**: 1,812.1 × $800 = **$1,449,680/year**

### CO2 Emission Reduction

Every ton of fuel burned produces 3.206 tons of CO2.

**CO2 saved**: 1,812.1 tons fuel × 3.206 = **5,809.6 tons CO2/year**

This is equivalent to:
- Taking ~1,260 cars off the road for a year
- Planting ~267,000 tree seedlings

---

## Step 5: How Long Until You Break Even?

### iEMS System Cost

| Cost Component | Premium Price (NOK) | USD Equivalent |
|----------------|---------------------|----------------|
| iEMS System | 1,800,000 NOK | $180,000 |
| Commissioning | 300,000 NOK | $30,000 |
| **Total** | **2,100,000 NOK** | **$210,000** |

*(Using conversion: 10 NOK = 1 USD)*

### Payback Calculation

**Total investment**: $210,000  
**Annual savings**: $1,449,680/year  
**Payback period**: $210,000 ÷ $1,449,680 = **0.145 years**

**That's about 1.7 months!**

After this short period, all savings go straight to your bottom line.

---

## Summary: The Complete Picture

Here's the full calculation flow in simple terms:

### 📊 Your Current Situation (Without iEMS)

1. **Your vessel needs**: ~24,000 kW of power
   - 20,000 kW for propulsion (+ 5% sea margin)
   - 3,000 kW for hotel/mission loads

2. **Your engines are providing**:
   - Main engines: 22,000 kW (at 55% capacity)
   - Shaft generators: 2,000 kW (at 50% capacity)
   - Auxiliary engines: 0 kW (not needed)

3. **Your fuel consumption**: 
   - Main engines burn at 171.6 g/kWh (at 55% load)
   - Total consumption: 3,775 kg/hour
   - **Annual consumption: 30,201.6 tons/year**

4. **Your costs**:
   - Fuel: 30,201.6 tons × $800 = **$24,161,280/year**
   - CO2 emissions: 30,201.6 × 3.206 = **96,826 tons CO2/year**

### ✅ With iEMS Premium

1. **iEMS optimizes your power management**
   - Efficiency factor: 0.94 (6% reduction)
   
2. **Your new fuel consumption**:
   - 30,201.6 × 0.94 = **28,389.5 tons/year**

3. **Your new costs**:
   - Fuel: 28,389.5 tons × $800 = **$22,711,600/year**
   - CO2 emissions: 28,389.5 × 3.206 = **91,017 tons CO2/year**

### 💰 Your Savings

| Metric | Amount |
|--------|--------|
| **Fuel saved** | 1,812.1 tons/year |
| **Money saved** | $1,449,680/year |
| **CO2 reduced** | 5,809 tons/year |
| **Investment** | $210,000 (one-time) |
| **Payback time** | 1.7 months |
| **10-year savings** | $14,496,800 |

---

## The Bar Chart Explained

The calculator shows you a visual comparison with two sets of bars:

### Bar 1: Annual Fuel Consumption
- **Orange bar (Actual)**: 30,201.6 tons - what you burn now
- **Green bar (With iEMS)**: 28,389.5 tons - what you'll burn with iEMS
- **Difference**: 1,812.1 tons saved

### Bar 2: Annual CO2 Emissions
- **Orange bar (Actual)**: 96,826 tons - your current emissions
- **Green bar (With iEMS)**: 91,017 tons - emissions with iEMS
- **Difference**: 5,809 tons CO2 reduced

---

## Quick Reference: Key Formulas

For those who want the technical details:

### 1. Current Fuel Consumption
```
Hourly_Fuel_kg = (ME_Power_kW × ME_SFOC_g/kWh + AE_Power_kW × AE_SFOC_g/kWh) ÷ 1000
Annual_Fuel_tons = (Hourly_Fuel_kg × Annual_Hours) ÷ 1000
```

### 2. Fuel with iEMS
```
Annual_Fuel_with_iEMS = Annual_Fuel_tons × iEMS_Efficiency_Factor
```

### 3. Savings
```
Fuel_Savings_tons = Annual_Fuel_tons - Annual_Fuel_with_iEMS
Cost_Savings_USD = Fuel_Savings_tons × Fuel_Price_per_ton
CO2_Savings_tons = Fuel_Savings_tons × 3.206
```

### 4. Payback
```
Payback_Years = iEMS_Total_Cost_USD ÷ Annual_Cost_Savings_USD
```

---

## Important Notes

### 📌 What the Calculator Assumes

1. **Shaft Generator Fuel**: The shaft generator gets power from the main engine shaft, so its fuel use is already counted in the main engine's consumption.

2. **Auxiliary Engine Distribution**: If aux engines are needed, the load is split evenly among the running engines.

3. **SFOC Accuracy**: The calculator interpolates between known SFOC values to get accurate fuel consumption at your specific engine load.

4. **CO2 Factor**: Every ton of marine fuel oil produces 3.206 tons of CO2 when burned (industry standard).

5. **Multiple Systems**: If you add SAIL AND Battery, the savings stack: Premium (6%) + SAIL (0.5%) + Battery (0.5%) = ~7% total reduction.

### 💡 Tips for Using the Calculator

- **Accurate Inputs = Accurate Results**: The better your input data (especially engine utilization %), the more accurate your savings estimate.
  
- **Conservative Estimates**: iEMS efficiency factors are based on real-world performance data, not theoretical maximums.

- **Fuel Price Impact**: The payback period is very sensitive to fuel prices. Higher fuel prices = faster payback.

- **Annual Hours**: More operating hours = more savings. Vessels with high utilization see better ROI.

### 🔄 How to Experiment

Try changing these inputs to see their impact:
- **Increase ME Utilization**: Higher load = better fuel efficiency (up to ~75%)
- **Change iEMS Variant**: Compare Advanced vs Pro vs Premium
- **Add SAIL or Battery**: See the additional savings
- **Adjust Fuel Price**: See how fuel market changes affect payback
- **Change Operating Hours**: See savings for different vessel schedules

---

## Need More Detail?

### Behind the Scenes: SFOC Tables

The calculator uses industry-standard Specific Fuel Oil Consumption (SFOC) curves:

**Main Engine SFOC** (grams per kWh):
- 25% load → 176.14 g/kWh
- 50% load → 172.28 g/kWh
- 75% load → 168.85 g/kWh (sweet spot!)
- 90% load → 167.69 g/kWh
- 100% load → 169.04 g/kWh

**Why does SFOC change?** Engines are most efficient at 70-80% load. Below 50% they waste fuel, and at 100% they work too hard.

**Auxiliary Engine SFOC** (less efficient):
- 25% load → 220.36 g/kWh
- 50% load → 202.81 g/kWh
- 75% load → 196.22 g/kWh
- 100% load → 195.91 g/kWh

---

*Document Version: 1.0*  
*Last Updated: November 20, 2025*  
*Based on: index.html iEMS Savings Calculator*
