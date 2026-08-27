import { Page, Locator } from '@playwright/test';

/**
 * Base Page Object - common methods for all pages
 */
export class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(url: string) {
    await this.page.goto(url);
  }

  async waitForElement(locator: Locator, timeout: number = 10000) {
    await locator.waitFor({ state: 'visible', timeout });
  }

  async fillInput(locator: Locator, text: string) {
    await locator.fill(text);
  }

  async click(locator: Locator) {
    await locator.click();
  }

  async getText(locator: Locator): Promise<string> {
    return await locator.textContent() || '';
  }

  async isVisible(locator: Locator): Promise<boolean> {
    return await locator.isVisible();
  }

  async waitForURL(url: string | RegExp, timeout: number = 10000) {
    await this.page.waitForURL(url, { timeout });
  }

  getCurrentURL(): string {
    return this.page.url();
  }
}
