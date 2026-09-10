/**
 * Headed E2E: Admin Manage Users — create, deactivate (+ login blocked), terminate confirm.
 * Light and dark mode screenshots in frontend/qa-visual.
 */
import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182';
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.join(__dirname, '..', 'qa-visual');
fs.mkdirSync(outDir, { recursive: true });

async function login(page, email, password) {
  await page.goto(`${BASE}/login`);
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
}

async function loginStatus(email, password) {
  const res = await fetch(`${API}/api/v1/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  return res.status;
}

async function main() {
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 250,
    args: headless ? [] : ['--start-maximized'],
  });
  const context = await browser.newContext({
    viewport: headless ? { width: 1280, height: 900 } : null,
  });
  const page = await context.newPage();
  const stamp = Date.now();
  const email = `proc.e2e.${stamp}@example.com`;
  const password = 'Proc12345';
  const displayName = `E2E Procurement ${stamp}`;

  try {
    if (!headless) {
      console.log('Opening Chromium — Manage Users flow');
    }

    await login(page, 'admin@example.com', 'admin123');
    await page.evaluate(() => {
      localStorage.setItem('vendors-theme', 'light');
      document.documentElement.setAttribute('data-theme', 'light');
    });
    await page.locator('.sidebar').waitFor({ timeout: 15000 });
    const usersNav = page.locator('.sidebar a.nav-link', { hasText: 'Users' });
    await usersNav.waitFor({ state: 'visible', timeout: 15000 });
    await usersNav.click();
    await page.getByRole('heading', { name: /Manage Users/i }).waitFor({ timeout: 15000 });
    console.log('PASS [nav] Admin sees Users');

    await page.screenshot({ path: path.join(outDir, 'users-light.png'), fullPage: true });

    await page.getByRole('button', { name: /Create User/i }).click();
    await page.locator('.modal.show').waitFor({ state: 'visible' });
    const roleOptions = await page.locator('#user-role option').allTextContents();
    if (roleOptions.some((t) => /admin/i.test(t))) {
      throw new Error(`Admin appeared in role dropdown: ${roleOptions.join(', ')}`);
    }
    await page.locator('#user-display-name').fill(displayName);
    await page.locator('#user-email').fill(email);
    await page.locator('#user-role').selectOption('ProcurementManager');
    await page.locator('#user-password').fill(password);
    await page.locator('.modal.show button[type="submit"]').click();
    await page.locator('.modal.show').waitFor({ state: 'hidden', timeout: 15000 });

    const newRow = page.locator('tr', { hasText: email });
    await newRow.waitFor({ state: 'visible', timeout: 15000 });
    const rowText = (await newRow.innerText()).replace(/\s+/g, ' ');
    if (!/Procurement Manager/i.test(rowText)) throw new Error(`role missing: ${rowText}`);
    if (!/\bActive\b/i.test(rowText)) throw new Error(`Active status missing: ${rowText}`);
    console.log(`PASS [create] ${email} listed as Procurement Manager / Active`);

    await newRow.getByRole('button', { name: 'Deactivate' }).click();
    await newRow.getByRole('button', { name: 'Activate' }).waitFor({ timeout: 15000 });
    if (!/Deactivated/i.test(await newRow.innerText())) {
      throw new Error('row did not show Deactivated after toggle');
    }
    console.log('PASS [deactivate] status is Deactivated');

    const deactivatedLogin = await loginStatus(email, password);
    if (deactivatedLogin !== 401) {
      throw new Error(`expected deactivated login 401, got ${deactivatedLogin}`);
    }
    console.log('PASS [login] deactivated account cannot log in');

    await newRow.getByRole('button', { name: 'Terminate' }).click();
    const confirm = page.locator('.modal.show, .modal.fade.show').filter({
      hasText: /This is permanent/i,
    });
    await confirm.waitFor({ state: 'visible', timeout: 10000 });
    const confirmText = await confirm.innerText();
    if (!confirmText.includes(displayName) || !/never be able to log in again/i.test(confirmText)) {
      throw new Error(`confirm copy mismatch: ${confirmText}`);
    }
    await confirm.getByRole('button', { name: 'Confirm' }).click();
    await newRow.locator('.status-pill', { hasText: 'Terminated' }).waitFor({ timeout: 15000 });
    await newRow.getByText('—').waitFor({ timeout: 5000 });
    console.log('PASS [terminate] confirmation shown; row is Terminated');

    const terminatedLogin = await loginStatus(email, password);
    if (terminatedLogin !== 401) {
      throw new Error(`expected terminated login 401, got ${terminatedLogin}`);
    }
    console.log('PASS [login] terminated account cannot log in');

    await page.getByRole('button', { name: /theme/i }).click();
    await page.waitForTimeout(400);
    const theme = await page.evaluate(() => document.documentElement.getAttribute('data-theme'));
    if (theme !== 'dark') throw new Error(`expected dark theme, got ${theme}`);
    await page.screenshot({ path: path.join(outDir, 'users-dark.png'), fullPage: true });
    console.log('PASS [theme] dark mode screenshot');

    await context.clearCookies();
    const procPage = await context.newPage();
    await login(procPage, 'procurement@example.com', 'proc123');
    const usersLink = procPage.getByRole('link', { name: /^Users$/ });
    if (await usersLink.count()) throw new Error('Procurement user should not see Users nav');
    await procPage.goto(`${BASE}/users`);
    await procPage.waitForURL((url) => !url.pathname.endsWith('/users'), { timeout: 10000 });
    console.log('PASS [gating] non-Admin cannot open Users');

    console.log('\nAll Manage Users checks passed.');
    console.log(`screenshots: ${path.join(outDir, 'users-light.png')}`);
    console.log(`             ${path.join(outDir, 'users-dark.png')}`);
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
