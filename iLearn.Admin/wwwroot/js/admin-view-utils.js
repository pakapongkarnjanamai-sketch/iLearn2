/**
 * admin-view-utils.js
 * Shared utility functions for iLearn.Admin views.
 * Loaded globally after admin-layout.js.
 *
 * Exports (attached to window):
 *   setQuickActionLoadingState  – toggles loading state on .quick-action elements
 *   refreshGridInstance         – repaints a DevExtreme DataGrid and recalculates dimensions
 *   buildAdminSummaryCard       – returns HTML for a single card inside .sg-summary-grid
 *   initAdminStudentOrgFilters  – wires up division / department / section / position SelectBoxes
 */
(function (window, $) {
    'use strict';

    // ── Quick Action Loading State ────────────────────────────────────────────
    //
    // Toggles the visual loading state of a .quick-action element.
    // Swaps the icon to a spinner and updates the visible text while isLoading
    // is true, then restores both when called with isLoading = false.
    //
    // @param {jQuery}  $element   The .quick-action jQuery element.
    // @param {boolean} isLoading  Whether to enter or exit the loading state.
    // @param {object}  [options]
    //   @param {string} [options.iconSelector]  Selector for the <i> icon; defaults to first .qa-icon i
    //   @param {string} [options.textSelector]  Selector for the label element; defaults to first .u-fw-semibold
    //   @param {string} [options.loadingText]   Text shown while loading; defaults to "Processing..."
    //
    function setQuickActionLoadingState($element, isLoading, options) {
        if (!$element || !$element.length) {
            return;
        }

        var settings = options || {};
        var $icon = settings.iconSelector
            ? $(settings.iconSelector)
            : $element.find('.qa-icon i').first();
        var $text = settings.textSelector
            ? $(settings.textSelector)
            : $element.find('.u-fw-semibold').first();

        $element
            .toggleClass('disabled', isLoading)
            .attr('aria-disabled', isLoading ? 'true' : 'false');

        if ($icon.length) {
            if (isLoading) {
                $icon.data('original-class', $icon.attr('class') || '');
                $icon.attr('class', 'fas fa-spinner fa-spin');
            } else {
                $icon.attr('class', $icon.data('original-class') || 'fas fa-spinner fa-spin');
            }
        }

        if ($text.length) {
            if (isLoading) {
                $text.data('original-text', $text.text());
                $text.text(settings.loadingText || 'Processing...');
            } else {
                $text.text($text.data('original-text') || $text.text());
            }
        }
    }

    // ── Grid Instance Refresh ─────────────────────────────────────────────────
    //
    // Repaints a DevExtreme DataGrid and recalculates its dimensions.
    // Use after showing a previously hidden container that holds a grid.
    //
    // @param {object} gridInstance  DevExtreme DataGrid instance (or null/undefined to no-op).
    //
    function refreshGridInstance(gridInstance) {
        if (!gridInstance) {
            return;
        }
        gridInstance.repaint();
        gridInstance.updateDimensions();
    }

    // ── Admin Summary Card Builder ────────────────────────────────────────────
    //
    // Returns the HTML string for a single summary card placed inside a
    // .sg-summary-grid container.
    //
    // @param {*}      value      Numeric or text value displayed prominently.
    // @param {string} label      Short label shown below the value.
    // @param {string} className  Optional extra CSS class applied to the value element
    //                            (e.g. "u-text-success", "text-dark").
    // @returns {string} HTML string.
    //
    function buildAdminSummaryCard(value, label, className) {
        var safeValue = (value !== null && value !== undefined) ? String(value) : '\u2014';
        var safeLabel = window.escapeAdminHtml ? window.escapeAdminHtml(label || '') : (label || '');
        var colorClass = className || 'text-dark';
        return '<div class="sg-summary-card">'
            + '<div class="sg-summary-value ' + colorClass + '">' + safeValue + '</div>'
            + '<div class="sg-summary-label">' + safeLabel + '</div>'
            + '</div>';
    }

    // ── Student Org Filters ───────────────────────────────────────────────────
    //
    // Initialises DevExtreme SelectBox dropdowns for Division, Department,
    // Section and (optionally) Position that filter a student DataGrid.
    //
    // Each SelectBox loads its options from the Students API and calls
    // grid.filter() with the combined filter expression whenever a value changes.
    // A clear button resets all SelectBoxes and removes the active filter.
    //
    // @param {object} options
    //   @param {string} options.studentsBaseUrl   Base URL for Students API (no trailing slash).
    //   @param {object} options.gridInstance      DevExtreme DataGrid instance to filter.
    //   @param {string} [options.divSelector]     Selector for the Division SelectBox container.
    //                                             Defaults to "#filter-div".
    //   @param {string} [options.deptSelector]    Selector for the Department SelectBox container.
    //                                             Defaults to "#filter-dept".
    //   @param {string} [options.sectionSelector] Selector for the Section SelectBox container.
    //                                             Defaults to "#filter-section".
    //   @param {string} [options.positionSelector] Selector for the Position SelectBox container.
    //                                             Omit to skip position filtering.
    //   @param {string} [options.clearSelector]   Selector for the clear-filters button.
    //                                             Defaults to "#btn-clear-filter".
    //
    function initAdminStudentOrgFilters(options) {
        var opts = options || {};
        var baseUrl = opts.studentsBaseUrl;
        var grid = opts.gridInstance;

        if (!baseUrl || !grid) {
            return;
        }

        var divSel     = opts.divSelector      || '#filter-div';
        var deptSel    = opts.deptSelector     || '#filter-dept';
        var sectionSel = opts.sectionSelector  || '#filter-section';
        var posSel     = opts.positionSelector || null;
        var clearSel   = opts.clearSelector    || '#btn-clear-filter';

        var selectBoxDefaults = {
            showClearButton: true,
            placeholder: 'All',
            width: '100%',
            onValueChanged: function () {
                applyOrgFilters();
            }
        };

        function makeDataSource(endpoint) {
            return {
                store: new DevExpress.data.CustomStore({
                    key: 'Name',
                    load: function () {
                        return $.ajax({
                            url: baseUrl + '/' + endpoint,
                            method: 'GET',
                            xhrFields: { withCredentials: true }
                        }).then(function (data) {
                            return Array.isArray(data) ? data : (data.data || []);
                        });
                    }
                }),
                paginate: false
            };
        }

        function initBox(selector, endpoint) {
            var $el = $(selector);
            if (!$el.length) {
                return null;
            }
            return $el.dxSelectBox($.extend({}, selectBoxDefaults, {
                dataSource: makeDataSource(endpoint),
                displayExpr: 'Name',
                valueExpr: 'Name'
            })).dxSelectBox('instance');
        }

        var divBox     = initBox(divSel,     'GetDivisions');
        var deptBox    = initBox(deptSel,    'GetDepartments');
        var sectionBox = initBox(sectionSel, 'GetSections');
        var posBox     = posSel ? initBox(posSel, 'GetPositions') : null;

        function applyOrgFilters() {
            var filters = [];

            function addFilter(col, box) {
                var val = box ? box.option('value') : null;
                if (val !== null && val !== undefined && val !== '') {
                    filters.push([col, '=', val]);
                }
            }

            addFilter('Division',   divBox);
            addFilter('Department', deptBox);
            addFilter('Section',    sectionBox);
            addFilter('Position',   posBox);

            if (filters.length === 0) {
                grid.filter(null);
            } else if (filters.length === 1) {
                grid.filter(filters[0]);
            } else {
                var combined = filters[0];
                for (var i = 1; i < filters.length; i++) {
                    combined = [combined, 'and', filters[i]];
                }
                grid.filter(combined);
            }
        }

        $(clearSel).off('click.orgFilter').on('click.orgFilter', function () {
            if (divBox)     { divBox.option('value', null); }
            if (deptBox)    { deptBox.option('value', null); }
            if (sectionBox) { sectionBox.option('value', null); }
            if (posBox)     { posBox.option('value', null); }
            grid.filter(null);
        });
    }

    // ── Exports ───────────────────────────────────────────────────────────────
    window.setQuickActionLoadingState = setQuickActionLoadingState;
    window.refreshGridInstance        = refreshGridInstance;
    window.buildAdminSummaryCard      = buildAdminSummaryCard;
    window.initAdminStudentOrgFilters = initAdminStudentOrgFilters;

})(window, window.jQuery);
