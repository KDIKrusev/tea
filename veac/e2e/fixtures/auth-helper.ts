import { Page } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { TEST_USERS } from './test-data';

/**
 * Auth Helper Functions
 */

export async function loginAsAdmin(page: Page): Promise<void> {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();
  await loginPage.login(TEST_USERS.VALID_ADMIN.username, TEST_USERS.VALID_ADMIN.password);
  await loginPage.waitForSuccessfulLogin();
}

export async function getAuthToken(page: Page): Promise<string | null> {
  return await page.evaluate(() => {
    return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
  });
}

export async function hasAuthToken(page: Page): Promise<boolean> {
  const token = await getAuthToken(page);
  return token !== null && token.length > 0;
}

export async function clearAuthTokens(page: Page): Promise<void> {
  await page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
}

export async function setMockAuthToken(page: Page, token: string): Promise<void> {
  await page.evaluate((mockToken) => {
    localStorage.setItem('authToken', mockToken);
  }, token);
}
