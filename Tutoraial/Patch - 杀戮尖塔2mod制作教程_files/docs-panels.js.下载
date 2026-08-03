(function () {
  var TRANSITION_MS = 380;

  function bindPanelCollapse(options) {
    var root = document.querySelector(options.rootSelector);
    var collapseBtn = document.getElementById(options.collapseId);
    var reopenBtn = document.getElementById(options.reopenId);
    if (!root || !collapseBtn) return;

    var storageKey = options.storageKey;
    var collapsed = storageKey && localStorage.getItem(storageKey) === "1";

    function applyState(nextCollapsed, skipSave) {
      collapsed = !!nextCollapsed;
      root.classList.toggle("is-collapsed", collapsed);
      collapseBtn.setAttribute("aria-expanded", collapsed ? "false" : "true");
      if (reopenBtn) reopenBtn.hidden = !collapsed;
      if (!skipSave && storageKey) {
        try {
          localStorage.setItem(storageKey, collapsed ? "1" : "0");
        } catch (e) {}
      }
    }

    applyState(collapsed, true);

    collapseBtn.addEventListener("click", function () {
      applyState(!collapsed);
    });

    if (reopenBtn) {
      reopenBtn.addEventListener("click", function () {
        applyState(false);
      });
    }
  }

  function init() {
    bindPanelCollapse({
      rootSelector: "#sidebar",
      collapseId: "kira-sidebar-collapse",
      reopenId: "kira-sidebar-reopen",
      storageKey: "kira:sidebar-collapsed",
    });

    bindPanelCollapse({
      rootSelector: "#kira-toc-column",
      collapseId: "kira-toc-collapse",
      reopenId: "kira-toc-reopen",
      storageKey: "kira:toc-collapsed",
    });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
