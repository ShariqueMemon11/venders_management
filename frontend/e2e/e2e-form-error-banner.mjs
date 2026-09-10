/**
 * Headed E2E: FormErrorBanner on 400 ProblemDetails (temporary stopgap).
 * Watch Chromium — slowMo so banners are visible.
 * HEADLESS=1 to opt out of headed mode.
 */
import { chromium } from 'playwright';
import path from 'path';
import os from 'os';
import fs from 'fs';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const outDir = path.join(os.tmpdir(), 'form-error-banner-e2e');
fs.mkdirSync(outDir, { recursive: true });

const long = (n) => 'x'.repeat(n);

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function expectBanner(page, label) {
  const banner = page.locator('.modal.show .alert-danger, form .alert-danger, .alert-danger').first();
  await banner.waitFor({ state: 'visible', timeout: 12000 });
  const text = (await banner.innerText()).trim().replace(/\s+/g, ' ');
  console.log(`PASS [${label}] banner: ${text.slice(0, 160)}`);
  return text;
}

async function clickTab(page, name) {
  // Tab buttons render raw keys with CSS text-capitalize (e.g. "contacts")
  await page.locator('.nav-tabs .nav-link').filter({ hasText: new RegExp(`^${name}`, 'i') }).click();
  await page.waitForTimeout(500);
}

async function main() {
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 350,
    args: headless ? [] : ['--start-maximized'],
  });
  const context = await browser.newContext({
    viewport: headless ? { width: 1280, height: 900 } : null,
  });
  const page = await context.newPage();

  const results = {
    createInvalid: false,
    createValid: false,
    editInvalid: false,
    contact: false,
    address: false,
    bank: false,
    contract: false,
    compliance: false,
    performance: false,
    risk: false,
  };

  let vendorUrl = '';
  const stamp = Date.now();

  try {
    if (!headless) {
      console.log('========================================');
      console.log('Opening Chromium HEADED — watch your screen');
      console.log('========================================');
    }

    await login(page, 'procurement@example.com', 'proc123');

    // --- 1) Vendor Create: invalid currency (passes HTML5, fails FV) ---
    console.log('\n1) Vendor Create — invalid currency "US"');
    await page.goto(`${BASE}/vendors/new`);
    await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 15000 });
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill(`Banner Test ${stamp}`);
    await page.locator('label').filter({ hasText: /Preferred Currency/i }).locator('xpath=following-sibling::input').fill('US');
    await page.locator('label').filter({ hasText: /Company Website/i }).locator('xpath=following-sibling::input').fill('not-a-url');
    await page.locator('button[type="submit"]').click();
    await expectBanner(page, 'create-invalid');
    results.createInvalid = true;
    await page.waitForTimeout(1200);

    // --- 2) Vendor Create: valid (happy path) ---
    console.log('\n2) Vendor Create — valid draft');
    await page.goto(`${BASE}/vendors/new`);
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill(`Banner Valid ${stamp}`);
    await page.locator('label').filter({ hasText: /Preferred Currency/i }).locator('xpath=following-sibling::input').fill('USD');
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/vendors\/[0-9a-f-]+$/i, { timeout: 20000 });
    vendorUrl = page.url();
    console.log(`PASS [create-valid] landed ${vendorUrl}`);
    results.createValid = true;
    await page.waitForTimeout(800);

    // --- 3) Vendor Edit: invalid website ---
    console.log('\n3) Vendor Edit — invalid website');
    await page.goto(`${vendorUrl}/edit`);
    await page.getByRole('heading', { name: /Edit/i }).waitFor({ timeout: 15000 }).catch(() => {});
    await page.locator('label').filter({ hasText: /Company Website/i }).locator('xpath=following-sibling::input').fill('notaurl');
    await page.getByRole('button', { name: /Save Changes/i }).click();
    await expectBanner(page, 'edit-invalid');
    results.editInvalid = true;
    await page.waitForTimeout(1000);

    await page.goto(vendorUrl);
    await page.getByRole('heading', { name: /Banner Valid/i }).waitFor({ timeout: 15000 });

    // --- 4) Contact: overlong job title ---
    console.log('\n4) Contact modal — overlong Job Title');
    await clickTab(page, 'contacts');
    await page.getByRole('button', { name: /Add Contact/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /^Name/i }).locator('xpath=following-sibling::input').fill('Test Contact');
    await page.locator('.modal.show input[type="email"]').fill('test@example.com');
    await page.locator('.modal.show label').filter({ hasText: /Job Title/i }).locator('xpath=following-sibling::input').fill(long(210));
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'contact');
    results.contact = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 5) Address: missing state (required by FV, not HTML5) ---
    console.log('\n5) Address modal — missing State/Province');
    await clickTab(page, 'addresses');
    await page.getByRole('button', { name: /Add Address/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Address Line 1/i }).locator('xpath=following-sibling::input').fill('1 Main St');
    await page.locator('.modal.show label').filter({ hasText: /^City/i }).locator('xpath=following-sibling::input').fill('Karachi');
    await page.locator('.modal.show label').filter({ hasText: /^Country/i }).locator('xpath=following-sibling::input').fill('PK');
    // leave State empty on purpose
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'address');
    results.address = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 6) Bank: invalid IBAN ---
    console.log('\n6) Bank modal — invalid IBAN');
    await clickTab(page, 'banking');
    await page.getByRole('button', { name: /Add Account/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Bank Name/i }).locator('xpath=following-sibling::input').fill('Test Bank');
    await page.locator('.modal.show label').filter({ hasText: /Account Name/i }).locator('xpath=following-sibling::input').fill('Acct');
    await page.locator('.modal.show label').filter({ hasText: /Account Number/i }).locator('xpath=following-sibling::input').fill('123456');
    await page.locator('.modal.show label').filter({ hasText: /^IBAN/i }).locator('xpath=following-sibling::input').fill('INVALID');
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'bank');
    results.bank = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 7) Contract: end before start ---
    console.log('\n7) Contract modal — end before start');
    await clickTab(page, 'contracts');
    await page.getByRole('button', { name: /Add Contract/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Contract Number/i }).locator('xpath=following-sibling::input').fill('C-1');
    await page.locator('.modal.show label').filter({ hasText: /^Title/i }).locator('xpath=following-sibling::input').fill('Title');
    const dates = page.locator('.modal.show input[type="date"]');
    await dates.nth(0).fill('2026-06-01');
    await dates.nth(1).fill('2026-01-01');
    await page.locator('.modal.show input[type="number"]').first().fill('100');
    await page.locator('.modal.show label').filter({ hasText: /Currency/i }).locator('xpath=following-sibling::input').fill('USD');
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'contract');
    results.contract = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 8) Compliance: overlong requirement (Risk role) ---
    console.log('\n8) Compliance modal — overlong requirement (login as risk)');
    await login(page, 'risk@example.com', 'risk123');
    await page.goto(vendorUrl);
    await page.getByRole('heading', { name: /Banner Valid/i }).waitFor({ timeout: 15000 });
    await clickTab(page, 'compliance');
    await page.getByRole('button', { name: /Add Compliance Log/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Requirement/i }).locator('xpath=following-sibling::input').fill(long(210));
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'compliance');
    results.compliance = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 9) Performance: overlong period (Procurement role) ---
    console.log('\n9) Performance modal — overlong evaluation period (login as procurement)');
    await login(page, 'procurement@example.com', 'proc123');
    await page.goto(vendorUrl);
    await page.getByRole('heading', { name: /Banner Valid/i }).waitFor({ timeout: 15000 });
    await clickTab(page, 'performance');
    await page.getByRole('button', { name: /Add Performance Review/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Evaluation Period/i }).locator('xpath=following-sibling::input').fill(long(110));
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'performance');
    results.performance = true;
    await page.locator('.modal.show .btn-close, .modal.show button:has-text("Cancel")').first().click();
    await page.waitForTimeout(600);

    // --- 10) Risk: overlong category (Risk role) ---
    console.log('\n10) Risk modal — overlong category (login as risk)');
    await login(page, 'risk@example.com', 'risk123');
    await page.goto(vendorUrl);
    await page.getByRole('heading', { name: /Banner Valid/i }).waitFor({ timeout: 15000 });
    await clickTab(page, 'risk');
    await page.getByRole('button', { name: /Add Risk Log/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    await page.locator('.modal.show label').filter({ hasText: /Risk Category/i }).locator('xpath=following-sibling::input').fill(long(210));
    await page.locator('.modal.show textarea').first().fill('desc');
    await page.locator('.modal.show button[type="submit"]').click();
    await expectBanner(page, 'risk');
    results.risk = true;
    await page.waitForTimeout(1500);

    const allPass = Object.values(results).every(Boolean);
    console.log('\n======== RESULTS ========');
    console.log(JSON.stringify(results, null, 2));
    console.log(allPass ? 'ALL_FORM_BANNER_PASS=True' : 'ALL_FORM_BANNER_PASS=False');
    if (!allPass) process.exitCode = 1;
  } catch (err) {
    console.log(`FAIL: ${err.message}`);
    const shot = path.join(outDir, `failure-${Date.now()}.png`);
    await page.screenshot({ path: shot, fullPage: true }).catch(() => {});
    console.log(`SCREENSHOT=${shot}`);
    console.log(JSON.stringify(results, null, 2));
    process.exitCode = 1;
  } finally {
    await page.waitForTimeout(headless ? 0 : 2000);
    await browser.close();
  }
}

main();
