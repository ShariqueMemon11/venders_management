/**
 * Browser E2E for Batch 1c — exercises the real VendorDetails UI
 * (Axios+Bearer download button + verify/reject), not the API layer alone.
 *
 * Usage:
 *   node e2e/e2e-1c-browser.mjs <vendorId> <marker> <approveDocTitle> <rejectDocTitle> <downloadDocTitle>
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import os from 'os';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const [
  , ,
  vendorId = '8c82aca5-111d-418b-8881-2656877226a9',
  marker = 'UI-BROWSER-DOWNLOAD-MARKER-20260826153418',
  approveTitle = 'UI Browser Approve Doc',
  rejectTitle = 'UI Browser Reject Doc',
  downloadTitle = 'UI Browser Download Doc',
] = process.argv;

const outDir = path.join(os.tmpdir(), '1c-browser-e2e');
fs.mkdirSync(outDir, { recursive: true });

function log(msg) {
  console.log(msg);
}

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function openDocumentsTab(page, id) {
  await page.goto(`${BASE}/vendors/${id}`);
  await page.waitForSelector('text=Verification Documents, text=Overview, text=Contacts', { timeout: 20000 }).catch(() => {});
  // Prefer clicking the Documents tab
  const tab = page.locator('button, a, .nav-link').filter({ hasText: /^Documents$/i }).first();
  if (await tab.count()) {
    await tab.click();
  } else {
    // fallback: some UIs use role=tab
    await page.getByRole('button', { name: /documents/i }).first().click();
  }
  await page.waitForSelector('text=Verification Documents', { timeout: 15000 });
}

async function rowForTitle(page, title) {
  return page.locator('table tbody tr').filter({ hasText: title }).first();
}

async function main() {
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 200,
    args: headless ? [] : ['--start-maximized'],
  });
  const context = await browser.newContext({
    acceptDownloads: true,
    viewport: headless ? { width: 1280, height: 800 } : null,
  });
  const page = await context.newPage();
  if (!headless) console.log('Opening Chromium (headed) — watch your screen...');
  const result = { download: false, content: false, approve: false, reject: false };

  try {
    // --- 1 & 2: Risk user clicks download button, open file ---
    log('LOGIN risk@example.com');
    await login(page, 'risk@example.com', 'risk123');
    await openDocumentsTab(page, vendorId);

    const dlRow = await rowForTitle(page, downloadTitle);
    await dlRow.waitFor({ state: 'visible', timeout: 15000 });
    const dlBtn = dlRow.locator('button[title="Download"]');
    if (!(await dlBtn.count())) {
      throw new Error('Download button not found in UI (permission gate or wrong row)');
    }

    const downloadPromise = page.waitForEvent('download', { timeout: 20000 });
    await dlBtn.click();
    const download = await downloadPromise;
    const suggested = download.suggestedFilename();
    const savePath = path.join(outDir, suggested || 'downloaded.txt');
    await download.saveAs(savePath);
    const stat = fs.statSync(savePath);
    const content = fs.readFileSync(savePath, 'utf8');
    log(`DOWNLOAD_FILE path=${savePath} bytes=${stat.size} name=${suggested}`);
    log(`DOWNLOAD_CONTENT=${JSON.stringify(content)}`);
    result.download = stat.size > 0;
    result.content = content.includes(marker);
    if (!result.content) {
      throw new Error(`Downloaded file missing marker. Got: ${content.slice(0, 200)}`);
    }
    log('PASS_DOWNLOAD_UI=True');
    log('PASS_CONTENT=True');

    // --- 3a: Approve button updates badge without full navigation refresh ---
    const approveRow = await rowForTitle(page, approveTitle);
    await approveRow.waitFor({ state: 'visible', timeout: 10000 });
    const approveBtn = approveRow.locator('button[title="Approve / Verify"]');
    if (!(await approveBtn.count())) {
      throw new Error('Approve button not visible for Risk on pending doc');
    }
    await approveBtn.click();
    await page.waitForSelector('.modal.show, .modal.d-block', { timeout: 10000 });
    await page.fill('.modal input.form-control', 'UI approve comment');
    await page.locator('.modal-footer button.btn-primary').filter({ hasText: /submit/i }).click();
    // Wait for badge Verified in that row (loadVendor refresh in-place)
    await approveRow.locator('.badge').filter({ hasText: /Verified/i }).waitFor({ state: 'visible', timeout: 15000 });
    // Confirm approve buttons gone for that row
    const approveStill = await approveRow.locator('button[title="Approve / Verify"]').count();
    log(`APPROVE_BADGE=${await approveRow.locator('.badge').first().innerText()} approveBtnsLeft=${approveStill}`);
    result.approve = approveStill === 0;
    if (!result.approve) throw new Error('Approve UI did not update badge/actions');
    log('PASS_APPROVE_UI=True');

    // --- 3b: Reject button ---
    const rejectRow = await rowForTitle(page, rejectTitle);
    await rejectRow.waitFor({ state: 'visible', timeout: 10000 });
    const rejectBtn = rejectRow.locator('button[title="Reject"]');
    await rejectBtn.click();
    await page.waitForSelector('.modal.show, .modal.d-block', { timeout: 10000 });
    await page.fill('.modal input.form-control', 'UI reject comment');
    await page.locator('.modal-footer button.btn-primary').filter({ hasText: /submit/i }).click();
    await rejectRow.locator('.badge').filter({ hasText: /Rejected/i }).waitFor({ state: 'visible', timeout: 15000 });
    const rejectStill = await rejectRow.locator('button[title="Reject"]').count();
    log(`REJECT_BADGE=${await rejectRow.locator('.badge').first().innerText()} rejectBtnsLeft=${rejectStill}`);
    result.reject = rejectStill === 0;
    if (!result.reject) throw new Error('Reject UI did not update badge/actions');
    log('PASS_REJECT_UI=True');

    log(`SUMMARY ${JSON.stringify(result)}`);
    log('ALL_1C_BROWSER_PASS=True');
  } catch (err) {
    log(`FAIL: ${err.message}`);
    const shot = path.join(outDir, 'failure.png');
    await page.screenshot({ path: shot, fullPage: true }).catch(() => {});
    log(`SCREENSHOT=${shot}`);
    log(`SUMMARY ${JSON.stringify(result)}`);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
}

main();
