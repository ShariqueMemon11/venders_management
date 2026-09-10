/**
 * Visible UI demo — Chromium opens on your machine (headed, slowMo).
 * Watch: login → create Draft → add contact → Submit → Risk Approve
 */
import { chromium } from 'playwright';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const PAUSE_MS = Number(process.env.UI_PAUSE_MS || 1400);

async function pause(page, ms = PAUSE_MS) {
  await page.waitForTimeout(ms);
}

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await pause(page);
  await page.fill('input[type="email"]', '');
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', '');
  await page.fill('input[type="password"]', password);
  await pause(page, 700);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
  await pause(page);
}

async function main() {
  console.log('Opening Chromium (headed) — watch your screen...');
  const browser = await chromium.launch({
    headless: false,
    slowMo: 300,
    args: ['--start-maximized'],
  });
  const context = await browser.newContext({
    acceptDownloads: true,
    viewport: null,
  });
  const page = await context.newPage();
  const legalName = `Visible Demo ${Date.now()}`;

  try {
    console.log('1) Login as procurement');
    await login(page, 'procurement@example.com', 'proc123');

    console.log('2) Create Draft vendor');
    await page.goto(`${BASE}/vendors/new`);
    await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 15000 });
    await pause(page);
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill(legalName);
    await pause(page);
    await page.getByRole('button', { name: /Save Draft/i }).click();
    await page.waitForURL(/\/vendors\/[0-9a-f-]+$/i, { timeout: 20000 });
    console.log(`   Landed: ${page.url()}`);
    await page.getByRole('heading', { name: legalName }).waitFor({ timeout: 15000 });
    await pause(page, 2000);

    console.log('3) Contacts tab → Add Contact');
    await page.getByRole('button', { name: /^contacts$/i }).click();
    await pause(page);
    await page.getByRole('button', { name: /Add Contact/i }).click();
    await page.getByRole('heading', { name: /Add New Contact/i }).waitFor({ timeout: 10000 });
    await pause(page);
    await page.locator('.modal.show label').filter({ hasText: /^Name/ }).locator('xpath=following-sibling::input').fill('Demo Contact');
    await page.locator('.modal.show label').filter({ hasText: /^Email/ }).locator('xpath=following-sibling::input').fill('demo@example.com');
    await pause(page);
    await page.getByRole('button', { name: /Save Contact/i }).click();
    await page.getByText('Demo Contact').waitFor({ timeout: 15000 });
    await pause(page, 1500);

    console.log('4) Submit for Approval');
    await page.getByRole('button', { name: /Submit for Approval/i }).click();
    await pause(page);
    await page.getByRole('button', { name: /^Confirm$/i }).click();
    await page.getByRole('button', { name: /^OK$/i }).waitFor({ timeout: 15000 });
    await pause(page);
    await page.getByRole('button', { name: /^OK$/i }).click();
    await page.getByText(/Approval Workflow in Progress/i).waitFor({ timeout: 15000 });
    await pause(page, 2000);

    console.log('5) Login as Risk → Approve');
    const vendorUrl = page.url();
    await login(page, 'risk@example.com', 'risk123');
    await page.goto(vendorUrl);
    await page.getByText(/Approval Workflow in Progress/i).waitFor({ timeout: 15000 });
    await pause(page, 1500);
    await page.getByRole('button', { name: /^Approve$/i }).click();
    await pause(page);
    await page.locator('.modal.show input.form-control, .modal.d-block input.form-control').fill('Looks good (visible demo)');
    await page.getByRole('button', { name: /^Submit$/i }).click();
    await page.getByRole('button', { name: /^OK$/i }).waitFor({ timeout: 15000 });
    await pause(page);
    await page.getByRole('button', { name: /^OK$/i }).click();
    await pause(page, 3000);

    console.log('Done — leaving browser open 4s then closing');
    await pause(page, 4000);
    console.log('VISIBLE_DEMO_PASS=True');
  } catch (err) {
    console.error('FAIL:', err.message);
    await pause(page, 6000);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
}

main();
