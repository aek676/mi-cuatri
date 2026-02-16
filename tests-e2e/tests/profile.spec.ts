import { test, expect, authenticate, navigateToProfile } from './fixtures';

const TEST_USERNAME = process.env.E2E_TEST_USERNAME;
const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD;
const hasCredentials = !!(TEST_USERNAME && TEST_PASSWORD);

test.describe('Profile', () => {

  test.beforeEach(async ({ page }) => {
    test.skip(!hasCredentials, 'E2E_TEST_USERNAME and E2E_TEST_PASSWORD environment variables are required');
    await authenticate(page);
  });

  test('TC-PROFILE-001: Profile page displays user information', async ({ page }) => {
    await navigateToProfile(page);

    await expect(page.getByText('CORREO ELECTRÓNICO')).toBeVisible();
    await expect(page.locator('button:has-text("Log out")')).toBeVisible();
  });

  test('TC-PROFILE-002: Logout functionality works correctly', async ({ page }) => {
    await navigateToProfile(page);

    await page.click('button:has-text("Log out")');

    await expect(page).toHaveURL(/\/login/, { timeout: 10000 });
  });

});
