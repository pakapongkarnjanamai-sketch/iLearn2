// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// PLAN-108 §2: instant visual feedback on navigation links/buttons.
// These are plain <a href> elements with no click handler, so on a slower
// device (iPad) the page can feel "stuck" while the next page's JS parses.
// Mark the element as navigating immediately on click so the user sees the
// system received the tap. Do NOT preventDefault — navigation must proceed
// normally. Whole course cards are clickable <a> elements (PLAN-108 §2a), so
// the icon to swap for a spinner must be looked up via the dedicated
// `.js-card-action-icon` marker first — a plain "first <i> in the card"
// lookup would incorrectly grab an unrelated icon (e.g. the due-date
// calendar icon, which appears earlier in the DOM than the action arrow).
(function () {
    if (typeof $ === "undefined") return;

    function markNavigating(el) {
        var $el = $(el);
        if ($el.data("ilearnNavigating")) return;
        $el.data("ilearnNavigating", true);
        $el.addClass("is-navigating").css("pointer-events", "none");

        var $icon = $el.find(".js-card-action-icon").first();
        if (!$icon.length) {
            $icon = $el.find("i.fas, i.far").first();
        }
        if ($icon.length) {
            $icon.attr("class", "fas fa-circle-notch fa-spin js-card-action-icon");
        }
    }

    $(document).on("click", ".course-item, .catalog-course-item, .player-back-link", function () {
        markNavigating(this);
    });
})();
