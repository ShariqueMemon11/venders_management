/**
 * E2E: Bank field encryption through the real UI + HTTP pipeline.
 * Create bank → hard reload → plaintext visible → edit IBAN → reload → DB ciphertext check.
 *
 * Prereqs: API on :5182, Vite on :5173
 * Default: HEADED (browser visible). Set HEADLESS=1 only when you explicitly want headless.
 */
import { chromium } from 'playwright';
import { execFileSync } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const BASE = process.env.UI_BASE || 'http://localhost:5173';
const API = process.env.API_BASE || 'http://localhost:5182/api/v1';
const ACCOUNT = '99887766554433';
const IBAN_CREATE = 'DE89370400440532013000';
const IBAN_EDIT = 'GB82WEST12345698765432';
const BANK_NAME = `E2E Encrypt Bank ${Date.now()}`;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../..');

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

function queryRawBank(accountPlaintext) {
  // Query LocalDB for ciphertext of the account we just wrote (match via decrypted path is app-side;
  // here we find rows whose AccountNumber starts with v1. and was recently written).
  const sql = `
SET NOCOUNT ON;
SELECT TOP 5 Id, AccountNumber, Iban, BankName
FROM BankAccounts
WHERE BankName LIKE N'E2E Encrypt Bank%'
ORDER BY CreatedAt DESC;
`;
  const out = execFileSync(
    'sqlcmd',
    [
      '-S', '(localdb)\\mssqllocaldb',
      '-d', 'VendorsDb',
      '-E',
      '-Q', sql,
      '-W',
      '-s', '|',
    ],
    { encoding: 'utf8', cwd: repoRoot }
  );
  return out;
}

async function main() {
  // Default HEADED so the user can watch. Opt into headless only with HEADLESS=1.
  const headless = process.env.HEADLESS === '1' || process.env.HEADLESS === 'true';
  console.log('=== 4c bank encryption E2E (real UI + LocalDB) ===');
  console.log(`UI=${BASE} API=${API} headless=${headless}`);
  if (!headless) {
    console.log('Opening Chromium HEADED — watch your screen');
  }

  // Sanity: API up
  const health = await fetch(`${API}/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email: 'procurement@example.com', password: 'proc123' }),
  });
  if (!health.ok) {
    throw new Error(`API login failed: ${health.status} ${await health.text()}`);
  }
  const { token } = await health.json();
  console.log('PASS API login (procurement)');

  const browser = await chromium.launch({
    headless,
    slowMo: headless ? 0 : 250,
  });
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });

  try {
    await login(page, 'procurement@example.com', 'proc123');
    console.log('PASS UI login');

    // Create draft vendor
    const stamp = Date.now();
    await page.goto(`${BASE}/vendors/new`);
    await page.getByRole('heading', { name: /Create Vendor Draft/i }).waitFor({ timeout: 15000 });
    await page.locator('label').filter({ hasText: /Legal Entity Name/i }).locator('xpath=following-sibling::input')
      .fill(`Bank Encrypt E2E ${stamp}`);
    await page.locator('button[type="submit"]').click();
    await page.waitForURL(/\/vendors\/[0-9a-f-]{36}/i, { timeout: 20000 });
    const vendorUrl = page.url();
    const vendorId = vendorUrl.split('/').pop();
    console.log(`PASS create vendor ${vendorId}`);

    // Banking tab → Add Account
    await clickTab(page, 'banking');
    await page.getByRole('button', { name: /Add Account/i }).click();
    await page.getByRole('heading', { name: /Add Bank Account/i }).waitFor({ timeout: 10000 });

    const modal = page.locator('.modal.show');
    await modal.locator('label').filter({ hasText: /^Bank Name/i }).locator('xpath=following-sibling::input').fill(BANK_NAME);
    await modal.locator('label').filter({ hasText: /^Account Name/i }).locator('xpath=following-sibling::input').fill('E2E Holder');
    await modal.locator('label').filter({ hasText: /Account Number/i }).locator('xpath=following-sibling::input').fill(ACCOUNT);
    await modal.locator('label').filter({ hasText: /^IBAN/i }).locator('xpath=following-sibling::input').fill(IBAN_CREATE);
    await modal.locator('label').filter({ hasText: /Currency/i }).locator('xpath=following-sibling::input').fill('EUR');
    await modal.getByRole('button', { name: /Save Account/i }).click();
    await page.waitForTimeout(1500);

    // Table should show plaintext account number (not v1.)
    const row = page.locator('table tbody tr').filter({ hasText: BANK_NAME });
    await row.waitFor({ timeout: 10000 });
    const tableText = await row.innerText();
    if (tableText.includes('v1.')) {
      throw new Error(`FAIL create display shows ciphertext: ${tableText}`);
    }
    if (!tableText.includes(ACCOUNT)) {
      throw new Error(`FAIL create display missing plaintext account: ${tableText}`);
    }
    console.log('PASS create — table shows plaintext account number');

    // Hard reload
    await page.reload({ waitUntil: 'networkidle' });
    await clickTab(page, 'banking');
    const rowAfterReload = page.locator('table tbody tr').filter({ hasText: BANK_NAME });
    await rowAfterReload.waitFor({ timeout: 10000 });
    const reloadText = await rowAfterReload.innerText();
    if (reloadText.includes('v1.') || !reloadText.includes(ACCOUNT)) {
      throw new Error(`FAIL hard reload display: ${reloadText}`);
    }
    console.log('PASS hard reload — plaintext still displayed');

    // Open edit — confirm form fields are plaintext
    await rowAfterReload.locator('button[title="Edit"]').click();
    await page.getByRole('heading', { name: /Edit Bank Account/i }).waitFor({ timeout: 10000 });
    const editModal = page.locator('.modal.show');
    const acctField = editModal.locator('label').filter({ hasText: /Account Number/i }).locator('xpath=following-sibling::input');
    const ibanField = editModal.locator('label').filter({ hasText: /^IBAN/i }).locator('xpath=following-sibling::input');
    const acctVal = await acctField.inputValue();
    const ibanVal = await ibanField.inputValue();
    if (acctVal !== ACCOUNT) throw new Error(`FAIL edit form account: got "${acctVal}"`);
    if (ibanVal !== IBAN_CREATE) throw new Error(`FAIL edit form iban: got "${ibanVal}"`);
    if (acctVal.startsWith('v1.') || ibanVal.startsWith('v1.')) {
      throw new Error('FAIL edit form showing ciphertext');
    }
    console.log('PASS edit form prefilled with plaintext');

    // Change IBAN and save
    await ibanField.fill(IBAN_EDIT);
    await editModal.getByRole('button', { name: /Update Account/i }).click();
    await page.waitForTimeout(1500);

    // Hard reload again
    await page.reload({ waitUntil: 'networkidle' });
    await clickTab(page, 'banking');
    await page.locator('table tbody tr').filter({ hasText: BANK_NAME }).locator('button[title="Edit"]').click();
    await page.getByRole('heading', { name: /Edit Bank Account/i }).waitFor({ timeout: 10000 });
    const ibanAfter = await page.locator('.modal.show').locator('label').filter({ hasText: /^IBAN/i }).locator('xpath=following-sibling::input').inputValue();
    if (ibanAfter !== IBAN_EDIT) {
      throw new Error(`FAIL edit round-trip IBAN: expected ${IBAN_EDIT}, got ${ibanAfter}`);
    }
    if (ibanAfter.startsWith('v1.')) throw new Error('FAIL edit round-trip returned ciphertext');
    console.log('PASS edit IBAN round-trip after hard reload');

    // API details endpoint (same path the UI uses)
    const details = await fetch(`${API}/vendors/${vendorId}/details`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!details.ok) throw new Error(`details ${details.status}`);
    const body = await details.json();
    const banks = body.data?.bankAccounts || body.bankAccounts || [];
    const bank = banks.find((b) => b.bankName === BANK_NAME || b.BankName === BANK_NAME);
    if (!bank) throw new Error(`bank not in details JSON: ${JSON.stringify(banks).slice(0, 400)}`);
    const apiAcct = bank.accountNumber || bank.AccountNumber;
    const apiIban = bank.iban || bank.Iban;
    if (apiAcct !== ACCOUNT || apiIban !== IBAN_EDIT) {
      throw new Error(`FAIL API details plaintext: account=${apiAcct} iban=${apiIban}`);
    }
    if (String(apiAcct).startsWith('v1.') || String(apiIban).startsWith('v1.')) {
      throw new Error('FAIL API details returned ciphertext to client');
    }
    console.log('PASS GET /vendors/{id}/details returns plaintext JSON');

    // LocalDB spot-check
    let dbOut;
    try {
      dbOut = queryRawBank(ACCOUNT);
    } catch (e) {
      console.warn('WARN sqlcmd failed — trying alternative connection string query via PowerShell');
      throw e;
    }
    console.log('--- LocalDB raw rows ---');
    console.log(dbOut);
    if (!dbOut.includes('v1.')) {
      throw new Error('FAIL LocalDB AccountNumber/Iban did not contain v1. ciphertext prefix');
    }
    if (dbOut.includes(ACCOUNT) || dbOut.includes(IBAN_EDIT) || dbOut.includes(IBAN_CREATE)) {
      throw new Error('FAIL LocalDB still contains plaintext account/IBAN');
    }
    console.log('PASS LocalDB stores v1. ciphertext, not plaintext');

    console.log('\n=== ALL 4c E2E CHECKS PASSED ===');
  } finally {
    await browser.close();
  }
}

main().catch((err) => {
  console.error('E2E FAILED:', err);
  process.exit(1);
});
