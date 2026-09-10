/**
 * UI-2 — headed check: every screen uses token surfaces in light and dark.
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182';
const headed = process.env.HEADLESS !== '1';

function parseRgb(s) {
  const m = String(s).match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
  if (!m) return s;
  return `${m[1]},${m[2]},${m[3]}`;
}

const loginRes = await fetch(`${API}/api/v1/Auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'procurement@example.com', password: 'proc123' }),
});
if (!loginRes.ok) throw new Error(`login ${loginRes.status}`);
const { token, user } = await loginRes.json();

const vendorsRes = await fetch(`${API}/api/v1/vendors?pageNumber=1&pageSize=1`, {
  headers: { Authorization: `Bearer ${token}` },
});
const vendorsJson = await vendorsRes.json();
const vendorList = Array.isArray(vendorsJson?.data)
  ? vendorsJson.data
  : vendorsJson?.data?.items || [];
const vendorId = vendorList[0]?.id;

const routes = [
  { name: 'Login', path: '/login', authed: false },
  { name: 'Dashboard', path: '/', authed: true },
  { name: 'Suppliers list', path: '/vendors', authed: true },
  { name: 'Vendor Directory', path: '/vendors/directory', authed: true },
  { name: 'Vendor Create', path: '/vendors/new', authed: true },
  { name: 'Notifications', path: '/notifications', authed: true },
  { name: 'Reports', path: '/reports', authed: true },
];
if (vendorId) {
  routes.push({ name: 'Vendor Details', path: `/vendors/${vendorId}`, authed: true });
  routes.push({ name: 'Vendor Edit', path: `/vendors/${vendorId}/edit`, authed: true });
}

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 40 : 0 });
const page = await browser.newPage();
const failures = [];
const snapshots = [];

async function applyAuthAndTheme(theme) {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(
    ({ token, user, theme }) => {
      sessionStorage.setItem('auth_token', token);
      sessionStorage.setItem('user', JSON.stringify(user));
      localStorage.setItem('vendors-theme', theme);
      document.documentElement.setAttribute('data-theme', theme);
    },
    { token, user, theme }
  );
}

async function sample(name, theme) {
  await page.waitForTimeout(400);
  return page.evaluate(({ name, theme }) => {
    const css = getComputedStyle(document.documentElement);
    const tokenBg = css.getPropertyValue('--bg').trim();
    const tokenSurface = css.getPropertyValue('--surface').trim();
    const tokenText = css.getPropertyValue('--text-primary').trim();
    const bodyBg = getComputedStyle(document.body).backgroundColor;
    const card = document.querySelector('.card');
    const table = document.querySelector('.table');
    const heading = document.querySelector('.main-content h2, .card h4, form h4');
    const cell = document.querySelector('.main-content td, .table td, .table th');
    const cardBg = card ? getComputedStyle(card).backgroundColor : null;
    const tableBg = table ? getComputedStyle(table).backgroundColor : null;
    const cellBg = cell ? getComputedStyle(cell).backgroundColor : null;
    const headingColor = heading ? getComputedStyle(heading).color : null;
    const htmlTheme = document.documentElement.getAttribute('data-theme');
    const white = (c) => c === 'rgb(255, 255, 255)' || c === 'rgba(255, 255, 255, 1)';
    return {
      name,
      theme,
      htmlTheme,
      tokenBg,
      tokenSurface,
      tokenText,
      bodyBg,
      cardBg,
      tableBg,
      cellBg,
      headingColor,
      darkCardIsWhite: theme === 'dark' && card && white(cardBg),
      darkTableIsWhite: theme === 'dark' && table && white(tableBg),
      darkBodyIsWhite: theme === 'dark' && white(bodyBg),
    };
  }, { name, theme });
}

try {
  for (const theme of ['light', 'dark']) {
    await applyAuthAndTheme(theme);
    for (const route of routes) {
      const url = `${UI}${route.path}`;
      await page.goto(url, { waitUntil: 'networkidle' });
      if (route.authed) {
        await page.waitForSelector('.main-content, .card, h2', { timeout: 15000 });
      } else {
        await page.waitForSelector('form, .card', { timeout: 15000 });
      }
      const row = await sample(route.name, theme);
      snapshots.push(row);
      if (row.htmlTheme !== theme) failures.push(`${route.name} ${theme}: data-theme=${row.htmlTheme}`);
      if (row.darkCardIsWhite) failures.push(`${route.name} dark: card stayed white`);
      if (row.darkTableIsWhite) failures.push(`${route.name} dark: table stayed white`);
      if (theme === 'dark' && row.cellBg && (row.cellBg === 'rgb(255, 255, 255)' || row.cellBg === 'rgba(255, 255, 255, 1)')) {
        failures.push(`${route.name} dark: table cell stayed white (${row.cellBg})`);
      }
      if (theme === 'dark' && row.headingColor === 'rgb(20, 23, 31)') {
        failures.push(`${route.name} dark: heading still light-theme dark text`);
      }
      if (row.darkBodyIsWhite) failures.push(`${route.name} dark: body stayed white`);
    }
  }

  console.log(JSON.stringify({ snapshots, failures, vendorId, ok: failures.length === 0 }, null, 2));
  if (failures.length) process.exitCode = 1;
  if (headed) await page.waitForTimeout(800);
} finally {
  await browser.close();
}
