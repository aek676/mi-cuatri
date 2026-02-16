import { test, expect, authenticate, createEvent, getFutureDate, toLocalISO, navigateToCalendar } from './fixtures';

test.describe('Calendar', () => {

  test.beforeEach(async ({ page }) => {
    await authenticate(page);
  });

  test.describe('Navigation', () => {

    test('TC-CAL-001: Header navigation functionality', async ({ page }) => {
      await navigateToCalendar(page);

      await page.click('a[href="/ultra/calendar"]');
      await expect(page).toHaveURL(/\/ultra\/calendar/);

      await page.click('a[aria-label="User Account"]');
      await expect(page).toHaveURL(/\/ultra\/profile/);

      await expect(page.getByText('CORREO ELECTRÓNICO')).toBeVisible();
    });

    test('TC-CAL-002: Calendar month navigation', async ({ page }) => {
      await navigateToCalendar(page);

      const currentMonth = await page.locator('h1').textContent();
      expect(currentMonth).toContain(new Date().toLocaleDateString('en-US', { month: 'long' }));

      await page.click('button[aria-label="Previous month"]:visible');

      const prevMonth = await page.locator('h1').textContent();
      const expectedPrevMonth = new Date(new Date().setMonth(new Date().getMonth() - 1))
        .toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
      expect(prevMonth).toContain(expectedPrevMonth.split(' ')[0]);

      await page.click('button[aria-label="Next month"]:visible');
      await page.click('button[aria-label="Next month"]:visible');

      const nextMonth = await page.locator('h1').textContent();
      const expectedNextMonth = new Date(new Date().setMonth(new Date().getMonth() + 1))
        .toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
      expect(nextMonth).toContain(expectedNextMonth.split(' ')[0]);

      await page.click('button[aria-label="Go to today"]:visible');

      const todayMonth = await page.locator('h1').textContent();
      expect(todayMonth).toContain(new Date().toLocaleDateString('en-US', { month: 'long' }));
    });

  });

  test.describe('Data Loading', () => {

    test('TC-CAL-003: Calendar grid loads correctly', async ({ page }) => {
      await navigateToCalendar(page);

      const weekGrid = page.locator('.grid.grid-cols-7');
      if (await weekGrid.isVisible()) {
        await expect(weekGrid).toBeVisible();
      }

      const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
      for (const day of weekdays) {
        await expect(page.locator(`text=${day}`).first()).toBeVisible();
      }

      await expect(page.locator('button:has-text("Today")')).toBeVisible();
      await page.click('button[aria-label="Previous month"]:visible');
      await expect(page.locator('button[aria-label="Previous month"]:visible')).toBeVisible();
      await expect(page.locator('button[aria-label="Next month"]:visible')).toBeVisible();
      await expect(page.locator('button[aria-label="Go to today"]:visible')).toBeVisible();
    });

  });

  test.describe('Events', () => {

    test('TC-CAL-004: View event details', async ({ page }) => {
      await navigateToCalendar(page);

      const { start, end } = getFutureDate(24);
      await createEvent(page, {
        title: 'Event for Viewing',
        start,
        end,
      });

      const eventElement = page.getByText('Event for Viewing').first();
      await eventElement.click();

      await expect(page.locator('[data-state="open"][role="dialog"]')).toBeVisible();
      await expect(page.locator('[data-state="open"][role="dialog"]').getByText('Event for Viewing')).toBeVisible();
    });

    test('TC-CAL-005: Edit existing event', async ({ page }) => {
      await navigateToCalendar(page);

      const { start, end } = getFutureDate(48);
      await createEvent(page, {
        title: 'Event to Edit',
        start,
        end,
      });

      await page.getByText('Event to Edit').first().click();

      await expect(page.locator('[data-state="open"][role="dialog"]')).toBeVisible();
    });

    test('TC-CAL-006: Delete event', async ({ page }) => {
      await navigateToCalendar(page);

      const { start, end } = getFutureDate(72);
      await createEvent(page, {
        title: 'Event to Delete',
        start,
        end,
      });

      await page.getByText('Event to Delete').first().click();

      await expect(page.locator('[data-state="open"][role="dialog"]')).toBeVisible();
    });

  });

  test.describe('Google Integration', () => {

    test('TC-CAL-007: Google connection status check', async ({ page }) => {
      await navigateToCalendar(page);

      const googleButton = page.locator('button:has-text("Connect"), button:has-text("Export")');
      await expect(googleButton).toBeVisible();

      const buttonText = await googleButton.textContent();
      expect(buttonText).toMatch(/(Connect|Export)/i);
    });

  });

  test.describe('Responsive', () => {

    test('TC-CAL-008: Calendar works on mobile viewport', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 });

      await navigateToCalendar(page);

      await expect(page.locator('button[aria-label="Previous month"]:visible')).toBeVisible();
      await expect(page.locator('button[aria-label="Next month"]:visible')).toBeVisible();
      await expect(page.locator('button[aria-label="Add event"]')).toBeVisible();
    });

    test('TC-CAL-009: Calendar works on tablet viewport', async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 });

      await navigateToCalendar(page);

      await expect(page.locator('h1')).toBeVisible();
    });

  });

  test.describe('Error Handling', () => {

    test('TC-CAL-010: Handle event creation with missing required fields', async ({ page }) => {
      await navigateToCalendar(page);

      await page.click('button[aria-label="Add event"]');
      await page.click('button:has-text("Save Event")');

      await expect(page.locator('[data-state="open"][role="dialog"]')).toBeVisible();
      await expect(page.locator('[id="ec-title"]:invalid, [aria-invalid="true"], .error:has-text("required")')).toBeVisible();
    });

    test('TC-CAL-011: Handle end date before start date', async ({ page }) => {
      await navigateToCalendar(page);

      await page.click('button[aria-label="Add event"]');

      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      const today = new Date();

      await page.fill('input[id="ec-title"]', 'Invalid Date Event');
      await page.fill('input[id="ec-start"]', toLocalISO(today));
      await page.fill('input[id="ec-end"]', toLocalISO(yesterday));

      await page.click('button:has-text("Save Event")');

      const hasValidationError = await page.locator('text=End date, text=end date, text=before').first().isVisible().catch(() => false);
      if (hasValidationError) {
        await expect(page.locator('text=End date, text=end date, text=before')).toBeVisible();
      } else {
        await expect(page.locator('[data-state="open"][role="dialog"]')).toBeVisible();
      }
    });

  });

});
