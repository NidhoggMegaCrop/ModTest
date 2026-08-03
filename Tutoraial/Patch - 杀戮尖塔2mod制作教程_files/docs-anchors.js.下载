(function () {
  if (location.hostname === "glitchedreme.github.io") {
    var prefix = "/SlayTheSpire2ModdingTutorials";
    var path = location.pathname;
    if (path === prefix || path.indexOf(prefix + "/") === 0) {
      var rest = path.slice(prefix.length) || "/";
      location.replace(
        "https://tutorials.sts2modding.com" + rest + location.search + location.hash
      );
      return;
    }
  }

  var DESKTOP_MQ = "(min-width: 1001px)";
  var SCROLL_OFFSET_DESKTOP = 28;

  /** @type {{ article: HTMLElement, tocIdSet: Set<string>, scrollLocked: boolean, onScroll: () => void, scrollTarget: EventTarget | null } | null} */
  var state = null;

  function getHeadingText(heading) {
    var clone = heading.cloneNode(true);
    var anchor = clone.querySelector(".kira-heading-anchor");
    if (anchor) anchor.remove();
    return (clone.textContent || "").trim();
  }

  function slugifyHeading(text) {
    var slug = (text || "")
      .trim()
      .toLowerCase()
      .replace(/\s+/g, "-")
      .replace(/[^\w\u4e00-\u9fff\-]+/g, "")
      .replace(/-+/g, "-")
      .replace(/^-|-$/g, "");
    if (!slug) return "section";
    if (/^__/.test(slug)) slug = "param-" + slug.replace(/^__+/, "");
    return slug;
  }

  function assignHeadingId(heading, usedIds) {
    var text = getHeadingText(heading);
    if (!text) return null;
    var id = slugifyHeading(text);
    var base = id;
    var suffix = 2;
    while (usedIds.has(id)) {
      id = base + "-" + suffix;
      suffix += 1;
    }
    usedIds.add(id);
    heading.id = id;
    heading.querySelectorAll("span[id]").forEach(function (span) {
      span.removeAttribute("id");
    });
    return { id: id, target: heading };
  }

  function shouldIncludeInToc(heading) {
    var level = heading.tagName.toLowerCase();
    if (level !== "h2" && level !== "h3" && level !== "h4") return false;
    return !!getHeadingText(heading);
  }

  /**
   * 桌面：仅 .kira-content 滚动（body overflow:hidden）
   * 移动：整页 window 滚动
   * 不用 scrollHeight 启发式——布局未完成时会误判为 window，导致间歇失效
   */
  function getScrollRoot() {
    var content = document.querySelector(".kira-content");
    if (window.matchMedia(DESKTOP_MQ).matches && content) {
      return { type: "element", el: content };
    }
    return { type: "window" };
  }

  function getScrollOffset() {
    if (window.matchMedia(DESKTOP_MQ).matches) return SCROLL_OFFSET_DESKTOP;
    var header = document.querySelector(".kira-header");
    if (header) {
      var h = header.getBoundingClientRect().height;
      if (h > 0) return Math.ceil(h) + 12;
    }
    return 64;
  }

  function getHeadingTop(heading, root) {
    if (root.type === "element") {
      var container = root.el;
      var elRect = heading.getBoundingClientRect();
      var boxRect = container.getBoundingClientRect();
      return container.scrollTop + (elRect.top - boxRect.top);
    }
    return heading.getBoundingClientRect().top + window.scrollY;
  }

  function getScrollTop(root) {
    return root.type === "element" ? root.el.scrollTop : window.scrollY;
  }

  function setScrollTop(root, top) {
    if (root.type === "element") {
      root.el.scrollTop = top;
    } else {
      window.scrollTo(0, top);
    }
  }

  function setLocationHash(id) {
    if (!id) return;
    var hash = "#" + id;
    if (location.hash === hash) return;
    history.replaceState(null, "", location.pathname + location.search + hash);
  }

  function scrollToHeading(heading, updateHash) {
    if (!heading || !heading.id) return;
    var root = getScrollRoot();
    var offset = getScrollOffset();
    var top = Math.max(0, getHeadingTop(heading, root) - offset);
    setScrollTop(root, top);
    if (updateHash !== false) setLocationHash(heading.id);
    if (state) {
      lockScrollSync(80);
      setActiveToc(heading.id);
    }
  }

  function scrollTocItemIntoView(nav, item) {
    if (!nav || !item) return;
    var navRect = nav.getBoundingClientRect();
    var itemRect = item.getBoundingClientRect();
    if (itemRect.top < navRect.top) {
      nav.scrollTop -= navRect.top - itemRect.top + 4;
    } else if (itemRect.bottom > navRect.bottom) {
      nav.scrollTop += itemRect.bottom - navRect.bottom + 4;
    }
  }

  function setActiveToc(targetId) {
    if (!state || !targetId) return;
    var tocId = resolveTocId(targetId);
    var list = document.getElementById("kira-chapters-list");
    var chaptersNav = document.querySelector(".kira-chapters-nav");
    if (!list) return;

    var activeItem = null;
    list.querySelectorAll(".kira-chapter-item").forEach(function (item) {
      var link = item.querySelector("a");
      if (!link) return;
      var id = decodeURIComponent((link.getAttribute("href") || "").replace(/^#/, ""));
      var isActive = id === tocId;
      item.classList.toggle("is-active", isActive);
      if (isActive) activeItem = item;
    });
    if (activeItem && chaptersNav) scrollTocItemIntoView(chaptersNav, activeItem);
  }

  function getPreviousHeading(heading, article) {
    var all = article.querySelectorAll("h2, h3, h4");
    for (var i = all.length - 1; i >= 0; i--) {
      if (all[i] === heading) return i > 0 ? all[i - 1] : null;
    }
    return null;
  }

  function resolveTocId(headingId) {
    if (!state) return headingId;
    if (state.tocIdSet.has(headingId)) return headingId;
    var el = document.getElementById(headingId);
    if (!el || !state.article.contains(el)) return headingId;
    var prev = getPreviousHeading(el, state.article);
    while (prev) {
      if (state.tocIdSet.has(prev.id)) return prev.id;
      prev = getPreviousHeading(prev, state.article);
    }
    return headingId;
  }

  function lockScrollSync(ms) {
    if (!state) return;
    state.scrollLocked = true;
    if (state.scrollUnlockTimer) clearTimeout(state.scrollUnlockTimer);
    state.scrollUnlockTimer = setTimeout(function () {
      state.scrollLocked = false;
      syncActiveByScroll();
    }, ms || 80);
  }

  function syncActiveByScroll() {
    if (!state || state.scrollLocked || !state.spyHeadings.length) return;
    var root = getScrollRoot();
    var offset = getScrollOffset();
    var scrollPos = getScrollTop(root);
    var active = state.spyHeadings[0];

    state.spyHeadings.forEach(function (heading) {
      if (getHeadingTop(heading, root) - offset <= scrollPos + 1) {
        active = heading;
      }
    });

    setActiveToc(active.id);
  }

  function bindScrollListener() {
    if (!state) return;
    if (state.scrollTarget && state.onScroll) {
      state.scrollTarget.removeEventListener("scroll", state.onScroll);
    }
    var root = getScrollRoot();
    state.onScroll = function () {
      if (!state.scrollLocked) syncActiveByScroll();
    };
    state.scrollTarget = root.type === "element" ? root.el : window;
    state.scrollTarget.addEventListener("scroll", state.onScroll, { passive: true });
  }

  function addHeadingAnchors(article, usedIds) {
    var headings = article.querySelectorAll("h2, h3, h4");
    headings.forEach(function (heading) {
      var info = assignHeadingId(heading, usedIds);
      if (!info || heading.querySelector(".kira-heading-anchor")) return;

      var anchor = document.createElement("a");
      anchor.className = "kira-heading-anchor";
      anchor.href = "#" + info.id;
      anchor.setAttribute("aria-label", "链接到此章节");
      anchor.textContent = "#";
      anchor.addEventListener("click", function (event) {
        event.preventDefault();
        scrollToHeading(info.target, true);
      });

      heading.classList.add("kira-heading-with-anchor");
      var label = document.createElement("span");
      label.className = "kira-heading-text";
      while (heading.firstChild) label.appendChild(heading.firstChild);
      heading.appendChild(label);
      heading.appendChild(anchor);
    });
    return headings;
  }

  function createChapterLink(heading, list, tocIdSet) {
    if (!shouldIncludeInToc(heading)) return;
    var id = heading.id;
    if (!id) return;
    var text = getHeadingText(heading);
    if (!text) return;

    tocIdSet.add(id);

    var level = heading.tagName.toLowerCase();
    var li = document.createElement("li");
    li.className =
      "kira-chapter-item " +
      (level === "h4" ? "is-sub-2" : level === "h3" ? "is-sub" : "");

    var a = document.createElement("a");
    a.href = "#" + id;
    a.textContent = text;
    a.addEventListener("click", function (event) {
      event.preventDefault();
      lockScrollSync(120);
      scrollToHeading(heading, true);
    });

    li.appendChild(a);
    list.appendChild(li);
  }

  function scrollFromHash() {
    var raw = location.hash.replace(/^#/, "");
    if (!raw) return;
    var id = decodeURIComponent(raw);
    var el = document.getElementById(id);
    if (!el) return;
    var heading = el.closest("h2, h3, h4") || el;
    scrollToHeading(heading, false);
  }

  function buildToc() {
    var panel = document.getElementById("kira-chapters-panel");
    var list = document.getElementById("kira-chapters-list");
    var article = document.querySelector(".kira-main-content .kira-post article");
    if (!panel || !list || !article) return;

    var usedIds = new Set();
    addHeadingAnchors(article, usedIds);

    var headings = article.querySelectorAll("h2, h3, h4");
    if (!headings.length) {
      panel.classList.remove("is-visible");
      state = null;
      return;
    }

    list.innerHTML = "";
    var tocIdSet = new Set();
    var spyHeadings = [];

    headings.forEach(function (heading) {
      if (heading.id) spyHeadings.push(heading);
      createChapterLink(heading, list, tocIdSet);
    });

    var inlineToc = document.getElementById("kira-inline-toc");
    if (inlineToc) inlineToc.remove();

    if (!list.children.length) {
      panel.classList.remove("is-visible");
      state = null;
      return;
    }

    if (window.matchMedia("(min-width: 1001px)").matches) {
      panel.classList.add("is-visible");
    } else {
      panel.classList.remove("is-visible");
    }

    state = {
      article: article,
      tocIdSet: tocIdSet,
      spyHeadings: spyHeadings,
      scrollLocked: false,
      scrollUnlockTimer: null,
      onScroll: null,
      scrollTarget: null,
    };

    bindScrollListener();
    syncActiveByScroll();
  }

  var layoutRefreshTimer = null;

  function refreshAfterLayout() {
    if (!state) return;
    bindScrollListener();
    syncActiveByScroll();
  }

  function scheduleLayoutRefresh() {
    if (layoutRefreshTimer) clearTimeout(layoutRefreshTimer);
    layoutRefreshTimer = setTimeout(refreshAfterLayout, 120);
  }

  function init() {
    buildToc();

    requestAnimationFrame(function () {
      requestAnimationFrame(function () {
        refreshAfterLayout();
        scrollFromHash();
      });
    });

    window.addEventListener("load", function () {
      refreshAfterLayout();
      scrollFromHash();
    });
    window.addEventListener("hashchange", scrollFromHash);

    window.matchMedia(DESKTOP_MQ).addEventListener("change", function () {
      buildToc();
      refreshAfterLayout();
      scrollFromHash();
    });

    var content = document.querySelector(".kira-content");
    if (content && window.ResizeObserver) {
      var ro = new ResizeObserver(scheduleLayoutRefresh);
      ro.observe(content);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
