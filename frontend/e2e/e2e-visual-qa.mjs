/**
 * Visual QA: 16 screenshots (8 screens × light/dark) + 768px overlap checks.
 */
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const headed = process.env.HEADLESS !== '1';
const outDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '../qa-visual');
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 30 : 0 });
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
const notes = [];

async function setTheme(theme) {
  await page.evaluate((t) => {
    localStorage.setItem('vendors-theme', t);
    document.documentElement.setAttribute('data-theme', t);
  }, theme);
  await page.waitForTimeout(250);
}

async function shot(name) {
  const file = path.join(outDir, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log('saved', name);
}

async function login() {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', 'procurement@example.com');
  await page.fill('input[type="password"]', 'proc123');
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
}

async function captureTheme(theme) {
  await page.goto(`${UI}/login`, { waitUntil: 'networkidle' });
  await setTheme(theme);
  await shot(`${theme}-01-login`);

  await login();
  await setTheme(theme);

  await page.goto(`${UI}/`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(600);
  await shot(`${theme}-02-dashboard`);

  await page.goto(`${UI}/vendors`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(500);
  await shot(`${theme}-03-suppliers`);

  await page.goto(`${UI}/vendors/directory`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(500);
  await shot(`${theme}-04-directory`);

  const details = page.locator('a.btn-outline-primary').filter({ hasText: /Details/i }).first();
  await details.click();
  await page.waitForURL(/\/vendors\/[0-9a-f-]+$/i, { timeout: 15000 });
  await page.locator('.workflow-stepper').waitFor({ timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(500);
  await shot(`${theme}-05-vendor-details`);

  await page.goto(`${UI}/vendors/new`, { waitUntil: 'networkidle' });
  await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 10000 });
  await shot(`${theme}-06-vendor-create`);

  await page.goto(`${UI}/notifications`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(400);
  await shot(`${theme}-07-notifications`);

  await page.goto(`${UI}/reports`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(400);
  await shot(`${theme}-08-reports`);

  await page.evaluate(() => {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('user');
  });
}

try {
  await captureTheme('light');
  await captureTheme('dark');

  // 768px pass (dark, logged in)
  await login();
  await setTheme('dark');
  await page.setViewportSize({ width: 768, height: 900 });

  for (const [route, label] of [
    ['/', 'dashboard'],
    ['/vendors', 'suppliers'],
    ['/vendors/directory', 'directory'],
  ]) {
    await page.goto(`${UI}${route}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(400);
    const box = await page.locator('.sidebar').boundingBox();
    const main = await page.locator('.main-content').boundingBox();
    const overlap = box && main ? (box.x + box.width) > main.x + 2 : false;
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 2);
    notes.push({ label, sidebarWidth: box?.width, mainX: main?.x, overlap, overflow });
    await page.screenshot({ path: path.join(outDir, `768-${label}.png`), fullPage: true });
  }

  await page.goto(`${UI}/vendors/directory`, { waitUntil: 'networkidle' });
  await page.locator('a.btn-outline-primary').filter({ hasText: /Details/i }).first().click();
  await page.waitForURL(/\/vendors\//, { timeout: 15000 });
  await page.locator('.workflow-stepper').waitFor({ timeout: 8000 }).catch(() => {});
  await page.screenshot({ path: path.join(outDir, '768-vendor-details.png'), fullPage: true });

  // Focus ring check
  await page.goto(`${UI}/`, { waitUntil: 'networkidle' });
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  const focused = await page.evaluate(() => {
    const el = document.activeElement;
    if (!el) return null;
    const cs = getComputedStyle(el);
    return { tag: el.tagName, className: el.className, outline: cs.outline, outlineColor: cs.outlineColor };
  });
  notes.push({ focusSample: focused });
  await page.screenshot({ path: path.join(outDir, '768-focus-tab.png') });

  console.log(JSON.stringify({ notes, outDir }, null, 2));
} finally {
  if (headed) await page.waitForTimeout(800);
  await browser.close();
}
