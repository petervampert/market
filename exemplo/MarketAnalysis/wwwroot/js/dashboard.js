/* ═══════════════════════════════════════════════════════════════════════════
   MarketBoard Dashboard  —  dashboard.js
   Auto-refreshes every time the server completes a live data update.
   Polls /api/market/status every 30 s to track the server-side countdown.
═══════════════════════════════════════════════════════════════════════════ */

/* ── Helpers ──────────────────────────────────────────────────────────────── */
const $ = id => document.getElementById(id);
const fmt  = (n, d = 2) => Number(n).toLocaleString('en-US', { minimumFractionDigits: d, maximumFractionDigits: d });
const fmtM = n => {
  const abs = Math.abs(n);
  if (abs >= 1e12) return (n / 1e12).toFixed(2) + 'T';
  if (abs >= 1e9)  return (n / 1e9).toFixed(2)  + 'B';
  if (abs >= 1e6)  return (n / 1e6).toFixed(2)  + 'M';
  return fmt(n);
};
const sign  = n  => n >= 0 ? '+' : '';
const cls   = n  => n >= 0 ? 'gain' : 'loss';
const badge = n  => n >= 0
  ? `<span class="badge-gain">${sign(n)}${fmt(n)}%</span>`
  : `<span class="badge-loss">${sign(n)}${fmt(n)}%</span>`;
const money = n  => `$${fmt(n)}`;
const fmtTime = d =>
  new Date(d).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' });

/* ── Navigation ───────────────────────────────────────────────────────────── */
document.querySelectorAll('.nav-item').forEach(item => {
  item.addEventListener('click', () => {
    document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    item.classList.add('active');
    document.getElementById('view-' + item.dataset.view).classList.add('active');
  });
});

/* ── Toast ────────────────────────────────────────────────────────────────── */
let toastTimer = null;
function showToast(msg, isError = false) {
  const el = $('update-toast');
  el.textContent = msg;
  el.classList.toggle('error', isError);
  el.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove('show'), 4000);
}

/* ── PIE CHART (pure canvas) ─────────────────────────────────────────────── */
const PALETTE = [
  '#58a6ff','#3fb950','#f0883e','#bc8cff','#ffa657',
  '#ff7b72','#56d364','#79c0ff','#d2a8ff','#ffa198',
];

function drawPie(canvasId, data) {
  const canvas = $(canvasId);
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const w = canvas.width = canvas.offsetWidth || 280;
  const h = canvas.height = 280;
  const cx = w / 2, cy = h / 2, r = Math.min(cx, cy) - 20;
  const total = data.reduce((s, d) => s + d.value, 0);
  ctx.clearRect(0, 0, w, h);
  let angle = -Math.PI / 2;
  data.forEach((d, i) => {
    const slice = (d.value / total) * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, r, angle, angle + slice);
    ctx.closePath();
    ctx.fillStyle = PALETTE[i % PALETTE.length];
    ctx.fill();
    ctx.strokeStyle = '#0d1117';
    ctx.lineWidth = 2;
    ctx.stroke();
    angle += slice;
  });
  ctx.beginPath();
  ctx.arc(cx, cy, r * 0.52, 0, Math.PI * 2);
  ctx.fillStyle = '#161b22';
  ctx.fill();
}

function buildLegend(legendId, data) {
  const el = $(legendId);
  if (!el) return;
  const total = data.reduce((s, d) => s + d.value, 0);
  el.innerHTML = data.map((d, i) =>
    `<span class="legend-item">
      <span class="legend-dot" style="background:${PALETTE[i % PALETTE.length]}"></span>
      ${d.label} <strong>${((d.value / total) * 100).toFixed(1)}%</strong>
    </span>`
  ).join('');
}

/* ═══════════════════════════════════════════════════════════════════════════
   LIVE STATUS  &  AUTO-REFRESH ENGINE
═══════════════════════════════════════════════════════════════════════════ */

let lastKnownUpdate = null;   // ISO string from last server response
let countdownTarget = null;   // Date when next server update is expected
let countdownTimer  = null;   // setInterval handle for the countdown display

/* Poll /api/market/status every 30 s.
   If lastUpdated changed since our last check → re-fetch all dashboard data. */
async function pollStatus() {
  try {
    const s = await fetch('/api/market/status').then(r => r.json());
    updateStatusBar(s);

    if (lastKnownUpdate && s.lastUpdated !== lastKnownUpdate) {
      // Server just completed a fresh fetch — reload all views
      showToast(s.dataSource === 'live'
        ? '✅ Live data updated'
        : `⚠️ Data refreshed (${s.dataSource})`);
      await loadAllViews();
    }
    lastKnownUpdate = s.lastUpdated;

    // Set countdown target based on server's nextUpdateSeconds
    countdownTarget = new Date(Date.now() + s.nextUpdateSeconds * 1000);

  } catch {
    // Network error — don't crash the polling loop
  }
}

function updateStatusBar(s) {
  const isError = !!s.lastError;
  const isLive  = !isError && s.dataSource === 'live';
  const dotCls  = isError ? 'error' : isLive ? 'live' : 'seed';
  const srcText = isError ? 'Error' : isLive ? 'Live data' : 'Seed data';
  const srcColor= isError ? 'var(--loss)' : isLive ? 'var(--gain)' : 'var(--warning)';
  const updText = `Updated ${fmtTime(s.lastUpdated)}`;

  // ── Sidebar ──────────────────────────────────────────────────────────────
  const dot   = $('source-dot');
  const label = $('source-label');
  dot.className = `source-dot ${dotCls}`;
  label.textContent  = srcText;
  label.style.color  = srcColor;
  $('last-updated').textContent = updText;

  // ── Topbar (main content area) ───────────────────────────────────────────
  const tdot = $('topbar-dot');
  if (tdot) {
    tdot.className = `source-dot topbar-dot ${dotCls}`;
    $('topbar-source-label').textContent = srcText;
    $('topbar-source-label').style.color = srcColor;
    $('topbar-updated').textContent = updText;
  }
}

function startCountdownTick() {
  clearInterval(countdownTimer);
  countdownTimer = setInterval(() => {
    if (!countdownTarget) return;
    const secs = Math.max(0, Math.round((countdownTarget - Date.now()) / 1000));
    const m = String(Math.floor(secs / 60)).padStart(2, '0');
    const s = String(secs % 60).padStart(2, '0');
    $('next-update-countdown').textContent = `${m}:${s}`;
  }, 1000);
}

/* Manual refresh button (sidebar) */
$('refresh-btn').addEventListener('click', async () => {
  const btn = $('refresh-btn');
  btn.disabled = true;
  btn.textContent = '⟳ Refreshing…';
  try {
    await loadAllViews();
    showToast('Dashboard refreshed');
  } finally {
    btn.disabled = false;
    btn.textContent = '⟳ Refresh now';
  }
});

/* ── Yahoo Finance fetch button ───────────────────────────────────────────── */
$('yahoo-fetch-btn').addEventListener('click', async () => {
  const btn   = $('yahoo-fetch-btn');
  const icon  = $('yahoo-btn-icon');
  const lbl   = $('yahoo-btn-label');

  // Remove old result badge if present
  document.querySelectorAll('.fetch-result').forEach(el => el.remove());

  btn.disabled    = true;
  icon.className  = 'yahoo-btn-icon spinning';
  lbl.textContent = 'Fetching from Yahoo Finance…';

  try {
    const res = await fetch('/api/market/refresh', { method: 'POST' });
    const data = await res.json();

    // Reload all views with the fresh data
    await loadAllViews();

    // Update both status bars immediately
    const status = await fetch('/api/market/status').then(r => r.json());
    updateStatusBar(status);
    lastKnownUpdate = status.lastUpdated;
    countdownTarget = new Date(Date.now() + status.nextUpdateSeconds * 1000);

    // Show result badge next to button
    const badge = document.createElement('span');
    badge.className = `fetch-result ${data.success ? 'ok' : 'err'}`;
    badge.textContent = data.success
      ? `✓ Updated at ${fmtTime(data.lastUpdated)}`
      : `✗ ${data.message}`;
    btn.parentElement.appendChild(badge);
    // Trigger transition
    requestAnimationFrame(() => badge.classList.add('visible'));
    // Auto-remove after 6 s
    setTimeout(() => badge.classList.remove('visible'), 6000);
    setTimeout(() => badge.remove(), 6600);

    showToast(data.success
      ? `✅ Yahoo Finance data loaded (${fmtTime(data.lastUpdated)})`
      : `⚠️ ${data.message}`, !data.success);

  } catch (err) {
    showToast(`❌ Request failed: ${err.message}`, true);
  } finally {
    btn.disabled    = false;
    icon.className  = 'yahoo-btn-icon';
    lbl.textContent = 'Update from Yahoo Finance';
  }
});

/* ═══════════════════════════════════════════════════════════════════════════
   DATA LOADERS
═══════════════════════════════════════════════════════════════════════════ */

async function loadOverview() {
  const [ov] = await Promise.all([
    fetch('/api/market/overview').then(r => r.json()),
  ]);

  // Index chips
  $('index-bar').innerHTML = ov.indices.map(idx => `
    <div class="index-chip">
      <div class="idx-name">${idx.indexName}</div>
      <div class="idx-value">${fmt(idx.value)}</div>
      <div class="idx-chg ${cls(idx.changePercent)}">${sign(idx.changePercent)}${fmt(idx.changePercent)}%
        &nbsp;(${sign(idx.changeAmount)}${fmt(idx.changeAmount)})</div>
    </div>`).join('');

  // Sectors
  const maxAbs = Math.max(...ov.sectors.map(s => Math.abs(s.changePercent)));
  $('sector-table').innerHTML = ov.sectors
    .sort((a, b) => b.changePercent - a.changePercent)
    .map(s => {
      const pct  = s.changePercent;
      const barW = Math.abs(pct) / (maxAbs || 1) * 100;
      const color = pct >= 0 ? 'var(--gain)' : 'var(--loss)';
      return `<div class="sector-row">
        <span class="sector-name">${s.sector}</span>
        <div class="sector-bar-wrap">
          <div class="sector-bar-fill" style="width:${barW}%;background:${color}"></div>
        </div>
        <span class="sector-pct ${cls(pct)}">${sign(pct)}${fmt(pct)}%</span>
        <span class="sector-counts">${s.gainersCount}↑ ${s.losersCount}↓</span>
      </div>`;
    }).join('');

  // Movers
  const moverRow = a => `<div class="mover-row">
    <div>
      <div class="mover-symbol">${a.symbol}</div>
      <div class="mover-price">${money(a.currentPrice)}</div>
    </div>
    <div class="mover-chg ${cls(a.dailyChangePercent)}">${sign(a.dailyChangePercent)}${fmt(a.dailyChangePercent)}%</div>
  </div>`;
  $('top-gainers').innerHTML = ov.topGainers.map(moverRow).join('');
  $('top-losers').innerHTML  = ov.topLosers.map(moverRow).join('');
}

async function loadPortfolio() {
  const p = await fetch('/api/portfolio/summary').then(r => r.json());

  $('portfolio-kpis').innerHTML = [
    { label: 'Total Invested',  value: money(p.totalInvested),     sub: '',  color: 'var(--text)' },
    { label: 'Current Value',   value: money(p.totalCurrentValue), sub: '',  color: 'var(--accent)' },
    { label: 'Unrealised P/L',
      value: `${sign(p.totalUnrealizedGain)}${money(p.totalUnrealizedGain)}`,
      sub:   `${sign(p.totalUnrealizedGainPercent)}${fmt(p.totalUnrealizedGainPercent)}%`,
      color: p.totalUnrealizedGain >= 0 ? 'var(--gain)' : 'var(--loss)' },
    { label: "Today's P/L",
      value: `${sign(p.totalDailyGain)}${money(p.totalDailyGain)}`,
      sub:   `${sign(p.totalDailyGainPercent)}${fmt(p.totalDailyGainPercent)}%`,
      color: p.totalDailyGain >= 0 ? 'var(--gain)' : 'var(--loss)' },
  ].map(k => `
    <div class="kpi-card">
      <div class="kpi-label">${k.label}</div>
      <div class="kpi-value" style="color:${k.color}">${k.value}</div>
      ${k.sub ? `<div class="kpi-sub" style="color:${k.color}">${k.sub}</div>` : ''}
    </div>`).join('');

  document.querySelector('#holdings-table tbody').innerHTML = p.holdings
    .sort((a, b) => b.currentValue - a.currentValue)
    .map(h => `<tr>
      <td><strong>${h.assetSymbol}</strong></td>
      <td>${h.assetName}</td>
      <td><span class="type-badge">${h.assetType}</span></td>
      <td class="num">${fmt(h.quantity, 4)}</td>
      <td class="num">${money(h.averageBuyPrice)}</td>
      <td class="num">${money(h.currentPrice)}</td>
      <td class="num">${money(h.currentValue)}</td>
      <td class="num ${cls(h.unrealizedGain)}">${sign(h.unrealizedGain)}${money(h.unrealizedGain)}</td>
      <td class="num">${badge(h.unrealizedGainPercent)}</td>
      <td class="num">${badge(h.dailyChangePercent)}</td>
    </tr>`).join('');
}

async function loadRoi() {
  const rows = await fetch('/api/portfolio/roi').then(r => r.json());
  const maxROI = Math.max(...rows.map(r => Math.abs(r.simpleRoiPercent)));

  document.querySelector('#roi-table tbody').innerHTML = rows.map(r => {
    const barW    = maxROI ? Math.abs(r.simpleRoiPercent) / maxROI * 100 : 0;
    const barColor = r.simpleRoiPercent >= 0 ? 'var(--gain)' : 'var(--loss)';
    const since    = new Date(r.holdingSince).toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
    return `<tr>
      <td><strong>${r.symbol}</strong></td>
      <td>${r.name}</td>
      <td class="num">${money(r.totalInvested)}</td>
      <td class="num">${money(r.currentValue)}</td>
      <td class="num ${cls(r.unrealizedGain)}">${sign(r.unrealizedGain)}${money(r.unrealizedGain)}</td>
      <td class="num">
        <span class="${cls(r.simpleRoiPercent)}">${sign(r.simpleRoiPercent)}${fmt(r.simpleRoiPercent)}%</span>
        <span class="roi-bar-wrap"><span class="roi-bar-fill" style="width:${barW}%;background:${barColor}"></span></span>
      </td>
      <td class="num ${cls(r.annualisedRoiPercent)}">${sign(r.annualisedRoiPercent)}${fmt(r.annualisedRoiPercent)}%</td>
      <td class="num">${fmt(r.yearsHeld, 2)}</td>
      <td>${since}</td>
    </tr>`;
  }).join('');
}

async function loadAllocation() {
  const data = await fetch('/api/portfolio/allocation').then(r => r.json());
  const typeData  = data.byType.map(t  => ({ label: t.type,   value: t.value }));
  const assetData = data.byAsset.map(a => ({ label: a.symbol, value: a.value }));

  requestAnimationFrame(() => {
    drawPie('chart-type',  typeData);
    drawPie('chart-asset', assetData);
  });
  buildLegend('type-legend',  typeData);
  buildLegend('asset-legend', assetData);

  $('allocation-bars').innerHTML = [
    ...data.byType.map(t  => ({ label: t.type,  value: t.value, pct: t.percent })),
    ...data.byAsset.map(a => ({ label: `${a.symbol} – ${a.name}`, value: a.value, pct: a.percent })),
  ].map(r => `
    <div class="alloc-row">
      <span class="alloc-label" title="${r.label}">${r.label}</span>
      <div class="alloc-bar-wrap">
        <div class="alloc-bar-fill" style="width:${r.pct}%"></div>
      </div>
      <span class="alloc-pct">${fmt(r.pct, 1)}%</span>
      <span class="alloc-val">${money(r.value)}</span>
    </div>`).join('');
}

let allAssets = [];
async function loadScreener() {
  allAssets = await fetch('/api/market/assets').then(r => r.json());
  renderScreener();
}
function renderScreener() {
  const q    = $('screener-search').value.toLowerCase();
  const type = $('screener-type').value;
  const rows = allAssets.filter(a =>
    (!q    || a.symbol.toLowerCase().includes(q) || a.name.toLowerCase().includes(q)) &&
    (!type || a.type === type)
  );
  document.querySelector('#screener-table tbody').innerHTML = rows.map(a => `<tr>
    <td><strong>${a.symbol}</strong></td>
    <td>${a.name}</td>
    <td><span class="type-badge">${a.type}</span></td>
    <td class="num">${money(a.currentPrice)}</td>
    <td class="num ${cls(a.dailyChangeAmount)}">${sign(a.dailyChangeAmount)}${money(a.dailyChangeAmount)}</td>
    <td class="num">${badge(a.dailyChangePercent)}</td>
    <td class="num">${money(a.week52High)}</td>
    <td class="num">${money(a.week52Low)}</td>
    <td class="num">${fmtM(a.volume)}</td>
  </tr>`).join('');
}
// Wire screener filters once
$('screener-search').addEventListener('input',  renderScreener);
$('screener-type').addEventListener('change',   renderScreener);

async function loadTransactions() {
  const rows = await fetch('/api/portfolio/transactions').then(r => r.json());
  document.querySelector('#tx-table tbody').innerHTML = rows.map(t => {
    const d = new Date(t.date).toLocaleDateString('en-US', { day: '2-digit', month: 'short', year: 'numeric' });
    const typeBadge = t.type === 'Buy'
      ? '<span class="badge-buy">BUY</span>'
      : '<span class="badge-sell">SELL</span>';
    return `<tr>
      <td>${d}</td>
      <td>${typeBadge}</td>
      <td><strong>${t.assetSymbol}</strong></td>
      <td class="num">${fmt(t.quantity, 4)}</td>
      <td class="num">${money(t.pricePerUnit)}</td>
      <td class="num">${money(t.fees)}</td>
      <td class="num"><strong>${money(t.totalCost)}</strong></td>
    </tr>`;
  }).join('');
}

/* Reload all data-driven views (not transactions — those don't change live) */
async function loadAllViews() {
  await Promise.all([
    loadOverview(),
    loadPortfolio(),
    loadRoi(),
    loadAllocation(),
    loadScreener(),
    loadTransactions(),
  ]);
}

/* ═══════════════════════════════════════════════════════════════════════════
   INIT
═══════════════════════════════════════════════════════════════════════════ */
async function init() {
  // 1. Load all views immediately with whatever data the server has (seed or live)
  await loadAllViews();

  // 2. Prime the status bar and set lastKnownUpdate reference
  try {
    const s = await fetch('/api/market/status').then(r => r.json());
    updateStatusBar(s);
    lastKnownUpdate = s.lastUpdated;
    countdownTarget = new Date(Date.now() + s.nextUpdateSeconds * 1000);
  } catch { /* non-fatal */ }

  // 3. Start live countdown tick (every second)
  startCountdownTick();

  // 4. Poll status every 30 s — triggers a full re-render when server updates
  setInterval(pollStatus, 30_000);
}

init().catch(err => console.error('Dashboard init error:', err));
