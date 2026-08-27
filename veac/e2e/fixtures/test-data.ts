/**
 * Test Data and Constants
 */

export const TEST_USERS = {
  VALID_ADMIN: {
    username: 'Admin',
    password: 'Admin@123',
  },
  INVALID_USER: {
    username: 'InvalidUser',
    password: 'WrongPassword123',
  },
};

export const URLS = {
  LOGIN: '/login',
  DASHBOARD: '/vec',
  LIVE_VIEW: '/live',
};

export const MESSAGES = {
  INVALID_CREDENTIALS: 'Invalid username or password',
};

export const TIMEOUTS = {
  DEFAULT: 10000,
  NAVIGATION: 15000,
  API_CALL: 5000,
};

export const SECURITY_PAYLOADS = {
  XSS_SCRIPT: '<script>alert("XSS")</script>',
  XSS_IMG: '<img src=x onerror=alert("XSS")>',
  XSS_SVG: '<svg/onload=alert("XSS")>',
  SQL_INJECTION_BASIC: "' OR '1'='1",
  SQL_INJECTION_UNION: "' UNION SELECT NULL--",
  SQL_INJECTION_COMMENT: "admin'--",
  HTML_INJECTION: '<h1>Injected HTML</h1>',
  JAVASCRIPT_PROTOCOL: 'javascript:alert("XSS")',
};

export const MOCK_TOKENS = {
  INVALID_FORMAT: 'invalid.token.format',
  EXPIRED: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c',
  MALFORMED: 'not-a-valid-jwt',
};
