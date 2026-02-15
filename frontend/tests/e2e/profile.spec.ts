import { test, expect, authenticate, navigateToProfile } from './fixtures';

test.describe('Profile', () => {

  test.beforeEach(async ({ page }) => {
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
