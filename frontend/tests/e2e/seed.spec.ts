import { test, expect, authenticate, createEvent, getFutureDate, navigateToCalendar, navigateToLogin } from './fixtures';

test.describe('Seed Tests', () => {

  test('TC-SEED-001: Establish authenticated session with valid credentials', async ({ page }) => {
    await navigateToLogin(page);

    await expect(page.locator('h1:has-text("Mi Cuatri")')).toBeVisible();
    await expect(page.locator('input[name="username"]')).toBeVisible();
    await expect(page.locator('input[name="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();

    await page.fill('input[name="username"]', process.env.E2E_TEST_USERNAME || 'testuser');
    await page.fill('input[name="password"]', process.env.E2E_TEST_PASSWORD || 'testpass');
    await page.click('button[type="submit"]');

    await page.waitForURL(/\/ultra\/(calendar|profile)/, { timeout: 15000 });

    const sessionCookie = await page.context().cookies();
    expect(sessionCookie.find(c => c.name === 'bb_session')).toBeDefined();
  });

  test('TC-SEED-002: Create single calendar event', async ({ page }) => {
    await authenticate(page);
    await navigateToCalendar(page);

    const { start, end } = getFutureDate(24);

    await createEvent(page, {
      title: 'Test Event Seed',
      start,
      end,
      location: 'Test Location',
      color: '#315F94',
    });
  });

  test('TC-SEED-003: Create multiple events for batch operations', async ({ page }) => {
    await authenticate(page);
    await navigateToCalendar(page);

    const events = [
      { title: 'Morning Event', hoursFromNow: 2, color: '#315F94' },
      { title: 'Afternoon Event', hoursFromNow: 5, color: '#E53E3E' },
      { title: 'Tomorrow Event', hoursFromNow: 26, color: '#38A169' },
      { title: 'Next Week Event', hoursFromNow: 24 * 7, color: '#805AD5' },
      { title: 'Evening Event', hoursFromNow: 8, color: '#D69E2E' },
    ];

    for (const event of events) {
      const { start, end } = getFutureDate(event.hoursFromNow);
      await createEvent(page, {
        title: event.title,
        start,
        end,
        color: event.color,
      });
    }
  });

});
