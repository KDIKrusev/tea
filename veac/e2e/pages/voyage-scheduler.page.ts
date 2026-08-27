import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class VoyageSchedulerPage extends BasePage {
  // Vessel Selector
  readonly vesselDropdown: Locator;
  readonly vesselLoadingIndicator: Locator;
  readonly vesselErrorMessage: Locator;

  // Route Selector
  readonly routeDropdown: Locator;
  readonly routeLoadingIndicator: Locator;
  readonly routeErrorMessage: Locator;

  // Time Window Mode
  readonly timeWindowETDButton: Locator;
  readonly timeWindowETAButton: Locator;

  // ETD Date/Time
  readonly etdDateInput: Locator;
  readonly etdTimeInput: Locator;
  readonly etdDateText: Locator;
  readonly etdTimeText: Locator;

  // ETA Date/Time
  readonly etaDateInput: Locator;
  readonly etaTimeInput: Locator;
  readonly etaDateText: Locator;
  readonly etaTimeText: Locator;

  // Speed Range
  readonly speedMinLabel: Locator;
  readonly speedMaxLabel: Locator;
  readonly speedSlider: Locator;

  // Validation
  readonly validationErrors: Locator;
  readonly validationErrorMessage: Locator;

  // Action Buttons
  readonly clearButton: Locator;
  readonly searchButton: Locator;

  // Map
  readonly routeMap: Locator;

  constructor(page: Page) {
    super(page);

    // Vessel Selector
    this.vesselDropdown = page.locator('app-vessel-selector select.select-field');
    this.vesselLoadingIndicator = page.locator('app-vessel-selector .ea-vec-loading-text');
    this.vesselErrorMessage = page.locator('app-vessel-selector .ea-vec-error-message');

    // Route Selector
    this.routeDropdown = page.locator('app-route-selector select.select-field');
    this.routeLoadingIndicator = page.locator('app-route-selector .ea-vec-loading-text');
    this.routeErrorMessage = page.locator('app-route-selector .ea-vec-error-message');

    // Time Window Mode toggle buttons
    this.timeWindowETDButton = page.locator('.toggle-option:has-text("Departure (ETD)")');
    this.timeWindowETAButton = page.locator('.toggle-option:has-text("Arrival (ETA)")');

    // ETD Date/Time inputs
    this.etdDateInput = page.locator('app-date-time[type="etd"] .dateTime-input').first();
    this.etdTimeInput = page.locator('app-date-time[type="etd"] .dateTime-input.time-input');
    this.etdDateText = page.locator('app-date-time[type="etd"] .dateTime-field:first-child .dateTime-text');
    this.etdTimeText = page.locator('app-date-time[type="etd"] .dateTime-field:last-child .dateTime-text');

    // ETA Date/Time inputs
    this.etaDateInput = page.locator('app-date-time[type="eta"] .dateTime-input').first();
    this.etaTimeInput = page.locator('app-date-time[type="eta"] .dateTime-input.time-input');
    this.etaDateText = page.locator('app-date-time[type="eta"] .dateTime-field:first-child .dateTime-text');
    this.etaTimeText = page.locator('app-date-time[type="eta"] .dateTime-field:last-child .dateTime-text');

    // Speed Range
    this.speedMinLabel = page.locator('.ea-vec-speed-label').first();
    this.speedMaxLabel = page.locator('.ea-vec-speed-label').last();
    this.speedSlider = page.locator('ngx-slider');

    // Validation
    this.validationErrors = page.locator('.validation-errors');
    this.validationErrorMessage = page.locator('.validation-error');

    // Action Buttons
    this.clearButton = page.locator('button.btn-clear');
    this.searchButton = page.locator('button.btn-search');

    // Map
    this.routeMap = page.locator('app-voyage-map');
  }

  async navigate() {
    await this.goto('/vec');
    await this.page.waitForLoadState('networkidle');
  }

  async selectVessel(vesselName: string) {
    const options = await this.vesselDropdown.locator('option').all();
    for (let i = 0; i < options.length; i++) {
      const text = await options[i].textContent();
      if (text?.trim() === vesselName.trim()) {
        await this.vesselDropdown.selectOption({ index: i });
        await this.page.waitForTimeout(500);
        return;
      }
    }
    throw new Error(`Vessel "${vesselName}" not found in dropdown`);
  }

  async selectRoute(routeName: string) {
    const options = await this.routeDropdown.locator('option').all();
    for (let i = 0; i < options.length; i++) {
      const text = await options[i].textContent();
      if (text?.trim() === routeName.trim()) {
        await this.routeDropdown.selectOption({ index: i });
        await this.page.waitForTimeout(500);
        return;
      }
    }
    throw new Error(`Route "${routeName}" not found in dropdown`);
  }

  async getSelectedVessel(): Promise<string> {
    const selectedOption = this.vesselDropdown.locator('option:checked');
    const text = await selectedOption.textContent();
    return text?.trim() || '';
  }

  async getSelectedRoute(): Promise<string> {
    const selectedOption = this.routeDropdown.locator('option:checked');
    const text = await selectedOption.textContent();
    return text?.trim() || '';
  }

  async getAvailableVessels(): Promise<string[]> {
    const options = await this.vesselDropdown.locator('option').allTextContents();
    return options.map(opt => opt.trim());
  }

  async getAvailableRoutes(): Promise<string[]> {
    const options = await this.routeDropdown.locator('option').allTextContents();
    return options.map(opt => opt.trim());
  }

  async switchToETDMode() {
    await this.click(this.timeWindowETDButton);
    await this.page.waitForTimeout(300);
  }

  async switchToETAMode() {
    await this.click(this.timeWindowETAButton);
    await this.page.waitForTimeout(300);
  }

  async openETDDatePicker() {
    await this.click(this.etdDateInput);
    await this.page.waitForTimeout(500);
  }

  async openETDTimePicker() {
    await this.click(this.etdTimeInput);
    await this.page.waitForTimeout(500);
  }

  async openETADatePicker() {
    await this.click(this.etaDateInput);
    await this.page.waitForTimeout(500);
  }

  async openETATimePicker() {
    await this.click(this.etaTimeInput);
    await this.page.waitForTimeout(500);
  }

  async getETDDateText(): Promise<string> {
    return await this.getText(this.etdDateText);
  }

  async getETDTimeText(): Promise<string> {
    return await this.getText(this.etdTimeText);
  }

  async getETADateText(): Promise<string> {
    return await this.getText(this.etaDateText);
  }

  async getETATimeText(): Promise<string> {
    return await this.getText(this.etaTimeText);
  }

  async getMinSpeed(): Promise<string> {
    const text = await this.getText(this.speedMinLabel);
    return text.replace(' kn', '').trim();
  }

  async getMaxSpeed(): Promise<string> {
    const text = await this.getText(this.speedMaxLabel);
    return text.replace('kn', '').trim();
  }

  async clickClear() {
    await this.click(this.clearButton);
    await this.page.waitForTimeout(500);
  }

  async clickSearch() {
    await this.click(this.searchButton);
  }

  async isSearchButtonEnabled(): Promise<boolean> {
    return await this.searchButton.isEnabled();
  }

  async isSearchButtonDisabled(): Promise<boolean> {
    return await this.searchButton.isDisabled();
  }

  async getSearchButtonText(): Promise<string> {
    return await this.getText(this.searchButton);
  }

  async hasValidationErrors(): Promise<boolean> {
    return await this.isVisible(this.validationErrors);
  }

  async getValidationErrors(): Promise<string[]> {
    if (!await this.hasValidationErrors()) {
      return [];
    }
    return await this.validationErrorMessage.allTextContents();
  }

  async isVesselLoading(): Promise<boolean> {
    return await this.isVisible(this.vesselLoadingIndicator);
  }

  async isRouteLoading(): Promise<boolean> {
    return await this.isVisible(this.routeLoadingIndicator);
  }

  async isRouteMapVisible(): Promise<boolean> {
    return await this.isVisible(this.routeMap);
  }

  async selectETDDateToday() {
    await this.etdDateInput.click();
    await this.page.waitForTimeout(1000);

    // Wait for modal/calendar to appear
    const calendarVisible = await this.page.locator('.calendar-picker').isVisible().catch(() => false);

    if (!calendarVisible) {
      // Try clicking again
      await this.etdDateInput.click();
      await this.page.waitForTimeout(1000);
    }

    const todayButton = this.page.locator('.nav-btn-today');
    const todayVisible = await todayButton.isVisible().catch(() => false);
    
    if (todayVisible) {
      await todayButton.click();
      await this.page.waitForTimeout(500);
    } else {
      // Try selecting today's date from calendar grid
      const todayCell = this.page.locator('.calendar-day-today');
      if (await todayCell.isVisible().catch(() => false)) {
        await todayCell.click();
        await this.page.waitForTimeout(500);
      }
    }

    // Apply selection
    const applyButton = this.page.locator('.btn-apply');
    const applyVisible = await applyButton.isVisible().catch(() => false);
    
    if (applyVisible) {
      await applyButton.click();
      await this.page.waitForTimeout(500);
    } else {
      const okButton = this.page.locator('button:has-text("Ok")');
      if (await okButton.isVisible().catch(() => false)) {
        await okButton.click();
        await this.page.waitForTimeout(500);
      }
    }
    
  }

  async selectETDDate(daysFromToday: number = 0) {
    await this.etdDateInput.click();
    await this.page.waitForTimeout(500);

    if (daysFromToday === 0) {
      // Select today
      const todayButton = this.page.locator('.nav-btn-today');
      await todayButton.click();
    } else {
      // Select specific day (simplified - just click first available date)
      const firstDay = this.page.locator('.calendar-day:not(.calendar-day-other)').first();
      await firstDay.click();
    }

    await this.page.waitForTimeout(300);

    // Apply
    const applyButton = this.page.locator('.btn-apply');
    await applyButton.click();
    await this.page.waitForTimeout(500);
  }

  async hasETDDate(): Promise<boolean> {
    const dateText = await this.etdDateText.textContent();
    return dateText !== null && dateText.trim() !== 'Select departure date' && dateText.trim().length > 0;
  }

  async fillETDTime(time: string = '12:00') {
    await this.etdTimeInput.click();
    await this.page.waitForTimeout(1000);

    // Wait for time picker modal
    const timePicker = this.page.locator('.time-picker');
    const timePickerVisible = await timePicker.waitFor({ state: 'visible', timeout: 5000 }).then(() => true).catch(() => false);

    if (!timePickerVisible) {
      await this.etdTimeInput.click();
      await this.page.waitForTimeout(1000);
      
      const retryVisible = await timePicker.isVisible().catch(() => false);
      
      if (!retryVisible) {
        return;
      }
    }

    if (time === 'any') {
      // Already selected by default
    } else {
      const timeOption = this.page.locator('.time-option').filter({ hasText: time }).first();
      const specificTimeVisible = await timeOption.isVisible().catch(() => false);
      
      if (specificTimeVisible) {
        await timeOption.click();
      } else {
        // Try to find any time that's not "Any time"
        const allTimeOptions = await this.page.locator('.time-option').all();
        
        if (allTimeOptions.length > 1) {
          // Click the second option (first real time, skip "Any time")
          await allTimeOptions[1].click();
          const selectedTime = await allTimeOptions[1].textContent();
        }
      }
    }

    await this.page.waitForTimeout(500);

    // Click "Ok" to apply
    const okButton = this.page.locator('.time-picker .btn-apply');
    const okVisible = await okButton.isVisible().catch(() => false);
    
    if (okVisible) {
      await okButton.click();
      await this.page.waitForTimeout(500);
    } else {
      const altOkButton = this.page.locator('button:has-text("Ok")').last();
      if (await altOkButton.isVisible().catch(() => false)) {
        await altOkButton.click();
        await this.page.waitForTimeout(500);
      }
    }
    
  }

  async hasETDTime(): Promise<boolean> {
    const timeText = await this.etdTimeText.textContent();
    return timeText !== null && timeText.trim().length > 0 && timeText.trim() !== '--:--';
  }

  async waitForVesselDropdownLoaded() {
    await this.page.locator('app-vessel-selector .ea-vec-loading').waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
    await this.vesselDropdown.waitFor({ state: 'visible', timeout: 5000 });
  }

  async waitForRouteDropdownLoaded() {
    await this.page.locator('app-route-selector .ea-vec-loading').waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
    await this.routeDropdown.waitFor({ state: 'visible', timeout: 5000 });
  }
}
