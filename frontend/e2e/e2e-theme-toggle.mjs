/**
 * UI-1 — headed theme toggle check (data-theme light|dark + token flip + persist).
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182';
const headed = process.env.HEADLESS !== '1';

const login = await fetch(`${API}/api/v1/Auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'procurement@example.com', password: 'proc123' }),
});
if (!login.ok) throw new Error(`login ${login.status}`);
const { token, user } = await login.json();

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 60 : 0 });
const page = await browser.newPage();

try {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(
    ({ token, user }) => {
      sessionStorage.setItem('auth_token', token);
      sessionStorage.setItem('user', JSON.stringify(user));
      localStorage.removeItem('vendors-theme');
    },
    { token, user }
  );
  await page.goto(`${UI}/`, { waitUntil: 'networkidle' });
  await page.waitForSelector('button[aria-label*="theme"]');

  const before = await page.evaluate(() => ({
    theme: document.documentElement.getAttribute('data-theme'),
    bg: getComputedStyle(document.documentElement).getPropertyValue('--bg').trim(),
    bodyBg: getComputedStyle(document.body).backgroundColor,
    mainBg: getComputedStyle(document.querySelector('.main-content')).backgroundColor,
  }));

  await page.click('button[aria-label*="theme"]');
  await page.waitForTimeout(250);

  const after = await page.evaluate(() => ({
    theme: document.documentElement.getAttribute('data-theme'),
    bg: getComputedStyle(document.documentElement).getPropertyValue('--bg').trim(),
    bodyBg: getComputedStyle(document.body).backgroundColor,
    mainBg: getComputedStyle(document.querySelector('.main-content')).backgroundColor,
    stored: localStorage.getItem('vendors-theme'),
  }));

  await page.reload({ waitUntil: 'networkidle' });
  const persisted = await page.evaluate(() => ({
    theme: document.documentElement.getAttribute('data-theme'),
    stored: localStorage.getItem('vendors-theme'),
  }));

  const ok =
    before.theme === 'light' &&
    before.bg.toLowerCase() === '#f7f8fa' &&
    after.theme === 'dark' &&
    after.bg.toLowerCase() === '#0f1115' &&
    after.stored === 'dark' &&
    before.bodyBg !== after.bodyBg &&
    before.mainBg !== after.mainBg &&
    persisted.theme === 'dark' &&
    persisted.stored === 'dark';

  console.log(JSON.stringify({ before, after, persisted, ok }, null, 2));
  if (!ok) process.exitCode = 1;
  if (headed) await page.waitForTimeout(1200);
} finally {
  await browser.close();
}
