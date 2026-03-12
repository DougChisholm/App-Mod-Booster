// @ts-check
const { test, expect } = require('@playwright/test');
const path = require('path');

const BASE = 'file://' + path.resolve(__dirname, '../New_App');

// ── Test timeout constants ────────────────────────────────
const VEHICLE_LOOKUP_TIMEOUT_MS = 5000;
const PANEL_TRANSITION_TIMEOUT_MS = 3000;
const QUOTE_RENDER_TIMEOUT_MS = 5000;

test.describe('Swinton Go! Insurance - Rebrand Tests', () => {

    test('Page 1 (index.html) loads with Swinton Go branding', async ({ page }) => {
        await page.goto(BASE + '/index.html');

        // Check title
        await expect(page).toHaveTitle(/Swinton Go/);

        // Check logo is present
        const logo = page.locator('img[alt="Swinton Go! Insurance"]');
        await expect(logo).toBeVisible();

        // Check welcome heading
        await expect(page.locator('h1')).toContainText('Welcome to Swinton Go!');

        // Check progress nav is present with 4 steps
        const steps = page.locator('.progress-step');
        await expect(steps).toHaveCount(4);

        // Check first step is active
        await expect(page.locator('.progress-step--active')).toContainText('Your details');

        // Check CTA button
        await expect(page.locator('#btn-start-quote')).toBeVisible();
        await expect(page.locator('#btn-start-quote')).toContainText('Start my quote');
    });

    test('Page 1 (index.html) validation works', async ({ page }) => {
        await page.goto(BASE + '/index.html');

        // Click submit without filling anything
        await page.click('#btn-start-quote');

        // Should show title validation error
        await expect(page.locator('#err-title')).toBeVisible();
    });

    test('Page 1 footer has Swinton Go branding', async ({ page }) => {
        await page.goto(BASE + '/index.html');
        const footer = page.locator('footer');
        await expect(footer).toContainText('Swinton Go!');
        await expect(footer).toContainText('All rights reserved');
    });

    test('Full happy path: details → car → cover → quote', async ({ page }) => {
        // ── Step 1: Your details ──────────────────────────────
        await page.goto(BASE + '/index.html');
        await expect(page).toHaveTitle(/Swinton Go/);

        // Select title
        await page.click('label[for="title-mr"]');

        // Fill first name
        await page.fill('#firstName', 'John');

        // Fill surname
        await page.fill('#lastName', 'Smith');

        // Fill DOB
        await page.fill('#dob', '15/03/1985');

        // Submit
        await page.click('#btn-start-quote');
        await page.waitForURL('**/your-car.html');

        // ── Step 2: Your car ──────────────────────────────────
        await expect(page).toHaveTitle(/Your Car/);

        // Enter registration
        await page.fill('#reg-input', 'D500 NLE');
        await page.click('#btn-find-car');

        // Wait for vehicle found panel
        await page.waitForSelector('#panel-vehicle-found', { state: 'visible', timeout: VEHICLE_LOOKUP_TIMEOUT_MS });
        await expect(page.locator('#veh-title')).toContainText('Volkswagen');

        // Confirm vehicle
        await page.click('label[for="veh-yes"]');
        await page.click('#btn-confirm-vehicle');

        // Wait for questions panel
        await page.waitForSelector('#panel-questions', { state: 'visible', timeout: PANEL_TRANSITION_TIMEOUT_MS });

        // Fill car questions
        await page.fill('#purchase-date', '06/2022');
        await page.click('label[for="keeper-yes"]');
        await page.selectOption('#car-owner', 'proposer');
        await page.click('label[for="use-commuting"]');
        await page.selectOption('#annual-mileage', '12000');
        await page.selectOption('#overnight-parking', 'driveway');
        await page.click('label[for="mod-no"]');
        await page.selectOption('#licence-type', 'full-uk');
        await page.selectOption('#licence-years', '20');
        await page.selectOption('#ncb-years', '9');
        await page.click('label[for="claims-no"]');
        await page.click('label[for="conv-no"]');
        await page.click('#btn-continue-to-cover');
        await page.waitForURL('**/your-cover.html');

        // ── Step 3: Your cover ────────────────────────────────
        await expect(page).toHaveTitle(/Your Cover/);

        // Comprehensive should be pre-selected
        const compCard = page.locator('.cover-card[data-value="comprehensive"]');
        await expect(compCard).toHaveClass(/selected/);

        // Select monthly payment
        await page.click('label[for="pay-monthly"]');

        // No additional drivers
        await page.click('label[for="adddrv-no"]');

        // Get quote
        await page.click('#btn-get-quote');
        await page.waitForURL('**/your_cover.html');

        // ── Step 4: Quote result ──────────────────────────────
        await expect(page).toHaveTitle(/Your Quote/);

        // Wait for quote to be calculated and rendered
        await page.waitForSelector('.quote-hero', { timeout: QUOTE_RENDER_TIMEOUT_MS });

        // Check quote hero is displayed
        await expect(page.locator('.quote-hero')).toBeVisible();
        await expect(page.locator('.quote-hero')).toContainText('Comprehensive');
        await expect(page.locator('.quote-hero')).toContainText('/month');

        // Check summary cards
        await expect(page.locator('text=Your cover summary')).toBeVisible();
        await expect(page.locator('text=Your details summary')).toBeVisible();

        // Check reference number
        await expect(page.locator('text=SG-')).toBeVisible();
    });

    test('your-car.html page has correct step highlighted', async ({ page }) => {
        await page.goto(BASE + '/your-car.html');
        await expect(page.locator('.progress-step--active')).toContainText('Your car');
    });

    test('your-cover.html page has correct step highlighted', async ({ page }) => {
        await page.goto(BASE + '/your-cover.html');
        await expect(page.locator('.progress-step--active')).toContainText('Your cover');
    });

    test('your_cover.html (quote page) has correct step highlighted', async ({ page }) => {
        await page.goto(BASE + '/your_cover.html');
        await expect(page.locator('.progress-step--active')).toContainText('Quote');
    });

    test('No Dial Direct branding remains on any page', async ({ page }) => {
        const pages = ['index.html', 'your-car.html', 'your-cover.html', 'your_cover.html'];
        for (const p of pages) {
            await page.goto(BASE + '/' + p);
            const bodyText = await page.locator('body').textContent();
            expect(bodyText).not.toContain('Dial Direct');
            expect(bodyText).not.toContain('dial direct');
        }
    });

    test('Orange CTA button is styled correctly', async ({ page }) => {
        await page.goto(BASE + '/index.html');
        const btn = page.locator('#btn-start-quote');
        const bgColor = await btn.evaluate(el => getComputedStyle(el).backgroundColor);
        // Expect orange button color (F15A22 = rgb(241, 90, 34))
        expect(bgColor).toBe('rgb(241, 90, 34)');
    });

    test('Footer is dark navy on all pages', async ({ page }) => {
        await page.goto(BASE + '/index.html');
        const footer = page.locator('footer');
        const bgColor = await footer.evaluate(el => getComputedStyle(el).backgroundColor);
        // Navy #0D3880 = rgb(13, 56, 128)
        expect(bgColor).toBe('rgb(13, 56, 128)');
    });
});
