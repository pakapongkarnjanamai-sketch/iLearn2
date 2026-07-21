// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// PLAN-108 §2: instant visual feedback on navigation links/buttons.
// These are plain <a href> elements with no click handler, so on a slower
// device (iPad) the page can feel "stuck" while the next page's JS parses.
// Mark the element as navigating immediately on click so the user sees the
// system received the tap. Do NOT preventDefault — navigation must proceed
// normally.
//
// PLAN-108 Fix 7: the spinner is applied via a CSS ::before content swap on
// `.js-card-action-icon` (see `.is-navigating .js-card-action-icon` in
// user-theme.css) — the JS never rewrites the icon's `class` attribute, so
// there is nothing to "undo" to restore the original icon; removing
// `.is-navigating` is enough. This matters because Safari/iPad aggressively
// restores pages from bfcache on Back navigation: the DOM (including any
// `.is-navigating` class and its `pointer-events:none`) is frozen exactly as
// it was when the user left the page, with no normal page-load JS re-running.
// Without clearing it on `pageshow`, a card the user tapped before leaving
// would come back stuck in the "navigating" spinner state and unclickable.
(function () {
    if (typeof $ === "undefined") return;

    var NAV_TIMEOUT_MS = 8000;

    function clearNavigating($el) {
        $el.removeClass("is-navigating").removeData("ilearnNavigating");
    }

    function markNavigating(el) {
        var $el = $(el);
        if ($el.data("ilearnNavigating")) return;
        $el.data("ilearnNavigating", true);
        $el.addClass("is-navigating");

        // Failsafe: if navigation never happens (user cancels, link blocked,
        // etc.) the card must not stay stuck forever.
        setTimeout(function () {
            clearNavigating($el);
        }, NAV_TIMEOUT_MS);
    }

    $(document).on("click", ".course-item, .catalog-course-item, .player-back-link", function () {
        markNavigating(this);
    });

    // `pageshow` fires both on a normal load (harmless no-op, nothing has
    // `.is-navigating` yet) and when the page is restored from bfcache
    // (the case that matters here).
    window.addEventListener("pageshow", function () {
        $(".is-navigating").each(function () {
            clearNavigating($(this));
        });
    });
})();

