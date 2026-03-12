/**
 * app.js – Swinton Go! Insurance Motor Quote Demo
 * Rebranded from Dial Direct. Manages per-page form data using
 * sessionStorage, vehicle lookup, and quote price calculation.
 */

// ── Storage helpers ───────────────────────────────────────
const Store = (() => {
    const PREFIX = 'sgq_';
    return {
        get(key)      { try { return JSON.parse(sessionStorage.getItem(PREFIX + key)); } catch { return null; } },
        set(key, val) { try { sessionStorage.setItem(PREFIX + key, JSON.stringify(val)); } catch {} },
        clear()       {
            const keys = Object.keys(sessionStorage).filter(k => k.startsWith(PREFIX));
            keys.forEach(k => sessionStorage.removeItem(k));
        }
    };
})();

// ── Vehicle database (demo stub) ──────────────────────────
const VEHICLES = {
    'D500NLE': {
        make:         'Volkswagen',
        model:        'Golf',
        variant:      '1.5 TSI SE 5dr',
        year:         2019,
        cc:           1498,
        fuelType:     'Petrol',
        transmission: 'Manual',
        doors:        5,
        bodyType:     'Hatchback',
        colour:       'Metallic Silver',
        value:        14250
    },
    'AB12CDE': {
        make:         'Ford',
        model:        'Focus',
        variant:      '1.0 EcoBoost 125 ST-Line 5dr',
        year:         2021,
        cc:           999,
        fuelType:     'Petrol',
        transmission: 'Manual',
        doors:        5,
        bodyType:     'Hatchback',
        colour:       'Magnetic Grey',
        value:        16500
    }
};

/**
 * Look up a vehicle by registration (API stub - returns demo data).
 * @param {string} rawReg - Raw registration string
 * @returns {object|null} Vehicle data or null if not found
 */
function lookupVehicle(rawReg) {
    const reg = rawReg.replace(/\s+/g, '').toUpperCase();
    return VEHICLES[reg] || null;
}

// ── Quote price calculation ───────────────────────────────
/** Finance uplift applied to monthly payments (represents ~7% APR for instalment plans) */
const MONTHLY_FINANCE_UPLIFT = 0.07;
/** Number of months in a year */
const MONTHS_PER_YEAR = 12;

/**
 * Calculate an indicative insurance quote price.
 * NOTE: This is a demo stub - real pricing would call an API.
 * @param {object} data - All form data collected across pages
 * @returns {{ monthly: number, annual: number }}
 */
function calculateQuote(data) {
    let base = 560;

    // Age factor
    if (data.dob) {
        const dob = new Date(data.dob);
        const age = Math.floor((Date.now() - dob) / (365.25 * 24 * 3600 * 1000));
        if      (age < 22) base += 550;
        else if (age < 25) base += 280;
        else if (age < 30) base += 90;
        else if (age > 70) base += 120;
    }

    // Cover type
    if (data.coverType === 'comprehensive') base += 0;
    if (data.coverType === 'tpft')          base -= 60;
    if (data.coverType === 'tpo')           base -= 120;

    // NCB discount
    const ncb = parseInt(data.ncbYears, 10) || 0;
    if      (ncb >= 9) base *= 0.55;
    else if (ncb >= 5) base *= 0.65;
    else if (ncb >= 3) base *= 0.78;
    else if (ncb >= 1) base *= 0.90;

    // Mileage
    const miles = parseInt(data.annualMileage, 10) || 10000;
    if (miles > 20000) base += 80;
    if (miles > 30000) base += 60;

    // Excess reduction
    const excess = parseInt(data.voluntaryExcess, 10) || 0;
    base -= (excess / 1000) * 40;

    // Claims/convictions
    const claims = parseInt(data.claimsCount, 10) || 0;
    const convictions = parseInt(data.convictionsCount, 10) || 0;
    base += claims * 180;
    base += convictions * 240;

    // Car use
    if (data.carUse === 'business') base += 60;

    // Ensure minimum
    base = Math.max(base, 220);

    const annual  = Math.round(base);
    const monthly = parseFloat((annual / MONTHS_PER_YEAR * (1 + MONTHLY_FINANCE_UPLIFT)).toFixed(2)); // Monthly includes finance uplift

    return { monthly, annual };
}

// ── Shared utilities ──────────────────────────────────────
function showErr(id, msg) {
    const el = document.getElementById(id);
    if (el) {
        if (msg) el.textContent = msg;
        el.classList.add('visible');
    }
}

function clearErr(id) {
    const el = document.getElementById(id);
    if (el) el.classList.remove('visible');
}

function setFooterYear() {
    const el = document.getElementById('footer-year');
    if (el) el.textContent = new Date().getFullYear();
}

document.addEventListener('DOMContentLoaded', setFooterYear);
