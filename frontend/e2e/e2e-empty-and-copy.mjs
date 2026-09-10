/**
 * Headed check: directory empty-state + three form validation banners.
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const headed = process.env.HEADLESS !== '1';

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 60 : 0 });
const page = await browser.newPage();
const failures = [];

async function login() {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', 'procurement@example.com');
  await page.fill('input[type="password"]', 'proc123');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
}

try {
  await login();

  // Empty filters on Vendor Directory
  await page.goto(`${UI}/vendors/directory`, { waitUntil: 'networkidle' });
  await page.locator('input[placeholder="Legal name, trade name, or number"]').fill('ZZZ-NO-MATCH-FILTER');
  await page.getByRole('button', { name: /Search/i }).click();
  await page.getByText('No vendors match your filters.').waitFor({ timeout: 15000 });
  await page.getByRole('button', { name: 'Clear filters' }).click();
  await page.waitForTimeout(800);
  const gone = await page.getByText('No vendors match your filters.').count();
  if (gone !== 0) failures.push('Clear filters did not restore results');
  else console.log('PASS empty-state + clear filters');

  // 1) Create: invalid currency (HTML5 passes, FV fails)
  await page.goto(`${UI}/vendors/new`, { waitUntil: 'networkidle' });
  await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill('Copy Check Vendor');
  await page.locator('label').filter({ hasText: /Preferred Currency/i }).locator('xpath=following-sibling::input').fill('US');
  await page.getByRole('button', { name: /Save Draft/i }).click();
  const banner1 = page.locator('.alert-danger').first();
  await banner1.waitFor({ timeout: 12000 });
  const t1 = (await banner1.innerText()).replace(/\s+/g, ' ');
  if (!/3-letter ISO|Currency code/i.test(t1)) failures.push(`create currency banner: ${t1}`);
  else console.log('PASS create validation:', t1.slice(0, 120));

  // 2) Create: empty legal name via JS submit (bypass HTML5)
  await page.goto(`${UI}/vendors/new`, { waitUntil: 'networkidle' });
  await page.evaluate(() => document.querySelector('form')?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })));
  const banner2 = page.locator('.alert-danger').first();
  await banner2.waitFor({ timeout: 8000 });
  const t2 = (await banner2.innerText()).replace(/\s+/g, ' ');
  if (!/Legal name is required/i.test(t2)) failures.push(`legal name banner: ${t2}`);
  else console.log('PASS legal name:', t2.slice(0, 120));

  // 3) Create: invalid website
  await page.goto(`${UI}/vendors/new`, { waitUntil: 'networkidle' });
  await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input').fill('Copy Check Vendor 2');
  await page.locator('label').filter({ hasText: /Preferred Currency/i }).locator('xpath=following-sibling::input').fill('USD');
  await page.locator('label').filter({ hasText: /Company Website/i }).locator('xpath=following-sibling::input').fill('not-a-url');
  await page.getByRole('button', { name: /Save Draft/i }).click();
  const banner3 = page.locator('.alert-danger').first();
  await banner3.waitFor({ timeout: 12000 });
  const t3 = (await banner3.innerText()).replace(/\s+/g, ' ');
  if (!/Website must be a valid HTTP/i.test(t3)) failures.push(`website banner: ${t3}`);
  else console.log('PASS website validation:', t3.slice(0, 120));

  console.log(JSON.stringify({ failures, ok: failures.length === 0 }, null, 2));
  if (failures.length) process.exitCode = 1;
  if (headed) await page.waitForTimeout(1000);
} finally {
  await browser.close();
}
