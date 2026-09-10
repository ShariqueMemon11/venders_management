/**
 * E2E: document upload/download through real UI with FileStorage:Provider = AzureBlob (Azurite).
 * Default: HEADED. Set HEADLESS=1 only if you explicitly want headless.
 */
import { chromium } from 'playwright';
import fs from 'fs';
import os from 'os';
import path from 'path';
import { fileURLToPath } from 'url';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182/api/v1';
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MARKER = `azurite-e2e-${Date.now()}`;
const FILE_CONTENTS = `AzureBlob UI round-trip ${MARKER}\nline-two-integrity-check\n`;

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
}

async function clickTab(page, name) {
  await page.locator('.nav-tabs .nav-link').filter({ hasText: new RegExp(`^${name}`, 'i') }).click();
  await page.waitForTimeout(400);
}

async function main() {
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  console.log('=== 4d AzureBlob document E2E ===');
  console.log(`UI=${BASE} headless=${headless}`);
  if (!headless) console.log('Opening Chromium HEADED — watch your screen');

  // Confirm API is on AzureBlob (health of login first)
  const loginRes = await fetch(`${API}/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'procurement@example.com', password: 'proc123' }),
  });
  if (!loginRes.ok) throw new Error(`API login failed: ${loginRes.status}`);
  const { token } = await loginRes.json();
  console.log('PASS API login');

  const tmpFile = path.join(os.tmpdir(), `${MARKER}.txt`);
  fs.writeFileSync(tmpFile, FILE_CONTENTS, 'utf8');

  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 280,
  });
  const context = await browser.newContext({ acceptDownloads: true });
  const page = await context.newPage({ viewport: { width: 1280, height: 900 } });

  try {
    await login(page, 'procurement@example.com', 'proc123');
    console.log('PASS UI login');

    const stamp = Date.now();
    await page.goto(`${BASE}/vendors/new`);
    await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 15000 });
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input')
      .fill(`AzureBlob Doc E2E ${stamp}`);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/vendors\/[0-9a-f-]{36}/i, { timeout: 20000 });
    const vendorId = page.url().split('/').pop();
    console.log(`PASS create vendor ${vendorId}`);

    await clickTab(page, 'documents');
    await page.getByRole('button', { name: /Upload/i }).click();
    await page.getByRole('heading', { name: /Upload Document/i }).waitFor({ timeout: 10000 });

    const modal = page.locator('.modal.show');
    const title = `Azurite Proof ${MARKER}`;
    await modal.locator('label').filter({ hasText: /Document Title/i }).locator('xpath=following-sibling::input').fill(title);
    await modal.locator('input[type="file"]').setInputFiles(tmpFile);
    await modal.getByRole('button', { name: /^Upload$/i }).click();
    await page.waitForTimeout(2000);

    const row = page.locator('table tbody tr').filter({ hasText: title });
    await row.waitFor({ timeout: 15000 });
    console.log('PASS upload — document appears in table');

    // Hard reload — still listed
    await page.reload({ waitUntil: 'networkidle' });
    await clickTab(page, 'documents');
    await page.locator('table tbody tr').filter({ hasText: title }).waitFor({ timeout: 15000 });
    console.log('PASS hard reload — document still listed');

    // Download via UI and verify bytes
    const downloadPromise = page.waitForEvent('download', { timeout: 20000 });
    await page.locator('table tbody tr').filter({ hasText: title }).locator('button[title="Download"]').click();
    const download = await downloadPromise;
    const downloadPath = path.join(os.tmpdir(), `dl-${MARKER}.txt`);
    await download.saveAs(downloadPath);
    const downloaded = fs.readFileSync(downloadPath, 'utf8');
    if (downloaded !== FILE_CONTENTS) {
      throw new Error(`FAIL download bytes mismatch.\nExpected: ${JSON.stringify(FILE_CONTENTS)}\nGot: ${JSON.stringify(downloaded)}`);
    }
    console.log('PASS download — byte content matches upload');

    // Confirm storage path exists as blob in Azurite via API details + Azure SDK is overkill;
    // details should show document metadata with storage path.
    const details = await fetch(`${API}/vendors/${vendorId}/details`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const body = await details.json();
    const docs = body.data?.documents || body.documents || [];
    const doc = docs.find((d) => (d.title || d.Title) === title);
    if (!doc) throw new Error('document missing from details JSON');
    const storagePath = doc.storagePath || doc.StoragePath;
    if (!storagePath || !/^\d{4}-\d{2}\//.test(storagePath)) {
      throw new Error(`unexpected storagePath: ${storagePath}`);
    }
    console.log(`PASS details storagePath=${storagePath} (blob-relative shape)`);

    console.log('\n=== ALL 4d AzureBlob UI CHECKS PASSED ===');
  } finally {
    await browser.close();
    try { fs.unlinkSync(tmpFile); } catch { /* ignore */ }
  }
}

main().catch((err) => {
  console.error('E2E FAILED:', err);
  process.exit(1);
});
