/**
 * Headed check: workflow stepper on Draft, Submitted, Active, Rejected.
 */
import { chromium } from 'playwright';

const UI = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182';
const headed = process.env.HEADLESS !== '1';

async function login(email, password) {
  const res = await fetch(`${API}/api/v1/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error(`login ${email} ${res.status}`);
  return res.json();
}

async function vendors(token) {
  const res = await fetch(`${API}/api/v1/vendors`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const json = await res.json();
  return Array.isArray(json?.data) ? json.data : json?.data?.items || [];
}

async function activeRequest(token, id) {
  const res = await fetch(`${API}/api/v1/vendors/${id}/active-request`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  const json = await res.json();
  return json?.data || json?.Data || null;
}

const proc = await login('procurement@example.com', 'proc123');
const list = await vendors(proc.token);

const draft = list.find((v) => v.status === 1 || v.statusName === 'Draft');
const active = list.find((v) => v.status === 5 || v.statusName === 'Active');

async function pickPendingAt(token, list, want) {
  const pending = list.filter((v) => v.status === 2 || v.statusName === 'PendingReview');
  for (const v of pending) {
    const wf = await activeRequest(token, v.id);
    const steps = wf?.approvalSteps || [];
    const risk = steps.find((s) => s.stepOrder === 1) || steps[0];
    const proc = steps.find((s) => s.stepOrder === 2) || steps[1];
    const riskPending = risk && (risk.status === 1 || risk.statusName === 'Pending');
    const procPending = proc && (proc.status === 1 || proc.statusName === 'Pending');
    const riskApproved = risk && (risk.status === 2 || risk.statusName === 'Approved');
    if (want === 'risk' && riskPending) return v;
    if (want === 'procurement' && riskApproved && procPending) return v;
  }
  return pending[0] || null;
}

const submitted = await pickPendingAt(proc.token, list, 'risk');

let rejected = null;
for (const v of list.filter((x) => x.status === 1 || x.statusName === 'Draft')) {
  const wf = await activeRequest(proc.token, v.id);
  if (wf && (wf.status === 6 || wf.statusName === 'Rejected')) {
    rejected = v;
    break;
  }
}

if (!rejected && submitted) {
  const risk = await login('risk@example.com', 'risk123');
  const wf = await activeRequest(risk.token, submitted.id);
  if (wf?.id) {
    const rej = await fetch(
      `${API}/api/v1/vendors/requests/${wf.id}/workflow?action=Reject`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${risk.token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify('Stepper e2e rejection reason'),
      }
    );
    if (rej.ok) rejected = submitted;
  }
}

const cases = [
  { name: 'Draft', vendor: draft, expectCurrent: 'Draft', expectRejected: false },
  { name: 'Submitted', vendor: submitted, expectCurrent: 'Risk Review', expectRejected: false },
  { name: 'Active', vendor: active, expectCurrent: 'Active', expectRejected: false },
  { name: 'Rejected', vendor: rejected, expectCurrent: 'Draft', expectRejected: true },
];

const browser = await chromium.launch({ headless: !headed, slowMo: headed ? 50 : 0 });
const page = await browser.newPage();
const results = [];
const failures = [];

async function visit(theme, vendor) {
  await page.goto(`${UI}/login`, { waitUntil: 'domcontentloaded' });
  await page.evaluate(
    ({ token, user, theme }) => {
      sessionStorage.setItem('auth_token', token);
      sessionStorage.setItem('user', JSON.stringify(user));
      localStorage.setItem('vendors-theme', theme);
      document.documentElement.setAttribute('data-theme', theme);
    },
    { token: proc.token, user: proc.user, theme }
  );
  await page.goto(`${UI}/vendors/${vendor.id}`, { waitUntil: 'networkidle' });
  await page.waitForSelector('.workflow-stepper', { timeout: 20000 });
  return page.evaluate(() => {
    const current = [...document.querySelectorAll('.workflow-stepper__item--current .workflow-stepper__label')]
      .map((el) => el.textContent.trim());
    const rejected = [...document.querySelectorAll('.workflow-stepper__item--rejected .workflow-stepper__label')]
      .map((el) => el.textContent.trim());
    const caption = document.querySelector('.workflow-stepper__caption')?.textContent?.trim() || null;
    const labels = [...document.querySelectorAll('.workflow-stepper__label')].map((el) => el.textContent.trim());
    const theme = document.documentElement.getAttribute('data-theme');
    return { current, rejected, caption, labels, theme };
  });
}

try {
  for (const theme of ['light', 'dark']) {
    for (const c of cases) {
      if (!c.vendor) {
        failures.push(`${c.name}: no fixture vendor found`);
        results.push({ name: c.name, theme, missing: true });
        continue;
      }
      const snap = await visit(theme, c.vendor);
      const okCurrent = snap.current.includes(c.expectCurrent);
      const okRejected = c.expectRejected
        ? snap.rejected.length > 0 && !!snap.caption
        : snap.rejected.length === 0;
      const okLabels = ['Draft', 'Submitted', 'Risk Review', 'Procurement Approval', 'Active']
        .every((l) => snap.labels.includes(l));
      if (!okCurrent || !okRejected || !okLabels || snap.theme !== theme) {
        failures.push(`${c.name} ${theme}: current=${snap.current} rejected=${snap.rejected} caption=${snap.caption}`);
      }
      results.push({
        name: c.name,
        theme,
        vendorId: c.vendor.id,
        legalName: c.vendor.legalName,
        status: c.vendor.statusName || c.vendor.status,
        ...snap,
        ok: okCurrent && okRejected && okLabels,
      });
    }
  }
  console.log(JSON.stringify({ results, failures, ok: failures.length === 0 }, null, 2));
  if (failures.length) process.exitCode = 1;
  if (headed) await page.waitForTimeout(1200);
} finally {
  await browser.close();
}
