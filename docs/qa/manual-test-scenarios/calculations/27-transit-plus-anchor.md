# 27 — Transit + Anchor

Anchor mode had never been exercised on its own — it appeared only inside scenarios 11 and 14,
bundled with three other modes, where its contribution cannot be isolated.

## What Anchor is, in the model

```
propulsion : 0        (the vessel is not moving)
hotel      : as entered
sea margin : not applied — only Transit adds it
```

Anchor and Port are the two zero-propulsion modes. The difference between them is only the hotel
figure the user enters; the code path is identical.

## The plant and the numbers

Scenario 03's vessel plus **1 500 h at anchor with a 600 kW hotel load**.

```
Transit  5 000 h   propulsion 12 036.2   SG 3 250.0   AE 626.0
Anchor   1 500 h   propulsion      0.0   SG   600.0   AE   0.0
```

Baseline **13 810.2 t/yr** · L1 savings **192.72 t/yr** · L2 and L3 zero (one aux engine).

## The SG-forced rule is visible here

At anchor the shaft generator carries the whole 600 kW hotel load — which means **a main engine is
running to spin it**, at roughly 2 % load, while four idle auxiliary engines sit unused.

That is logged observation #1: when a shaft generator is installed, `Level1CandidateBuilder` requires
every combination to run it, and an SG cannot turn without its main engine. Real vessels at anchor
run an auxiliary generator instead.

**The anchor hours therefore inflate the annual fuel figure**, and the more of them a vessel has the
worse the distortion. Scenario 27 exists partly to make that measurable: compare its baseline with
scenario 03's (13 652.6 → 13 810.2 for 1 500 anchor hours) and the cost of the rule is right there.

**Takeaway:** Anchor is a simple mode with a complicated side effect. Isolating it turns a known
observation into a number.
