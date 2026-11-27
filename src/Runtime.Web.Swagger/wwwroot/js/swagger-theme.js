// Swagger UI theme: remembers the light/dark choice made with the header toggle. Swagger UI follows the system
// preference on load and does not persist the toggle; this restores the last choice by clicking the toggle, which
// keeps its icon in sync.
(function () {
  var key = 'swagger-theme';
  var root = document.documentElement;

  function read() {
    try { return localStorage.getItem(key); } catch (e) { return null; }
  }

  function write(value) {
    try { localStorage.setItem(key, value); } catch (e) { /* storage unavailable */ }
  }

  function current() {
    return root.classList.contains('dark-mode') ? 'dark' : 'light';
  }

  var observer = new MutationObserver(function () {
    var toggle = document.querySelector('.swagger-ui .dark-mode-toggle button');
    if (!toggle) return;
    observer.disconnect();

    var stored = read();
    if (stored && stored !== current()) toggle.click();

    document.addEventListener('click', function (event) {
      if (event.target.closest && event.target.closest('.dark-mode-toggle')) write(current());
    });
  });

  observer.observe(document.body || root, { childList: true, subtree: true });
})();
