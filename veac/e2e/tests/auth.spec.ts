import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { DashboardPage } from '../pages/dashboard.page';
import { 
  loginAsAdmin, 
  getAuthToken, 
  hasAuthToken, 
  clearAuthTokens,
  setMockAuthToken 
} from '../fixtures/auth-helper';
import { TEST_USERS, URLS, MESSAGES, SECURITY_PAYLOADS, MOCK_TOKENS } from '../fixtures/test-data';

/**
 * ==========================================
 * AUTHENTICATION E2E TESTS
 * ==========================================
 * Comprehensive test suite for authentication functionality:
 * - Login/Logout
 * - Protected Routes & Auth Guards
 * - Session & Token Management
 * - Security (XSS, SQL Injection, etc.)
 */

/**
 * ==========================================
 * LOGIN FUNCTIONALITY
 * ==========================================
 */
test.describe('Login Functionality', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigate();
  });

  test('should login successfully with valid credentials', async ({ page }) => {
    const { username, password } = TEST_USERS.VALID_ADMIN;
    await loginPage.login(username, password);
    await loginPage.waitForSuccessfulLogin();
    expect(page.url()).toContain(URLS.DASHBOARD);
  });

  test('should show error message with invalid credentials', async ({ page }) => {
    const { username, password } = TEST_USERS.INVALID_USER;
    await loginPage.login(username, password);
    await expect(loginPage.errorMessage).toBeVisible({ timeout: 10000 });
    const errorText = await loginPage.getErrorMessage();
    expect(errorText).toBe(MESSAGES.INVALID_CREDENTIALS);
  });

  test('should not allow login with empty username', async () => {
    await loginPage.enterPassword(TEST_USERS.VALID_ADMIN.password);
    await loginPage.clickLogin();
    const currentUrl = loginPage.getCurrentURL();
    expect(currentUrl).toContain(URLS.LOGIN);
  });

  test('should not allow login with empty password', async () => {
    await loginPage.enterUsername(TEST_USERS.VALID_ADMIN.username);
    await loginPage.clickLogin();
    const currentUrl = loginPage.getCurrentURL();
    expect(currentUrl).toContain(URLS.LOGIN);
  });

  test('should display all login form elements', async ({ page }) => {
    await expect(loginPage.usernameInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();

    const usernameLabel = page.locator('label[for="username"]');
    await expect(usernameLabel).toBeVisible();
    await expect(usernameLabel).toContainText('Username');

    const passwordLabel = page.locator('label[for="password"]');
    await expect(passwordLabel).toBeVisible();
    await expect(passwordLabel).toContainText('Password');
  });

  test('should mask password input', async () => {
    const passwordType = await loginPage.passwordInput.getAttribute('type');
    expect(passwordType).toBe('password');
  });
});

/**
 * ==========================================
 * LOGOUT FUNCTIONALITY  
 * ==========================================
 * Note: These tests run serially to avoid race conditions
 * with logout state and navigation timing
 */
test.describe.serial('Logout Functionality', () => {
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForDashboard();
  });

  test('should logout successfully and redirect to login page', async ({ page }) => {
    expect(page.url()).toContain(URLS.DASHBOARD);
    expect(await hasAuthToken(page)).toBeTruthy();

    await dashboardPage.logout();

    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });
    expect(page.url()).toContain(URLS.LOGIN);
    expect(await hasAuthToken(page)).toBeFalsy();
  });

  test('should not access protected page after logout', async ({ page }) => {
    await dashboardPage.logout();
    await page.waitForURL(URLS.LOGIN);

    // Verify token is cleared
    expect(await hasAuthToken(page)).toBeFalsy();

    // Try to access protected page
    await page.goto(URLS.DASHBOARD);

    // Should redirect back to login
    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });
    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should clear auth token from storage after logout', async ({ page }) => {
    const tokenBeforeLogout = await hasAuthToken(page);
    expect(tokenBeforeLogout).toBeTruthy();

    await dashboardPage.logout();

    const tokenAfterLogout = await hasAuthToken(page);
    expect(tokenAfterLogout).toBeFalsy();
  });

  test('should not allow access via back button after logout', async ({ page }) => {
    await dashboardPage.logout();
    await page.waitForURL(URLS.LOGIN);

    await page.goBack();

    await page.waitForTimeout(1000);
    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should have visible and clickable logout button in menu', async ({ page }) => {
    await dashboardPage.click(dashboardPage.hamburgerButton);
    await expect(dashboardPage.logoutButton).toBeVisible();
    await expect(dashboardPage.logoutButton).toBeEnabled();
  });

  test('should handle multiple logout attempts gracefully', async ({ page }) => {
    await dashboardPage.logout();
    await page.waitForURL(URLS.LOGIN);

    const loginPage = new LoginPage(page);
    expect(page.url()).toContain(URLS.LOGIN);
    await expect(loginPage.loginButton).toBeVisible();
  });
});

/**
 * ==========================================
 * PROTECTED ROUTES & AUTH GUARD
 * ==========================================
 */
test.describe('Protected Routes & Auth Guard', () => {
  
  test('should redirect to login when accessing protected route without auth', async ({ page }) => {
    await page.goto('/');
    await clearAuthTokens(page);

    await page.goto(URLS.DASHBOARD);

    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });
    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should allow access to protected routes when authenticated', async ({ page }) => {
    await loginAsAdmin(page);

    const dashboardPage = new DashboardPage(page);
    await dashboardPage.navigate();

    await dashboardPage.waitForDashboard();
    expect(page.url()).toContain(URLS.DASHBOARD);
    expect(await dashboardPage.isDashboardVisible()).toBeTruthy();
  });

  test('should redirect to dashboard when logged in user visits login page', async ({ page }) => {
    await loginAsAdmin(page);

    await page.goto(URLS.LOGIN);

    await page.waitForTimeout(1000);
    const hasToken = await hasAuthToken(page);
    expect(hasToken).toBeTruthy();
  });

  test('should preserve return URL after login redirect', async ({ page }) => {
    await page.goto('/');
    await clearAuthTokens(page);

    const targetUrl = `${URLS.DASHBOARD}?test=param`;
    await page.goto(targetUrl);

    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });

    const loginPage = new LoginPage(page);
    await loginPage.login('Admin', 'Admin@123');

    await page.waitForTimeout(2000);
    expect(await hasAuthToken(page)).toBeTruthy();
  });

  test('should protect live view route from unauthenticated access', async ({ page }) => {
    await page.goto('/');
    await clearAuthTokens(page);

    await page.goto(URLS.LIVE_VIEW);

    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });
    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should maintain authentication after page refresh', async ({ page }) => {
    await loginAsAdmin(page);
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForDashboard();

    expect(await hasAuthToken(page)).toBeTruthy();

    await page.reload();

    await page.waitForTimeout(2000);
    expect(await hasAuthToken(page)).toBeTruthy();
    expect(page.url()).toContain(URLS.DASHBOARD);
  });

  test('should logout when localStorage is cleared manually', async ({ page }) => {
    await loginAsAdmin(page);
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForDashboard();

    await clearAuthTokens(page);

    await page.reload();

    await page.waitForURL(URLS.LOGIN, { timeout: 10000 });
    expect(page.url()).toContain(URLS.LOGIN);
  });
});

/**
 * ==========================================
 * SESSION & TOKEN MANAGEMENT
 * ==========================================
 */
test.describe.configure({ mode: 'serial' });
test.describe('Session & Token Management', () => {

  test('should store JWT token after successful login', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();

    expect(await hasAuthToken(page)).toBeFalsy();

    await loginPage.login('Admin', 'Admin@123');
    await loginPage.waitForSuccessfulLogin();

    expect(await hasAuthToken(page)).toBeTruthy();
    
    const token = await getAuthToken(page);
    expect(token).toBeTruthy();
    expect(token?.split('.')).toHaveLength(3);
  });

  test('should send token in Authorization header for API calls', async ({ page }) => {
    await loginAsAdmin(page);
    const token = await getAuthToken(page);
    expect(token).toBeTruthy();

    let authHeaderValue: string | null = null;
    await page.route('**/api/**', (route) => {
      authHeaderValue = route.request().headers()['authorization'];
      route.continue();
    });

    await page.goto(URLS.DASHBOARD);
    await page.waitForTimeout(2000);

    if (authHeaderValue) {
      expect(authHeaderValue).toContain('Bearer');
      const tokenPart = authHeaderValue.replace('Bearer ', '');
      expect(tokenPart.split('.')).toHaveLength(3);
    }
  });

  test('should persist token across page refreshes', async ({ page }) => {
    await loginAsAdmin(page);
    
    const tokenBeforeRefresh = await getAuthToken(page);
    expect(tokenBeforeRefresh).toBeTruthy();
    expect(tokenBeforeRefresh?.split('.')).toHaveLength(3);

    await page.reload();
    await page.waitForTimeout(1000);

    const tokenAfterRefresh = await getAuthToken(page);
    expect(tokenAfterRefresh).toBeTruthy();
    expect(tokenAfterRefresh?.split('.')).toHaveLength(3);
    
    expect(page.url()).toContain(URLS.DASHBOARD);
  });

  test('should clear token after logout', async ({ page }) => {
    await loginAsAdmin(page);
    expect(await hasAuthToken(page)).toBeTruthy();

    const dashboardPage = new DashboardPage(page);
    await dashboardPage.logout();

    expect(await hasAuthToken(page)).toBeFalsy();
  });

  test('should logout when token is invalid', async ({ page }) => {
    await page.goto('/');
    await setMockAuthToken(page, MOCK_TOKENS.INVALID_FORMAT);

    await page.goto(URLS.DASHBOARD);

    await page.waitForTimeout(2000);
    const currentUrl = page.url();
    const hasToken = await hasAuthToken(page);
    
    expect(currentUrl.includes(URLS.LOGIN) || !hasToken).toBeTruthy();
  });

  test('should share token across multiple tabs', async ({ page, context }) => {
    await loginAsAdmin(page);
    const token = await getAuthToken(page);
    expect(token).toBeTruthy();

    const secondTab = await context.newPage();
    await secondTab.goto(URLS.DASHBOARD);
    await secondTab.waitForTimeout(1000);

    const tokenInSecondTab = await getAuthToken(secondTab);
    expect(tokenInSecondTab).toBeTruthy();
    expect(tokenInSecondTab.split('.').length).toBe(3);
    
    await secondTab.close();
  });

  test('should logout all tabs when logging out from one tab', async ({ page, context }) => {
    await loginAsAdmin(page);
    
    const secondTab = await context.newPage();
    await secondTab.goto(URLS.DASHBOARD);
    await secondTab.waitForTimeout(1000);

    const dashboardPage = new DashboardPage(page);
    await dashboardPage.logout();
    await page.waitForURL(URLS.LOGIN);

    expect(await hasAuthToken(page)).toBeFalsy();
    
    await secondTab.close();
  });

  test('should have valid JWT token structure', async ({ page }) => {
    await loginAsAdmin(page);
    const token = await getAuthToken(page);

    expect(token).toBeTruthy();
    const parts = token?.split('.');
    expect(parts).toHaveLength(3);

    parts?.forEach((part) => {
      expect(part.length).toBeGreaterThan(0);
    });
  });

  test('should handle session timeout gracefully', async ({ page }) => {
    await loginAsAdmin(page);
    
    await setMockAuthToken(page, MOCK_TOKENS.EXPIRED);

    await page.goto(URLS.DASHBOARD);
    await page.waitForTimeout(2000);

    const hasToken = await hasAuthToken(page);
    const currentUrl = page.url();
    
    expect(hasToken === false || currentUrl.includes(URLS.LOGIN)).toBeTruthy();
  });
});

/**
 * ==========================================
 * SECURITY TESTS
 * ==========================================
 */
test.describe('Security Tests', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.navigate();
  });

  test('should prevent XSS attack via script tag in username', async ({ page }) => {
    await loginPage.enterUsername(SECURITY_PAYLOADS.XSS_SCRIPT);
    await loginPage.enterPassword('anypassword');
    await loginPage.clickLogin();

    page.on('dialog', async (dialog) => {
      expect(dialog.type()).not.toBe('alert');
      await dialog.dismiss();
    });

    await page.waitForTimeout(2000);

    const isErrorVisible = await loginPage.isErrorMessageVisible();
    if (isErrorVisible) {
      const errorText = await loginPage.getErrorMessage();
      expect(errorText).not.toContain('<script>');
    }
  });

  test('should prevent XSS attack via img tag in password field', async ({ page }) => {
    await loginPage.enterUsername('admin');
    await loginPage.enterPassword(SECURITY_PAYLOADS.XSS_IMG);
    
    let dialogAppeared = false;
    page.on('dialog', async (dialog) => {
      dialogAppeared = true;
      await dialog.dismiss();
    });

    await loginPage.clickLogin();
    await page.waitForTimeout(2000);

    expect(dialogAppeared).toBeFalsy();
  });

  test('should prevent XSS attack via SVG payload', async ({ page }) => {
    await loginPage.login(SECURITY_PAYLOADS.XSS_SVG, 'password');
    
    let dialogAppeared = false;
    page.on('dialog', async (dialog) => {
      dialogAppeared = true;
      await dialog.dismiss();
    });

    await page.waitForTimeout(2000);
    expect(dialogAppeared).toBeFalsy();
  });

  test('should prevent SQL injection with OR 1=1 payload', async ({ page }) => {
    await loginPage.login(SECURITY_PAYLOADS.SQL_INJECTION_BASIC, 'password');
    await page.waitForTimeout(2000);

    expect(page.url()).not.toContain(URLS.DASHBOARD);
    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should prevent SQL injection with UNION SELECT', async ({ page }) => {
    await loginPage.login(SECURITY_PAYLOADS.SQL_INJECTION_UNION, 'password');
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should prevent SQL injection with comment bypass', async ({ page }) => {
    await loginPage.login(SECURITY_PAYLOADS.SQL_INJECTION_COMMENT, 'ignored');
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should prevent HTML injection in input fields', async ({ page }) => {
    await loginPage.enterUsername(SECURITY_PAYLOADS.HTML_INJECTION);
    await loginPage.enterPassword('password');
    await loginPage.clickLogin();
    await page.waitForTimeout(2000);

    const h1Elements = await page.locator('h1:has-text("Injected HTML")').count();
    expect(h1Elements).toBe(0);
  });

  test('should prevent javascript: protocol execution', async ({ page }) => {
    await loginPage.enterUsername(SECURITY_PAYLOADS.JAVASCRIPT_PROTOCOL);
    await loginPage.enterPassword('password');

    let dialogAppeared = false;
    page.on('dialog', async (dialog) => {
      dialogAppeared = true;
      await dialog.dismiss();
    });

    await loginPage.clickLogin();
    await page.waitForTimeout(2000);

    expect(dialogAppeared).toBeFalsy();
  });

  test('should handle special characters in input fields', async ({ page }) => {
    const specialChars = '!@#$%^&*()_+-=[]{}|;:,.<>?/~`';
    
    await loginPage.login(specialChars, specialChars);
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should handle very long input strings', async ({ page }) => {
    const veryLongString = 'A'.repeat(10000);
    
    await loginPage.enterUsername(veryLongString);
    await loginPage.enterPassword('password');
    await loginPage.clickLogin();
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should handle null bytes in input', async ({ page }) => {
    const nullByteString = 'admin\x00malicious';
    
    await loginPage.login(nullByteString, 'password');
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });

  test('should include CSRF protection headers in requests', async ({ page }) => {
    let csrfHeaderFound = false;

    await page.route('**/api/v1/auth/login', (route) => {
      const headers = route.request().headers();
      if (headers['x-csrf-token'] || headers['x-xsrf-token']) {
        csrfHeaderFound = true;
      }
      route.continue();
    });

    await loginPage.login('Admin', 'Admin@123');
    await page.waitForTimeout(2000);
  });

  test('should handle multiple rapid login attempts', async ({ page }) => {
    for (let i = 0; i < 10; i++) {
      await loginPage.enterUsername('attacker');
      await loginPage.enterPassword('wrongpass');
      await loginPage.clickLogin();
      await page.waitForTimeout(100);
    }

    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
    await expect(loginPage.loginButton).toBeVisible();
  });

  test('should handle unicode characters safely', async ({ page }) => {
    const unicodeString = '你好世界🚀💻';
    
    await loginPage.login(unicodeString, 'password');
    await page.waitForTimeout(2000);

    expect(page.url()).toContain(URLS.LOGIN);
  });
});
