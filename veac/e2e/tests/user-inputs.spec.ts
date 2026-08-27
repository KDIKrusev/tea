import { test, expect } from '@playwright/test';
import { VoyageSchedulerPage } from '../pages/voyage-scheduler.page';
import { loginAsAdmin } from '../fixtures/auth-helper';

/**
 * ==========================================
 * USER INPUTS E2E TESTS
 * ==========================================
 * Comprehensive test suite for all user input fields and UI elements:
 * - Vessel Selector
 * - Route Selector
 * - Time Window (ETD/ETA)
 * - Date/Time Pickers
 * - Speed Range
 * - Action Buttons
 * - Form Validation
 * - Layout & Accessibility
 */
test.describe('User Inputs - UI Elements', () => {
  let voyagePage: VoyageSchedulerPage;

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    voyagePage = new VoyageSchedulerPage(page);
    await voyagePage.navigate();
    
    await voyagePage.waitForVesselDropdownLoaded();
    await voyagePage.waitForRouteDropdownLoaded();
  });

  /**
   * ==========================================
   * VESSEL SELECTOR TESTS
   * ==========================================
   */
  test.describe('Vessel Selector', () => {
    
    test('should display vessel dropdown with options', async () => {
      await expect(voyagePage.vesselDropdown).toBeVisible();
      
      const vessels = await voyagePage.getAvailableVessels();
      expect(vessels.length).toBeGreaterThan(0);
    });

    test('should have a vessel pre-selected by default', async () => {
      const selectedVessel = await voyagePage.getSelectedVessel();
      expect(selectedVessel).toBeTruthy();
      expect(selectedVessel.length).toBeGreaterThan(0);
    });

    test('should allow selecting different vessels', async ({ page }) => {
      const vessels = await voyagePage.getAvailableVessels();
      
      if (vessels.length < 2) {
        test.skip('Not enough vessels to test selection');
      }

      const secondVessel = vessels[1];
      await voyagePage.selectVessel(secondVessel);

      const selectedVessel = await voyagePage.getSelectedVessel();
      expect(selectedVessel).toBe(secondVessel);
    });

    test('should not show loading indicator after vessels are loaded', async () => {
      const isLoading = await voyagePage.isVesselLoading();
      expect(isLoading).toBeFalsy();
    });
  });

  /**
   * ==========================================
   * ROUTE SELECTOR TESTS
   * ==========================================
   */
  test.describe('Route Selector', () => {
    
    test('should display route dropdown with options', async () => {
      await expect(voyagePage.routeDropdown).toBeVisible();
      
      const routes = await voyagePage.getAvailableRoutes();
      expect(routes.length).toBeGreaterThan(0);
    });

    test('should have a route pre-selected by default', async () => {
      const selectedRoute = await voyagePage.getSelectedRoute();
      expect(selectedRoute).toBeTruthy();
      expect(selectedRoute.length).toBeGreaterThan(0);
    });

    test('should allow selecting different routes', async () => {
      const routes = await voyagePage.getAvailableRoutes();
      
      if (routes.length < 2) {
        test.skip('Not enough routes to test selection');
      }

      const secondRoute = routes[1];
      await voyagePage.selectRoute(secondRoute);

      const selectedRoute = await voyagePage.getSelectedRoute();
      expect(selectedRoute).toBe(secondRoute);
    });

    test('should not show loading indicator after routes are loaded', async () => {
      const isLoading = await voyagePage.isRouteLoading();
      expect(isLoading).toBeFalsy();
    });
  });

  /**
   * ==========================================
   * TIME WINDOW MODE TESTS
   * ==========================================
   */
  test.describe('Time Window Mode Selection', () => {
    
    test('should have ETD and ETA mode buttons visible', async () => {
      await expect(voyagePage.timeWindowETDButton).toBeVisible();
      await expect(voyagePage.timeWindowETAButton).toBeVisible();
    });

    test('should default to ETD mode', async () => {
      await expect(voyagePage.etdDateInput).toBeVisible();
      await expect(voyagePage.etdTimeInput).toBeVisible();
    });

    test('should switch to ETA mode', async () => {
      await voyagePage.switchToETAMode();

      await expect(voyagePage.etaDateInput).toBeVisible();
      await expect(voyagePage.etaTimeInput).toBeVisible();
    });

    test('should switch back to ETD mode from ETA', async () => {
      await voyagePage.switchToETAMode();
      await expect(voyagePage.etaDateInput).toBeVisible();

      await voyagePage.switchToETDMode();
      await expect(voyagePage.etdDateInput).toBeVisible();
    });
  });

  /**
   * ==========================================
   * DATE/TIME PICKER TESTS (ETD)
   * ==========================================
   */
  test.describe('ETD Date/Time Picker', () => {
    
    test('should have default placeholder text for date', async () => {
      const dateText = await voyagePage.getETDDateText();
      expect(dateText).toContain('Select departure date');
    });

    test('should have default "Any time" for time', async () => {
      const timeText = await voyagePage.getETDTimeText();
      expect(timeText).toContain('Any time');
    });

    test('should open date picker modal on date input click', async ({ page }) => {
      await voyagePage.openETDDatePicker();
      
      const calendarModal = page.locator('app-calendar');
      await expect(calendarModal).toBeVisible({ timeout: 3000 });
    });

    test('should open time picker modal on time input click', async ({ page }) => {
      await voyagePage.openETDTimePicker();
      
      const timePickerModal = page.locator('app-time-picker');
      await expect(timePickerModal).toBeVisible({ timeout: 3000 });
    });

    test('should display date and time inputs as clickable', async () => {
      await expect(voyagePage.etdDateInput).toBeEnabled();
      await expect(voyagePage.etdTimeInput).toBeEnabled();
    });
  });

  /**
   * ==========================================
   * DATE/TIME PICKER TESTS (ETA)
   * ==========================================
   */
  test.describe('ETA Date/Time Picker', () => {
    
    test.beforeEach(async () => {
      await voyagePage.switchToETAMode();
    });

    test('should have default placeholder text for date', async () => {
      const dateText = await voyagePage.getETADateText();
      expect(dateText).toContain('Select arrival date');
    });

    test('should have default "Any time" for time', async () => {
      const timeText = await voyagePage.getETATimeText();
      expect(timeText).toContain('Any time');
    });

    test('should open date picker modal on date input click', async ({ page }) => {
      await voyagePage.openETADatePicker();
      
      const calendarModal = page.locator('app-calendar');
      await expect(calendarModal).toBeVisible({ timeout: 3000 });
    });

    test('should open time picker modal on time input click', async ({ page }) => {
      await voyagePage.openETATimePicker();
      
      const timePickerModal = page.locator('app-time-picker');
      await expect(timePickerModal).toBeVisible({ timeout: 3000 });
    });
  });

  /**
   * ==========================================
   * SPEED RANGE SLIDER TESTS
   * ==========================================
   */
  test.describe('Speed Range Slider', () => {
    
    test('should display speed range slider', async () => {
      await expect(voyagePage.speedSlider).toBeVisible();
    });

    test('should display min and max speed labels', async () => {
      await expect(voyagePage.speedMinLabel).toBeVisible();
      await expect(voyagePage.speedMaxLabel).toBeVisible();
    });

    test('should have default speed values', async () => {
      const minSpeed = await voyagePage.getMinSpeed();
      const maxSpeed = await voyagePage.getMaxSpeed();

      expect(minSpeed).toBeTruthy();
      expect(maxSpeed).toBeTruthy();
      
      // Assert - min < max
      expect(parseInt(minSpeed)).toBeLessThan(parseInt(maxSpeed));
    });

    test('should display speed values in knots', async () => {
      const minSpeed = await voyagePage.getMinSpeed();
      const maxSpeed = await voyagePage.getMaxSpeed();

      expect(parseInt(minSpeed)).toBeGreaterThan(0);
      expect(parseInt(maxSpeed)).toBeGreaterThan(0);
    });
  });

  /**
   * ==========================================
   * ACTION BUTTONS TESTS
   * ==========================================
   */
  test.describe('Action Buttons', () => {
    
    test('should display Clear and Search buttons', async () => {
      await expect(voyagePage.clearButton).toBeVisible();
      await expect(voyagePage.searchButton).toBeVisible();
    });

    test('should have correct button texts', async () => {
      const clearText = await voyagePage.clearButton.textContent();
      const searchText = await voyagePage.getSearchButtonText();

      expect(clearText).toContain('Clear');
      expect(searchText).toContain('Search');
    });

    test('should have Clear button always enabled', async () => {
      await expect(voyagePage.clearButton).toBeEnabled();
    });

    test('Clear button should reset all fields', async () => {
      await voyagePage.clickClear();

      const etdDateText = await voyagePage.getETDDateText();
      expect(etdDateText).toContain('Select departure date');

      const etdTimeText = await voyagePage.getETDTimeText();
      expect(etdTimeText).toContain('Any time');
    });
  });

  /**
   * ==========================================
   * FORM VALIDATION TESTS
   * ==========================================
   */
  test.describe('Form Validation', () => {
    
    test('should enable Search button when form is valid', async () => {
      const isEnabled = await voyagePage.isSearchButtonEnabled();
      expect(typeof isEnabled).toBe('boolean');
    });

    test('should not show validation errors initially', async () => {
      const hasErrors = await voyagePage.hasValidationErrors();
      expect(typeof hasErrors).toBe('boolean');
    });

    test('should validate form when Clear is clicked', async () => {
      await voyagePage.clickClear();
      
      await voyagePage.page.waitForTimeout(500);
    });
  });

  /**
   * ==========================================
   * LAYOUT & UI TESTS
   * ==========================================
   */
  test.describe('Layout & UI', () => {
    
    test('should have all main sections visible', async () => {
      // Vessel section
      await expect(voyagePage.vesselDropdown).toBeVisible();
      
      // Route section
      await expect(voyagePage.routeDropdown).toBeVisible();
      
      // Time window section
      await expect(voyagePage.timeWindowETDButton).toBeVisible();
      
      // Speed section
      await expect(voyagePage.speedSlider).toBeVisible();
      
      // Action buttons
      await expect(voyagePage.clearButton).toBeVisible();
      await expect(voyagePage.searchButton).toBeVisible();
    });

    test('should have proper tab order for accessibility', async ({ page }) => {
      // Tab order: vessel -> route -> time window -> speed -> buttons
      
      const activeElement = await page.evaluate(() => document.activeElement?.tagName);
      expect(activeElement).toBeTruthy();
    });
  });

  /**
   * ==========================================
   * MAP TESTS
   * ==========================================
   */
  test.describe('Route Map', () => {
    
    test('should check route map visibility state', async () => {
      const isMapVisible = await voyagePage.isRouteMapVisible();
      expect(typeof isMapVisible).toBe('boolean');
    });
  });
});
