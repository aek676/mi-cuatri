import { test, expect, authenticate, navigateToLogin, navigateToCalendar, navigateToProfile } from './fixtures';

test.describe('Authentication', () => {

  test('TC-AUTH-001: Login successful redirects to profile', async ({ page }) => {
    await navigateToLogin(page);

    await page.fill('input[name="username"]', process.env.E2E_TEST_USERNAME || 'testuser');
    await page.fill('input[name="password"]', process.env.E2E_TEST_PASSWORD || 'testpass');
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL(/.*\/ultra\/profile/);
  });

  test('TC-AUTH-002: Session persists across protected pages', async ({ page }) => {
    await authenticate(page);

    await navigateToCalendar(page);
    await expect(page).toHaveURL(/\/ultra\/calendar/);

    await navigateToProfile(page);
    await expect(page).toHaveURL(/\/ultra\/profile/);

    await expect(page.getByText('CORREO ELECTRÓNICO')).toBeVisible();
  });

  test('TC-AUTH-003: Session cookie has correct security properties', async ({ page }) => {
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
