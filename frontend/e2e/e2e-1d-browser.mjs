/**
 * Browser E2E for Batch 1d — draft create then submit-for-approval (+ Risk approve).
 */
import { chromium } from 'playwright';
import path from 'path';
import os from 'os';
import fs from 'fs';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const legalName = `DraftThenSubmit ${Date.now()}`;
const outDir = path.join(os.tmpdir(), '1d-browser-e2e');
fs.mkdirSync(outDir, { recursive: true });

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function dismissAlertIfAny(page) {
  const ok = page.locator('.modal-footer button').filter({ hasText: /^OK$/i });
  if (await ok.count()) {
    await ok.click();
    await page.waitForTimeout(300);
  }
}

async function main() {
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 250,
    args: headless ? [] : ['--start-maximized'],
  });
  const context = await browser.newContext({
    viewport: headless ? { width: 1280, height: 800 } : null,
  });
  const page = await context.newPage();
  if (!headless) console.log('Opening Chromium (headed) — watch your screen...');
  const result = { createDraft: false, statusDraft: false, submit: false, riskApprove: false };

  try {
    console.log(`LOGIN procurement name=${legalName}`);
    await login(page, 'procurement@example.com', 'proc123');

    await page.goto(`${BASE}/vendors/new`);
    await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 15000 });
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill(legalName);
    await page.locator('button[type="submit"]').click();

    await page.waitForURL(/\/vendors\/[0-9a-f-]+$/i, { timeout: 20000 });
    const vendorUrl = page.url();
    console.log(`LANDED=${vendorUrl}`);
    result.createDraft = true;

    await page.getByRole('heading', { name: legalName }).waitFor({ timeout: 15000 });
    const submitBtn = page.getByRole('button', { name: /Submit for Approval/i });
    await submitBtn.waitFor({ state: 'visible', timeout: 10000 });
    // Submit button only renders for Draft — treat that as proof of draft status in UI
    result.statusDraft = true;
    console.log('PASS_DRAFT_UI=True (Submit for Approval visible)');

    await submitBtn.click();
    await page.getByRole('button', { name: /^Confirm$/i }).click();
    await page.getByRole('button', { name: /^OK$/i }).waitFor({ timeout: 15000 });
    await dismissAlertIfAny(page);

    await page.getByText(/Approval Workflow in Progress/i).waitFor({ timeout: 15000 });
    result.submit = true;
    console.log('PASS_SUBMIT_UI=True');

    // Risk approves current step (SoD: RiskAndCompliance Review)
    await login(page, 'risk@example.com', 'risk123');
    await page.goto(vendorUrl);
    await page.getByText(/Approval Workflow in Progress/i).waitFor({ timeout: 15000 });
    const approve = page.getByRole('button', { name: /^Approve$/i });
    await approve.waitFor({ state: 'visible', timeout: 10000 });
    await approve.click();
    await page.locator('.modal input.form-control').fill('1d UI approve');
    await page.getByRole('button', { name: /^Submit$/i }).click();
    await page.getByRole('button', { name: /^OK$/i }).waitFor({ timeout: 15000 });
    await dismissAlertIfAny(page);
    result.riskApprove = true;
    console.log('PASS_RISK_APPROVE_UI=True');

    console.log(`SUMMARY ${JSON.stringify(result)}`);
    console.log('ALL_1D_BROWSER_PASS=True');
  } catch (err) {
    console.log(`FAIL: ${err.message}`);
    const shot = path.join(outDir, 'failure.png');
    await page.screenshot({ path: shot, fullPage: true }).catch(() => {});
    console.log(`SCREENSHOT=${shot}`);
    console.log(`SUMMARY ${JSON.stringify(result)}`);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
}

main();
