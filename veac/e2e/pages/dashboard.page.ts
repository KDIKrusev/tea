import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Dashboard Page Object Model
 */
export class DashboardPage extends BasePage {
  readonly hamburgerButton: Locator;
  readonly logoutButton: Locator;
  readonly planningViewTab: Locator;
  readonly liveViewTab: Locator;
  readonly topBar: Locator;

  constructor(page: Page) {
    super(page);
    this.hamburgerButton = page.locator('.vea-hamburger-btn');
    this.logoutButton = page.locator('button.vea-menu-item:has-text("Log out")');
    this.planningViewTab = page.locator('a[routerLink="/vec"]');
    this.liveViewTab = page.locator('a[routerLink="/live"]');
    this.topBar = page.locator('.vea-top-bar').first();
  }

  async navigate() {
    await this.goto('/vec');
  }

  async logout() {
    await this.click(this.hamburgerButton);
    await this.waitForElement(this.logoutButton);
    await this.click(this.logoutButton);
  }

  async isDashboardVisible(): Promise<boolean> {
    return await this.isVisible(this.topBar);
  }

  async waitForDashboard() {
    await this.waitForElement(this.topBar);
  }
}
