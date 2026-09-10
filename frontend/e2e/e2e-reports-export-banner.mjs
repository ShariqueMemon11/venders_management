/**
 * Headed: Reports export error uses inline banner, not alert().
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const headed = process.env.HEADLESS !== '1';

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 50 : 0 });
const page = await browser.newPage();
const failures = [];
const dialogs = [];
page.on('dialog', async (d) => {
  dialogs.push(d.message());
  await d.dismiss();
});

async function login() {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', 'procurement@example.com');
  await page.fill('input[type="password"]', 'proc123');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
}

try {
  await login();
  await page.goto(`${UI}/reports`, { waitUntil: 'networkidle' });

  // 1) API JSON error message
  await page.route('**/vendors/reports/**', async (route) => {
    await route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Export is temporarily unavailable.' }),
    });
  });
  await page.getByRole('button', { name: /Download as Excel/i }).first().click();
  const banner = page.locator('.alert-danger[role="alert"]');
  await banner.waitFor({ timeout: 8000 });
  const t1 = (await banner.innerText()).replace(/\s+/g, ' ');
  if (!/Export is temporarily unavailable/i.test(t1)) failures.push(`json banner: ${t1}`);
  else console.log('PASS API message in banner:', t1);

  // 2) Blocked request → fallback copy
  await page.unroute('**/vendors/reports/**');
  await page.route('**/vendors/reports/**', (route) => route.abort('failed'));
  await page.getByRole('button', { name: /Download as CSV/i }).first().click();
  await page.waitForTimeout(800);
  const t2 = (await banner.innerText()).replace(/\s+/g, ' ');
  if (!/Report export failed\. Try again/i.test(t2)) failures.push(`fallback banner: ${t2}`);
  else console.log('PASS fallback banner:', t2);

  if (dialogs.length) failures.push(`native alert still fired: ${dialogs.join(' | ')}`);
  else console.log('PASS no native alert()');

  console.log(JSON.stringify({ failures, ok: failures.length === 0 }, null, 2));
  if (failures.length) process.exitCode = 1;
  if (headed) await page.waitForTimeout(1200);
} finally {
  await browser.close();
}
