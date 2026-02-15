import { test as base, expect, type Page } from '@playwright/test';

export { expect };

interface AuthenticatedPageFixture {
  authenticatedPage: Page;
}

const testFixtures = base.extend<AuthenticatedPageFixture>({
  authenticatedPage: async ({ page }, use) => {
    const TEST_USERNAME = process.env.E2E_TEST_USERNAME || 'testuser';
    const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD || 'testpass';
    const BASE_URL = process.env.BASE_URL || 'http://localhost:4321';

    await page.goto(`${BASE_URL}/login`);

    await page.fill('input[name="username"]', TEST_USERNAME);
    await page.fill('input[name="password"]', TEST_PASSWORD);

    await page.click('button[type="submit"]');

    await page.waitForURL(/\/ultra\/(calendar|profile)/, { timeout: 15000 });

    await use(page);
  },
});

export const test = testFixtures;

export async function authenticate(page: import('@playwright/test').Page) {
  const TEST_USERNAME = process.env.E2E_TEST_USERNAME || 'testuser';
  const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD || 'testpass';
  const BASE_URL = process.env.BASE_URL || 'http://localhost:4321';

  await page.goto(`${BASE_URL}/login`);
  await page.fill('input[name="username"]', TEST_USERNAME);
  await page.fill('input[name="password"]', TEST_PASSWORD);
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/ultra\/(calendar|profile)/);
}

export async function createEvent(
  page: import('@playwright/test').Page,
  eventData: {
    title: string;
    start: string;
    end: string;
    location?: string;
    color?: string;
  }
) {
  await page.click('button[aria-label="Add event"]');

  await expect(page.locator('[data-state="open"][role="dialog"], [data-slot="dialog-content"][data-state="open"]')).toBeVisible({ timeout: 10000 });

  await page.fill('input[id="ec-title"]', eventData.title);
  await page.fill('input[id="ec-start"]', eventData.start);
  await page.fill('input[id="ec-end"]', eventData.end);

  if (eventData.location) {
    await page.fill('input[id="ec-location"]', eventData.location);
  }

  if (eventData.color) {
    await page.fill('input[id="ec-color"]', eventData.color);
  }

  await page.click('button:has-text("Save Event")');

  await expect(page.locator('text=Event created successfully').first()).toBeVisible({ timeout: 10000 });
}

export function toLocalISO(d: Date): string {
  const pad = (n: number) => n.toString().padStart(2, '0');
  const y = d.getFullYear();
  const m = pad(d.getMonth() + 1);
  const day = pad(d.getDate());
  const h = pad(d.getHours());
  const min = pad(d.getMinutes());
  return `${y}-${m}-${day}T${h}:${min}`;
}

export function getFutureDate(hoursFromNow: number): { start: string; end: string } {
  const start = new Date();
  start.setHours(start.getHours() + hoursFromNow, 0, 0, 0);

  const end = new Date(start);
  end.setHours(end.getHours() + 1);

  return {
    start: toLocalISO(start),
    end: toLocalISO(end),
  };
}

const BASE_URL = process.env.BASE_URL || 'http://localhost:4321';

export async function navigateToCalendar(page: import('@playwright/test').Page) {
  await page.goto(`${BASE_URL}/ultra/calendar`);
  await expect(page.locator('h1')).toBeVisible();
}

export async function navigateToProfile(page: import('@playwright/test').Page) {
  await page.goto(`${BASE_URL}/ultra/profile`);
  await expect(page).toHaveURL(/\/ultra\/profile/);
}

export async function navigateToLogin(page: import('@playwright/test').Page) {
  await page.goto(`${BASE_URL}/login`);
}
