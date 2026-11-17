// Anarchy.Toolkit.Web.Ui: toggles the scroll-edge shadow classes of horizontally scrollable tables.
// Styles live in css/toolkit-ui.css (.events-table / .table-scroll / .scrolled-from-start / .has-more-to-end).
(function () {
    function update(el) {
        var sc = el.querySelector('.table-scroll') || el;
        var max = sc.scrollWidth - sc.clientWidth;
        el.classList.toggle('scrolled-from-start', sc.scrollLeft > 1);
        el.classList.toggle('has-more-to-end', sc.scrollLeft < max - 1);
    }
    function updateAll() {
        document.querySelectorAll('.events-table').forEach(update);
    }
    var pending = false;
    function schedule() {
        if (pending) return;
        pending = true;
        requestAnimationFrame(function () { pending = false; updateAll(); });
    }
    document.addEventListener('scroll', function (e) {
        if (e.target && e.target.classList && e.target.classList.contains('table-scroll')) update(e.target.parentElement);
    }, true);
    window.addEventListener('resize', schedule);
    new MutationObserver(schedule).observe(document.body, { childList: true, subtree: true });
    document.addEventListener('DOMContentLoaded', schedule);
})();

// Anarchy.Toolkit.Web.Ui: light/dark mode. [data-theme-toggle] buttons (the ThemeToggle component) switch
// <html data-theme>, which css/toolkit-ui.css reads; the choice is stored and re-applied before paint by UiHeadAssets.
(function () {
    var key = 'toolkit-ui-theme';
    var root = document.documentElement;
    var system = window.matchMedia('(prefers-color-scheme: dark)');

    function effective() {
        return root.dataset.theme || (system.matches ? 'dark' : 'light');
    }

    function sync() {
        var label = effective() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
        document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
            button.setAttribute('aria-label', label);
            button.title = label;
        });
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest && event.target.closest('[data-theme-toggle]');
        if (!button) return;
        var next = effective() === 'dark' ? 'light' : 'dark';
        root.dataset.theme = next;
        try { localStorage.setItem(key, next); } catch (e) { /* storage unavailable */ }
        sync();
    });

    system.addEventListener('change', sync);
    new MutationObserver(sync).observe(document.body, { childList: true, subtree: true });
    sync();
})();
