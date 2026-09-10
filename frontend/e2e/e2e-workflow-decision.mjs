/**
 * Headed check: same vendor at Procurement Approval.
 * Risk user: buttons hidden, waiting caption only.
 * Procurement user: Approve/Reject enabled, no caption.
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const headed = process.env.HEADLESS !== '1';

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 50 : 0 });
const page = await browser.newPage();
const failures = [];

async function login(email, password) {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 20000 });
}

async function openVisibleDemo() {
  await page.goto(`${UI}/vendors/directory`, { waitUntil: 'networkidle' });
  const nameBox = page.locator('input[placeholder="Legal name, trade name, or number"]');
  await nameBox.fill('Visible Demo');
  await page.getByRole('button', { name: /Search/i }).click();
  await page.waitForTimeout(800);
  const detailsLink = page.locator('a.btn-outline-primary').filter({ hasText: /Details/i }).first();
  if ((await detailsLink.count()) === 0) throw new Error('Visible Demo vendor not found');
  await detailsLink.click();
  await page.waitForURL(/\/vendors\//, { timeout: 15000 });
  await page.locator('.workflow-stepper').waitFor({ timeout: 15000 });
}

try {
  await login('risk@example.com', 'risk123');
  await openVisibleDemo();
  const currentRisk = (await page.locator('.workflow-stepper__item--current .workflow-stepper__label').innerText()).trim();
  if (currentRisk !== 'Procurement Approval') failures.push(`risk: stepper is "${currentRisk}"`);
  if (await page.getByRole('button', { name: /^Approve$/ }).count()) failures.push('risk: Approve still visible');
  if (await page.getByRole('button', { name: /^Reject$/ }).count()) failures.push('risk: Reject still visible');
  if ((await page.getByText('Waiting on Procurement Manager.').count()) === 0) {
    failures.push('risk: missing waiting caption');
  } else {
    console.log('PASS risk: buttons hidden, caption shown');
  }

  await login('procurement@example.com', 'proc123');
  await openVisibleDemo();
  const currentProc = (await page.locator('.workflow-stepper__item--current .workflow-stepper__label').innerText()).trim();
  if (currentProc !== 'Procurement Approval') failures.push(`proc: stepper is "${currentProc}"`);
  const approve = page.getByRole('button', { name: /^Approve$/ });
  const reject = page.getByRole('button', { name: /^Reject$/ });
  if (!(await approve.isVisible()) || !(await reject.isVisible())) failures.push('proc: Approve/Reject not visible');
  else if (!(await approve.isEnabled()) || !(await reject.isEnabled())) failures.push('proc: Approve/Reject not enabled');
  else console.log('PASS proc: Approve/Reject visible and enabled');
  if (await page.getByText('Waiting on Procurement Manager.').count()) {
    failures.push('proc: waiting caption should be hidden');
  }

  console.log(JSON.stringify({ failures, ok: failures.length === 0 }, null, 2));
  if (failures.length) process.exitCode = 1;
  if (headed) await page.waitForTimeout(1200);
} finally {
  await browser.close();
}
