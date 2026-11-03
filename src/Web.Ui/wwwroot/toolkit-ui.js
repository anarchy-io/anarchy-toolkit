// Anarchy.Toolkit.Web.Ui: toggles the scroll-edge shadow classes of horizontally scrollable tables.
// Styles live in toolkit-ui.css (.events-table / .table-scroll / .scrolled-from-start / .has-more-to-end).
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
