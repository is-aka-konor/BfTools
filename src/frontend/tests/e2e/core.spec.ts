import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

const fixtures = (p: string) => path.join(process.cwd(), 'tests', 'fixtures', p);

test.describe('Core Journeys', () => {
  test('filters, search, offline, update flow, deep links', async ({ page, context, baseURL }) => {
    // Intercept manifest/data/index to serve from fixtures, to enable update swapping later
    let manifestMode: 'primary'|'alt' = 'primary';
    await page.route('**/dist-site/site-manifest.json', async (route) => {
      const file = manifestMode === 'primary' ? fixtures('site-manifest.json') : fixtures('site-manifest-alt.json');
      const body = fs.readFileSync(file, 'utf-8');
      await route.fulfill({ body, contentType: 'application/json' });
    });
    await page.route('**/dist-site/data/*', async (route) => {
      const url = new URL(route.request().url());
      const name = path.basename(url.pathname);
      const file = fixtures(path.join('data', name));
      const body = fs.readFileSync(file, 'utf-8');
      await route.fulfill({ body, contentType: 'application/json' });
    });
    await page.route('**/dist-site/index/*', async (route) => {
      const url = new URL(route.request().url());
      const name = path.basename(url.pathname);
      const file = fixtures(path.join('index', name));
      const body = fs.readFileSync(file, 'utf-8');
      await route.fulfill({ body, contentType: 'application/json' });
    });

    // First run
    await page.goto(baseURL!);
    // Wait for manifest to be requested and fulfilled (mocked route)
    await page.waitForResponse((resp) => resp.url().includes('/dist-site/site-manifest.json'));
    // Tiles present
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible();
    await expect(page.locator('a[href="/spells"] .badge')).toBeVisible({ timeout: 15000 });

    // Spells: open, apply filters, open detail, back preserves filters
    await page.goto(baseURL! + '/spells');
    await expect(page.getByRole('heading', { name: 'Spells' })).toBeVisible();
    const selects = page.locator('select.select');
    await selects.nth(0).selectOption('1'); // Circle=1
    await expect(page.locator('a.app-card')).toHaveCount(1);
    const firstCard = page.locator('a.app-card').first();
    const href = await firstCard.getAttribute('href');
    await firstCard.click();
    await expect(page.locator('h2')).toBeVisible();
    await page.goBack();
    // Persisted filter yields 1 card
    await expect(page.locator('a.app-card')).toHaveCount(1);

    // Search: open /, type query, pick result → detail
    await page.keyboard.press('/');
    const modal = page.locator('search-modal');
    await expect(modal).toHaveAttribute('open', '');
    await page.locator('search-modal >>> input[name="q"]').fill('Spark');
    await page.locator('search-modal >>> .app-card').first().click();
    await expect(page.getByRole('heading', { level: 2 })).toContainText('Spark');

    // Tooltip: focus source tag shows correct data-tip (hover alternative for mobile)
    const badge = page.locator('.tooltip').first();
    await badge.focus();
    await expect(badge).toHaveAttribute('data-tip', /Black Flag Core|Tales Companion/);

    // Offline: set offline and verify list and detail still render
    await context.setOffline(true);
    await page.goto(baseURL! + '/spells');
    await expect(page.locator('a.app-card')).toHaveCountGreaterThan(0);
    const anyCard = page.locator('a.app-card').first();
    await anyCard.click();
    await expect(page.locator('h2')).toBeVisible();

    // Update flow: go online, switch manifest to alt; snackbar, accept, new data visible
    await context.setOffline(false);
    manifestMode = 'alt';
    // Trigger a sync via navigating home
    await page.goto(baseURL!);
    // Snackbar appears
    const toast = page.locator('.toast .alert-info');
    await expect(toast).toBeVisible();
    await expect(toast).toContainText(/New content available|New version available/);
    await toast.getByRole('button', { name: /Reload/i }).click();
    // After reload, search for an item removed by alt index (e.g., Veil), expect zero results
    await page.keyboard.press('/');
    await page.locator('search-modal >>> input[name="q"]').fill('Veil');
    await expect(page.locator('search-modal >>> .app-card')).toHaveCount(0);
    // And Spark still searchable
    await page.locator('search-modal >>> input[name="q"]').fill('Spark');
    await expect(page.locator('search-modal >>> .app-card')).toHaveCount(1);

    // Deep links: open /spells/spark directly offline
    await context.setOffline(true);
    await page.goto(baseURL! + '/spells/spark');
    await expect(page.locator('h2')).toContainText('Spark');
  });
});
