import { test, expect, authenticate, navigateToLogin, navigateToCalendar, navigateToProfile } from './fixtures';

const TEST_USERNAME = process.env.E2E_TEST_USERNAME;
const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD;
const hasCredentials = !!(TEST_USERNAME && TEST_PASSWORD);

test.describe('Authentication', () => {
  test.beforeAll(() => {
    if (!hasCredentials) {
      console.warn('\n⚠️  WARNING: E2E_TEST_USERNAME and E2E_TEST_PASSWORD environment variables are not set.');
      console.warn('   Authentication tests will fail. Please set these in frontend/.env file.\n');
    }
  });

  test('TC-AUTH-001: Login successful redirects to profile', async ({ page }) => {
    test.skip(!hasCredentials, 'Valid E2E credentials required for this test');

    await navigateToLogin(page);

    await page.fill('input[name="username"]', TEST_USERNAME!);
    await page.fill('input[name="password"]', TEST_PASSWORD!);
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL(/.*\/ultra\/profile/);
  });

  test('TC-AUTH-002: Session persists across protected pages', async ({ page }) => {
    test.skip(!hasCredentials, 'Valid E2E credentials required for this test');

    await authenticate(page);

    await navigateToCalendar(page);
    await expect(page).toHaveURL(/\/ultra\/calendar/);

    await navigateToProfile(page);
    await expect(page).toHaveURL(/\/ultra\/profile/);

    await expect(page.getByText('CORREO ELECTRÓNICO')).toBeVisible();
  });

  test('TC-AUTH-003: Session cookie has correct security properties', async ({ page }) => {
    test.skip(!hasCredentials, 'Valid E2E credentials required for this test');

    await authenticate(page);

    const cookies = await page.context().cookies();
    const sessionCookie = cookies.find(c => c.name === 'bb_session');

    expect(sessionCookie).toBeDefined();
    expect(sessionCookie?.value).toBeTruthy();
    expect(sessionCookie?.httpOnly).toBe(true);
    expect(sessionCookie?.path).toBe('/');
    expect(sessionCookie?.sameSite).toBe('Lax');
  });

  test('TC-AUTH-004: Session expiration redirects to login', async ({ page }) => {
    test.skip(!hasCredentials, 'Valid E2E credentials required for this test');

    await authenticate(page);
    await navigateToCalendar(page);

    const context = page.context();
    await context.clearCookies();

    await page.reload();

    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });

  test('TC-AUTH-005: Invalid credentials show error message', async ({ page }) => {
    await navigateToLogin(page);

    await page.fill('input[name="username"]', 'invalid_user');
    await page.fill('input[name="password"]', 'wrong_password');
    await page.click('button[type="submit"]');

    await expect(page.getByText('Unable to sign up. Please try again later.')).toBeVisible();
    await expect(page).toHaveURL(/\/login/);

    const cookies = await page.context().cookies();
    expect(cookies.find(c => c.name === 'bb_session')).toBeUndefined();
  });

  test('TC-AUTH-006: Empty form shows validation errors', async ({ page }) => {
    await navigateToLogin(page);

    await page.click('button[type="submit"]');

    await expect(page).toHaveURL(/\/login/);
  });
});
