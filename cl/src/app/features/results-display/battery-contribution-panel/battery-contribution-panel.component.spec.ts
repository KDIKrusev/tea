import { TestBed } from '@angular/core/testing';

import { BatteryContributionPanelComponent } from './battery-contribution-panel.component';
import { BatteryDetails } from '../../../calculations/calculator.types';

/**
 * DOM specs for the allocation-table labels (story Cosmetics A).
 *
 * The fixture reproduces the shape the client actually questioned (test23, DP mode): a DpReserve
 * row covered 800 kW while the peak-shaving total reads 0. The labels — not the numbers — are what
 * this story changed, so the specs pin the labels next to the numbers that made them confusing.
 */
describe('BatteryContributionPanelComponent', () => {
  const dpReserveDetails: BatteryDetails = {
    capacityKwh: 800,
    powerKw: 800,
    spinningReserveKw: 320,
    peakShavingKw: 0,
    benefitFocTonPerYear: 379.9,
    benefitCostPerYear: 296318,
    modeAllocations: [
      {
        mode: 'DP',
        loads: [
          {
            load: 'DpReserve', function: 'Reserve', coverageFactor: 1,
            averageLoadKw: 800, variationKw: 800, batteryUsedKw: 800,
            coveredBandKw: 800, uncoveredReserveKw: 0
          },
          {
            load: 'Hotel', function: 'PeakShaving', coverageFactor: 0.05,
            averageLoadKw: 3500, variationKw: 70, batteryUsedKw: 0,
            coveredBandKw: 0, uncoveredReserveKw: 70
          }
        ],
        peakShavingBandKw: 0,
        additionalSpinningReserveKw: 70,
        committedBatteryKw: 800,
        remainingBatteryKw: 0
      }
    ]
  };

  function render(details: BatteryDetails): HTMLElement {
    const fixture = TestBed.createComponent(BatteryContributionPanelComponent);
    fixture.componentRef.setInput('batteryDetails', details);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function headerTexts(root: HTMLElement): string[] {
    return Array.from(root.querySelectorAll('.battery-alloc-table thead th'))
      .map(th => th.textContent?.trim() ?? '');
  }

  it('marks both band columns with ±, in the same style', () => {
    const headers = headerTexts(render(dpReserveDetails));

    expect(headers).withContext('Variation carries the ± like Covered (client request 3)')
      .toContain('Variation ± (kW)');
    expect(headers).withContext('Covered keeps its existing label')
      .toContain('Covered ± (kW)');
  });

  it('says the Covered total sums peak-shaving rows only, next to the number that confused', () => {
    const root = render(dpReserveDetails);
    const footerCells = Array.from(root.querySelectorAll('.battery-alloc-table tfoot td'));

    expect(footerCells[0]?.textContent).withContext('the totals label carries the scope')
      .toContain('peak-shaving rows only');

    // The exact test23 shape: 800 covered in the reserve row, 0 in the peak-shaving total.
    const coveredColumn = 4; // Load | Function | Variation ± | Battery Used | Covered ± | Uncovered
    const reserveRow = root.querySelectorAll('.battery-alloc-table tbody tr')[0];
    expect(reserveRow?.children[coveredColumn]?.textContent).toContain('800');
    expect(footerCells[2]?.textContent?.trim()).toBe('0');
    expect(footerCells[2]?.getAttribute('title'))
      .withContext('the tooltip explains why the reserve row is excluded')
      .toContain('readiness, not peak shaving');
  });

  it('explains the exclusion in the panel footnote', () => {
    const note = render(dpReserveDetails).querySelector('.battery-note');
    expect(note?.textContent).toContain('Covered total sums peak-shaving rows only');
  });
});
