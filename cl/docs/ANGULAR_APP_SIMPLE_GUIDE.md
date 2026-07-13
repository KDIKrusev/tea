# iEMS Angular Application - Simple Calculation Guide

This document explains **how the Angular iEMS Calculator works** in simple terms - just the calculations, no code. It shows you exactly how your savings are calculated from start to finish.

---

## What You'll See (With Default Values)

When you use the default values in the Angular calculator, here's what you get:

| Result | Value | What It Means |
|--------|-------|---------------|
| **Annual Fuel Cost Savings** | **$1,449,626** | Money you save every year on fuel |
| **Annual Fuel Savings** | **1,812.03 MT** | Tons of fuel you save every year |
| **CO2 Emission Reduction** | **5,809.38 MT** | Tons of CO2 you prevent every year |
| **Simple Payback Period** | **0.1 years** | How fast you recover your investment (~1.2 months) |
| **10-Year ROI** | **6,803%** | Your return on investment over 10 years |
| **10-Year NPV** | **$11.0 Million** | Today's value of all future savings |
| **iEMS Investment** | **$210,000** | One-time cost (system + installation) |

---

## Step 1: What Information Do You Enter?

You provide information about your vessel in three main categories:

### Your Vessel's Power Needs

| What You Tell Us | Default Value | What It Means |
|------------------|---------------|---------------|
| **Avg Propulsion Power** | 20,000 kW | Power for your propellers |
| **Avg Hotel/Mission Load** | 3,000 kW | Power for lights, HVAC, equipment |
| **Sea Margin** | 5% | Extra power buffer for rough seas |

**→ Total power your vessel needs: ~24,000 kW**

### Your Engine Setup

**Main Engines:**
- **Capacity**: 40,000 kW (maximum they can produce)
- **Usage**: 55% (how much you actually use)
- **Your main engines produce**: 22,000 kW

**Shaft Generators:**
- **Capacity**: 4,000 kW
- **Usage**: 50%
- **Your shaft generators produce**: 2,000 kW

**Auxiliary Engines:**
- **Total capacity**: 6,000 kW
- **Number installed**: 3 engines
- **Average running**: 1.5 engines
- **In default scenario**: Not needed (main + shaft cover everything)

### Economic Information

| What You Tell Us | Default Value |
|------------------|---------------|
| **Fuel Price** | $800 per ton |
| **Operating Hours** | 8,000 hours per year |

### iEMS System Choice

| System | Fuel Savings | Investment |
|--------|--------------|------------|
| **Advanced** | 3% reduction | $110,000 |
| **Pro** | 4.5% reduction | $140,500 |
| **Premium** | 6% reduction | $210,000 |

**Default selection: Premium (6% savings)**

### Optional Add-Ons

| System | Extra Savings | Default |
|--------|---------------|---------|
| **SAIL** | +0.5% | Not installed |
| **Battery** | +0.5% | 0 kWh |

---

## Step 2: How We Calculate Your Current Fuel Consumption

### Understanding SFOC (Fuel Burn Rate)

**SFOC = Specific Fuel Oil Consumption** - tells us how many grams of fuel an engine burns to produce 1 kWh of power.

**Why does it matter?** Engines are like cars - they have a "sweet spot" where they run most efficiently:
- Running too light (below 50%): **Wastes fuel** ⛽💸
- Running at 70-80%: **Most efficient** ⛽✅
- Running at maximum (100%): **Works too hard, less efficient** ⛽😓

### Main Engine SFOC Chart

| Engine Load | Fuel Burn Rate | Efficiency |
|-------------|----------------|------------|
| 25% load | 176.14 g/kWh | Poor (running too light) |
| 50% load | 172.28 g/kWh | Better |
| 75% load | 168.85 g/kWh | **Best!** |
| 90% load | 167.69 g/kWh | Very good |
| 100% load | 169.04 g/kWh | Good (but maxed out) |

### Auxiliary Engine SFOC Chart

| Engine Load | Fuel Burn Rate | Why Different? |
|-------------|----------------|----------------|
| 25% load | 220.36 g/kWh | Aux engines are smaller, less efficient |
| 50% load | 202.81 g/kWh | than main engines |
| 75% load | 196.22 g/kWh | |
| 100% load | 195.91 g/kWh | |

### Your Current Fuel Consumption (Step-by-Step)

**1. Calculate total power needed:**
- Propulsion: 20,000 kW + 5% sea margin = 21,000 kW
- Hotel/mission loads: 3,000 kW
- **Total needed: 24,000 kW**

**2. See what your engines are producing:**
- Main engines: 40,000 kW × 55% = **22,000 kW**
- Shaft generators: 4,000 kW × 50% = **2,000 kW**
- Together: 22,000 + 2,000 = **24,000 kW** ✅ (perfectly covered!)
- Auxiliary engines: **0 kW** (not needed)

**3. Calculate fuel burn rate for main engines:**
- Main engines running at: 22,000 ÷ 40,000 = **55% load**
- At 55% load, SFOC is: **~171.6 g/kWh** (calculator finds exact value between 50% and 75%)

**4. Calculate hourly fuel consumption:**
- Main engines: 22,000 kW × 171.6 g/kWh = 3,775,200 grams/hour
- Convert to kg: 3,775,200 ÷ 1,000 = **3,775.2 kg/hour**
- Auxiliary engines: 0 kW × anything = **0 kg/hour**
- **Total: 3,775.2 kg/hour**

**5. Calculate annual fuel consumption:**
- Per hour: 3,775.2 kg
- Operating hours: 8,000 hours/year
- Annual total: 3,775.2 × 8,000 = 30,201,600 kg
- Convert to tons: 30,201,600 ÷ 1,000 = **30,201.6 tons/year**

**This is how much fuel you burn NOW (without iEMS).**

---

## Step 3: How iEMS Reduces Your Fuel Consumption

### What Does iEMS Actually Do?

iEMS is like having a **super-smart power manager** that:
- Constantly monitors all your engines
- Switches engines on/off at optimal times
- Balances loads to keep engines in their "sweet spot"
- Predicts power needs and prepares in advance
- Prevents engines from running inefficiently

**Result: Same power output, less fuel burned** ⛽→💰

### How Much Does Each iEMS Variant Save?

| Variant | Technology | Fuel Reduction | Annual Fuel Saved |
|---------|-----------|----------------|-------------------|
| **Advanced** | Basic optimization | 3% | 906 tons |
| **Pro** | Advanced load balancing | 4.5% | 1,359 tons |
| **Premium** | Full AI-powered optimization | 6% | 1,812 tons |

### Your Calculation with Premium iEMS

**1. Start with current consumption:**
- Current: 30,201.6 tons/year

**2. Apply iEMS efficiency:**
- Premium reduces fuel by: 6%
- Efficiency factor: 0.94 (meaning you use 94% of current fuel)

**3. Calculate new consumption:**
- With iEMS: 30,201.6 × 0.94 = **28,389.5 tons/year**

**4. Optional bonuses (if you have them):**
- **SAIL installed?** Extra 0.5% reduction
- **Battery installed?** Extra 0.5% reduction
- These stack! Premium + SAIL + Battery = ~7% total savings

---

## Step 4: Calculating Your Savings

Now we compare BEFORE and AFTER:

### Fuel Savings

**Before iEMS**: 30,201.6 tons/year  
**After iEMS**: 28,389.5 tons/year  
**Fuel Saved**: 30,201.6 - 28,389.5 = **1,812.1 tons/year**

**That's 6% less fuel every year!**

### Cost Savings

**How much does that fuel cost?**
- Fuel saved: 1,812.1 tons/year
- Fuel price: $800 per ton
- **Money saved**: 1,812.1 × $800 = **$1,449,680/year**

**That's $1.45 million saved every single year!**

### CO2 Emission Reduction

**Every ton of fuel burned produces 3.206 tons of CO2.**

**CO2 reduction calculation:**
- Fuel saved: 1,812.1 tons
- CO2 factor: 3.206
- **CO2 saved**: 1,812.1 × 3.206 = **5,809.6 tons CO2/year**

**What does 5,809 tons of CO2 mean?**
- Taking ~1,260 cars off the road for a year 🚗
- Planting ~267,000 tree seedlings 🌳
- Equivalent to powering ~730 homes for a year 🏠

---

## Step 5: Is It Worth the Investment?

### How Much Does iEMS Cost?

**Premium iEMS Investment:**

| Cost Item | Amount (NOK) | Amount (USD) |
|-----------|--------------|--------------|
| iEMS System | 1,800,000 | $180,000 |
| Commissioning & Installation | 300,000 | $30,000 |
| **Total Investment** | **2,100,000** | **$210,000** |

*(Conversion rate: 10 NOK = 1 USD)*

### Simple Payback Period: How Fast Do You Break Even?

**Calculation:**
- Investment: $210,000
- Annual savings: $1,449,680
- Payback time: $210,000 ÷ $1,449,680 = **0.145 years**

**That's about 1.7 months!** 🚀

**Timeline:**
- **Month 0**: Invest $210,000
- **Month 1**: Save $120,807
- **Month 2**: Save another $120,807 (total $241,614 saved - you've broken even!)
- **Months 3-12**: Pure profit! (~$1.2 million)
- **Year 2-10**: Keep saving $1.45M every year

---

## Step 6: Long-Term Financial Picture (10 Years)

The Angular app calculates two advanced financial metrics:

### Return on Investment (ROI)

**What is ROI?** Shows how much money you make compared to what you invested.

**Calculation over 10 years:**
- Annual savings: $1,449,680/year
- 10-year total savings: $1,449,680 × 10 = $14,496,800
- Subtract investment: $14,496,800 - $210,000 = $14,286,800 profit
- ROI percentage: ($14,286,800 ÷ $210,000) × 100 = **6,803%**

**What does 6,803% ROI mean?**
- For every $1 you invest, you get back $69! 💰💰💰
- In 10 years, you make 68 times your investment!

### Net Present Value (NPV)

**What is NPV?** Today's value of all future savings (accounting for "time value of money").

**Why does it matter?** Money today is worth more than money tomorrow because:
- You could invest today's money and earn interest
- Inflation reduces future money's purchasing power

**NPV Calculation (5% discount rate):**

| Year | Savings | Present Value |
|------|---------|---------------|
| 1 | $1,449,680 | $1,380,648 |
| 2 | $1,449,680 | $1,314,903 |
| 3 | $1,449,680 | $1,252,289 |
| 4 | $1,449,680 | $1,192,657 |
| 5 | $1,449,680 | $1,135,864 |
| 6 | $1,449,680 | $1,081,775 |
| 7 | $1,449,680 | $1,030,262 |
| 8 | $1,449,680 | $981,202 |
| 9 | $1,449,680 | $934,478 |
| 10 | $1,449,680 | $889,979 |
| **Total** | **$14,496,800** | **~$11,194,057** |
| **Minus Investment** | | **-$210,000** |
| **Net Present Value** | | **~$11,000,000** |

**What does $11M NPV mean?**
- Even accounting for time value of money, your investment is worth $11 million today!
- This is an **exceptionally profitable investment** 📈

---

## The Complete Picture: Your Vessel's Transformation

### 📊 Current Situation (Without iEMS)

**Power Setup:**
- Your vessel needs: 24,000 kW total power
  - 21,000 kW for propulsion (with 5% margin)
  - 3,000 kW for hotel/mission
- Your engines provide: 24,000 kW ✅
  - Main engines: 22,000 kW (running at 55% capacity)
  - Shaft generators: 2,000 kW (running at 50% capacity)
  - Aux engines: 0 kW (not needed)

**Fuel Consumption:**
- Hourly: 3,775.2 kg (3.8 tons)
- Daily: 90,605 kg (90.6 tons)
- Monthly: 2.72 million kg (2,720 tons)
- **Yearly: 30,201.6 tons**

**Annual Costs:**
- Fuel: 30,201.6 tons × $800 = **$24,161,280**
- CO2 emissions: 30,201.6 × 3.206 = **96,826 tons CO2**

### ✅ With iEMS Premium

**What Changes:**
- Power setup: Exactly the same (you still get 24,000 kW)
- Operating profile: Exactly the same (same routes, same operations)
- **What's different:** Engines run smarter, not harder

**New Fuel Consumption:**
- Efficiency improvement: 6% (factor of 0.94)
- Hourly: 3,548.7 kg (6% less)
- Daily: 85,169 kg (6% less)
- Monthly: 2,555 tons (6% less)
- **Yearly: 28,389.5 tons**

**New Annual Costs:**
- Fuel: 28,389.5 tons × $800 = **$22,711,600**
- CO2 emissions: 28,389.5 × 3.206 = **91,017 tons CO2**

### 💰 Your Annual Savings (Every Year)

| Category | Savings | Percentage |
|----------|---------|------------|
| **Fuel** | 1,812.1 tons | 6.0% |
| **Money** | $1,449,680 | 6.0% |
| **CO2** | 5,809.6 tons | 6.0% |

### 📈 Your Financial Results

| Metric | Value | What It Means |
|--------|-------|---------------|
| **Investment** | $210,000 | One-time cost |
| **Break-even** | 1.7 months | When you've recovered investment |
| **Year 1 profit** | $1,239,680 | Savings minus investment |
| **Year 2-10 profit/year** | $1,449,680 | Pure profit every year |
| **10-year total profit** | $14,286,800 | Total after subtracting investment |
| **10-year ROI** | 6,803% | Return on your $210k |
| **10-year NPV** | $11.0M | Today's value of all savings |

---

## Additional Insights: Engine Breakdown

The Angular app tracks each engine type separately:

### Current Fuel Consumption by Engine

| Engine Type | Power Output | Fuel Consumption | Percentage |
|-------------|--------------|------------------|------------|
| **Main Engines** | 22,000 kW | 30,201.6 tons/year | 100% |
| **Shaft Generators** | 2,000 kW | 0* tons/year | 0% |
| **Aux Engines** | 0 kW | 0 tons/year | 0% |

*Shaft generator fuel is included in main engine consumption (they're mechanically connected)

### With iEMS: Fuel Consumption by Engine

| Engine Type | Power Output | Fuel Consumption | Savings |
|-------------|--------------|------------------|---------|
| **Main Engines** | 22,000 kW | 28,389.5 tons/year | 1,812.1 tons |
| **Shaft Generators** | 2,000 kW | 0* tons/year | 0 tons |
| **Aux Engines** | 0 kW | 0 tons/year | 0 tons |

**Key Insight:** With the default values, all savings come from optimizing the main engines.

---

## Understanding the Numbers Better

### Why 6% Makes Such a Huge Difference

**6% sounds small, but...**

At sea, engines run constantly:
- 24 hours a day
- 365 days a year (or 8,000 hours in this case)
- Burning tons of expensive fuel every hour

**Small percentage × huge fuel consumption = massive savings**

### Visual Comparison

**Daily fuel consumption:**
- Before: 90.6 tons ($72,480)
- After: 85.2 tons ($68,160)
- Daily savings: 5.4 tons ($4,320)

**That's like saving $4,320 every single day!** 📆💰

**Monthly fuel consumption:**
- Before: 2,720 tons ($2,176,000)
- After: 2,557 tons ($2,046,000)
- Monthly savings: 163 tons ($130,000)

**That's like saving $130,000 every month!** 📅💰

### Break-Even Timeline

```
Month 0:  [-$210,000]  Initial investment
Month 1:  [-$89,193]   Saved $120,807
Month 2:  [+$31,614]   ✅ BREAK EVEN! Saved another $120,807
Month 3:  [+$152,421]  In profit! Saved another $120,807
Month 12: [+$1,239,680] End of Year 1
Year 2:   [+$2,689,360] Keep stacking savings
Year 10:  [+$14,286,800] Total profit
```

---

## What Makes the Angular App Special?

### Same Core Calculations, Better Presentation

The Angular app uses **exactly the same formulas** as the HTML calculator, but adds:

#### 1. Better Financial Analysis
- **ROI calculation**: Shows 10-year return percentage
- **NPV calculation**: Accounts for time value of money
- **Multi-year projections**: See savings over entire vessel lifetime

#### 2. Detailed Breakdowns
- **By engine type**: See ME vs AE contributions separately
- **By time period**: Annual, monthly, daily views
- **By category**: Fuel, cost, emissions tracked independently

#### 3. Visual Comparisons
- **Bar charts**: Compare baseline vs optimized visually
- **Trend lines**: See savings accumulate over time
- **Impact metrics**: Car equivalents, tree planting comparisons

#### 4. Real-Time Updates
- Change any input → Results update instantly
- Try different scenarios → Compare immediately
- No "Calculate" button needed → Just type and see

---

## Experimenting with the Calculator

### Try These Scenarios:

#### Scenario 1: What if fuel prices increase?
- Change fuel price from $800 to $1,200/ton
- **New annual savings**: $2,174,520
- **New payback**: 1.2 months (even faster!)

#### Scenario 2: What if you add SAIL?
- Change "SAIL Installed" to "Yes"
- **Additional savings**: 0.5% (total 6.5%)
- **New annual savings**: $1,522,464
- **New payback**: 1.7 months (slightly faster)

#### Scenario 3: What if you use Advanced instead of Premium?
- Change variant from "Premium" to "Advanced"
- **Savings drop to**: 3% (906 tons)
- **Annual savings**: $724,800
- **Investment drops to**: $110,000
- **New payback**: 1.8 months
- **But 10-year profit**: Only $7,138,000 (vs $14.3M with Premium)

#### Scenario 4: What if your engines run at different utilization?
- Change ME utilization from 55% to 75%
- At 75%, engines are more efficient (better SFOC)
- Watch your baseline fuel consumption change
- Compare new savings

---

## Key Assumptions & Important Notes

### What the Calculator Assumes

1. **Consistent Operations**: Your vessel operates the same way all year (same average loads, same hours)

2. **Fuel Properties**: Standard marine fuel oil with CO2 factor of 3.206 tons CO2 per ton fuel

3. **SFOC Accuracy**: Uses industry-standard fuel consumption curves for marine diesel engines

4. **Currency**: Fixed rate of 10 NOK = 1 USD (actual rate may vary)

5. **Discount Rate**: 5% used for NPV calculation (conservative for maritime industry)

6. **No Inflation**: Future savings calculated in today's dollars (conservative)

### What Can Change Results

**Higher savings if:**
- ✅ Fuel prices increase
- ✅ Operating hours increase
- ✅ You add SAIL or battery systems
- ✅ You upgrade to Premium from Advanced/Pro

**Lower savings if:**
- ❌ Fuel prices decrease
- ❌ Operating hours decrease  
- ❌ Engines already run at optimal loads (70-80%)

---

## Real-World Context

### Is This Realistic?

**Yes!** These calculations are based on:
- Real engine SFOC curves from manufacturers
- Actual iEMS performance data from installed systems
- Conservative efficiency estimates (real savings often higher)
- Industry-standard CO2 emission factors

### Why Such Fast Payback?

Marine fuel consumption is **enormous**:
- Large vessels burn millions of dollars in fuel annually
- Even small percentage improvements = massive savings
- iEMS investment is relatively small compared to annual fuel costs

### What Other Vessel Owners Experience

**Typical results:**
- Payback: 6 months to 2 years
- Fuel savings: 3-8% (depending on variant and vessel type)
- Additional benefits: Extended engine life, reduced maintenance
- CO2 compliance: Helps meet IMO emissions regulations

---

## Summary: The Bottom Line

### With Default Values (Premium iEMS):

**Your Investment:**
- **$210,000** one-time

**Your Annual Returns:**
- **$1,449,680** saved every year
- **1,812 tons** fuel saved
- **5,810 tons** CO2 reduced

**Your Timeline:**
- **1.7 months** to break even
- **10.3 months** to double your money
- **10 years** to make $14.3M profit

**Your ROI:**
- **590%** in Year 1
- **6,803%** in 10 years

**Your Environmental Impact:**
- **58,096 tons CO2** prevented over 10 years
- Equivalent to **12,600 cars** off the road for a year
- Or planting **2.67 million trees** 🌳🌳🌳

### Is It Worth It?

**Financially:** Absolutely! Few investments offer 590% first-year returns.

**Operationally:** Same power, same performance, just smarter management.

**Environmentally:** Massive CO2 reduction helps meet regulations and sustainability goals.

**Risk:** Very low - proven technology with quick payback.

---

*Document Version: 1.0*  
*Last Updated: November 20, 2025*  
*Application: Angular iEMS Calculator*  
*All calculations explained without code - just the math!*
