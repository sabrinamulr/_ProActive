// Datei: wwwroot/jsLisa/app.js
// Seite: app.js – zuverlässiger Logout beim Schließen (sendBeacon mit Body, keepalive-Backup, einmalig)

window.SearchCombo = window.SearchCombo || {};
window.SearchCombo.setDropdownPosition = function (id) {
    const input = document.getElementById(id);
    if (!input) return;
    const dropdown = input.parentElement.querySelector('.searchcombo-dropdown');
    if (!dropdown) return;

    const rect = input.getBoundingClientRect();
    dropdown.style.position = 'fixed';
    dropdown.style.top = rect.bottom + 'px';
    dropdown.style.left = rect.left + 'px';
    dropdown.style.width = rect.width + 'px';
};

// --- Logout bei Tab/Seiten-Schließen ---
// 1) Primär: navigator.sendBeacon('/auth/logout', <body>)
// 2) Backup: fetch('/auth/logout', { keepalive:true })
// 3) Einmal-Schutz: verhindert Mehrfach-Requests
window.Auth = {
    _wired: false,
    _fired: false,

    _sendLogoutOnce() {
        if (this._fired) return;
        this._fired = true;

        try {
            // Body NICHT leer – besseres Verhalten in einigen Browsern
            const body = new Blob(['logout=1'], { type: 'application/x-www-form-urlencoded' });
            const ok = navigator.sendBeacon && navigator.sendBeacon('/auth/logout', body);

            if (!ok) {
                // Backup: keepalive-Request
                fetch('/auth/logout', {
                    method: 'POST',
                    credentials: 'same-origin',
                    keepalive: true
                }).catch(() => { /* ignore */ });
            }
        } catch {
            // Letztes Backup
            try {
                fetch('/auth/logout', {
                    method: 'POST',
                    credentials: 'same-origin',
                    keepalive: true
                }).catch(() => { /* ignore */ });
            } catch { /* ignore */ }
        }
    },

    initLogoutOnClose() {
        if (this._wired) return;
        this._wired = true;

        const send = this._sendLogoutOnce.bind(this);

        // pagehide deckt Schließen + Navigieren ab (am zuverlässigsten)
        window.addEventListener('pagehide', send, { capture: true, once: false });

        // Tab wird unsichtbar (häufig beim Schließen) → Logout
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'hidden') send();
        }, { capture: true });

        // Chrome Page Lifecycle: Tab wird eingefroren
        // (wird nicht überall unterstützt, schadet aber nicht)
        document.addEventListener('freeze', send, { capture: true });

        // zusätzlicher Fallback (nicht immer zuverlässig)
        window.addEventListener('beforeunload', send, { capture: true });
    }
};

// Auto-Init
document.addEventListener('DOMContentLoaded', function () {
    if (window.Auth && typeof window.Auth.initLogoutOnClose === 'function') {
        window.Auth.initLogoutOnClose();
    }
});
