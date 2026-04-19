/**
 * admin-view-utils.js
 * Shared presentation-layer utilities for iLearn Admin views.
 * Loaded globally after admin-layout.js via _DevExtremeLayout.cshtml.
 *
 * Exports (window.*):
 *   appendText, truncateText, getApiErrorMessage, applyCombinedFilter,
 *   renderProgressCell, renderStatusCell, renderDueDateCell, escapeHtml,
 *   buildStudentGridColumns, initStudentCascadeFilters
 */
(function (window, $) {
    "use strict";

    // ─── Text Utilities ──────────────────────────────────────────────────────

    /**
     * Creates a <span> with optional inline styles and appends it to container.
     * @param {jQuery|Element} container
     * @param {string} text
     * @param {Object} [styles] - CSS properties object
     * @returns {jQuery} the created span
     */
    function appendText(container, text, styles) {
        return $("<span>").css(styles || {}).text(text).appendTo(container);
    }

    /**
     * Truncates text to maxLength and appends "…", or returns a fallback.
     * @param {string} text
     * @param {number} maxLength
     * @param {string} [fallback="—"]
     * @returns {string}
     */
    function truncateText(text, maxLength, fallback) {
        var fb = (fallback !== undefined) ? fallback : "—";
        if (!text) { return fb; }
        if (typeof maxLength !== "number" || maxLength <= 0 || text.length <= maxLength) { return text; }
        return text.slice(0, maxLength) + "…";
    }

    // ─── API Error Handling ──────────────────────────────────────────────────

    /**
     * Extracts a human-readable message from an Ajax error or returns fallback.
     * @param {Object} error - jQuery XHR / response error object
     * @param {string} [fallbackMessage]
     * @returns {string}
     */
    function getApiErrorMessage(error, fallbackMessage) {
        return (error && error.responseJSON && error.responseJSON.message)
            || (error && error.statusText)
            || fallbackMessage
            || "An error occurred.";
    }

    // ─── HTML Escaping ───────────────────────────────────────────────────────

    /**
     * Escapes HTML special characters in a string.
     * Falls back to escapeAdminHtml (admin-layout.js) when available.
     * @param {string|*} value
     * @returns {string}
     */
    function escapeHtml(value) {
        if (window.escapeAdminHtml) {
            return window.escapeAdminHtml(String(value || ""));
        }
        return $("<div>").text(String(value || "")).html();
    }

    // ─── Grid Filter Combinator ──────────────────────────────────────────────

    /**
     * Applies an AND-combined filter to a DevExtreme DataGrid, or clears it.
     * @param {DevExpress.ui.dxDataGrid} gridInstance
     * @param {Array[]} conditions - array of DevExtreme filter expressions
     */
    function applyCombinedFilter(gridInstance, conditions) {
        if (!gridInstance) { return; }
        if (!conditions || conditions.length === 0) {
            gridInstance.clearFilter();
            return;
        }
        var combined = conditions[0];
        for (var i = 1; i < conditions.length; i++) {
            combined = [combined, "and", conditions[i]];
        }
        gridInstance.filter(combined);
    }

    // ─── Cell Renderers ──────────────────────────────────────────────────────

    /**
     * Renders a slim progress bar + percentage label inside a grid cell.
     * Color thresholds: 100% → success, ≥50% → accent, <50% → warning.
     * @param {Element} container
     * @param {number} value - progress percentage (0–100)
     */
    function renderProgressCell(container, value) {
        var progress = value || 0;
        var color = progress === 100
            ? "var(--success-color)"
            : progress >= 50
                ? "var(--accent-color)"
                : "var(--warning-color)";

        $("<div>").addClass("d-flex align-items-center gap-2")
            .append(
                $("<div>").css({
                    flex: "1",
                    height: "3px",
                    background: "var(--border-color)",
                    borderRadius: "2px",
                    overflow: "hidden"
                }).append(
                    $("<div>").css({ height: "100%", width: progress + "%", background: color })
                )
            )
            .append(
                $("<span>").css({
                    fontSize: "11px",
                    fontWeight: "600",
                    minWidth: "32px",
                    color: "var(--text-secondary)"
                }).text(window.formatAdminPercentage(progress, "0%"))
            )
            .appendTo(container);
    }

    /**
     * Renders a tag-pill status badge inside a grid cell.
     * @param {Element} container
     * @param {string} value - raw status value
     * @param {Object} map - mapping of value → { cls, text }
     */
    function renderStatusCell(container, value, map) {
        var status = (map && map[value]) || { cls: "pill-default", text: value || "—" };
        $('<span class="tag-pill ' + status.cls + '">' + status.text + '</span>').appendTo(container);
    }

    /**
     * Renders a due-date label, coloured red when the date is in the past.
     * Delegates date formatting to window.formatAdminDate.
     * @param {Element} container
     * @param {string|Date} value
     * @param {string} [fallback="—"]
     */
    function renderDueDateCell(container, value, fallback) {
        var fb = (fallback !== undefined) ? fallback : "—";
        if (!value) {
            appendText(container, fb, { fontSize: "11px", color: "var(--text-secondary)" });
            return;
        }
        var isPast = new Date(value) < new Date();
        appendText(container, window.formatAdminDate(value, fb), {
            fontSize: "11px",
            color: isPast ? "var(--danger-color)" : "var(--text-secondary)",
            fontWeight: isPast ? "600" : "400"
        });
    }

    // ─── Student Grid Helpers ────────────────────────────────────────────────

    /**
     * Returns the standard DevExtreme column definitions for a student-selection grid.
     * Columns: EId (ID badge), Name, Division, Department, Section.
     * @param {string} [emptyText="—"]
     * @returns {Object[]} DevExtreme column config array
     */
    function buildStudentGridColumns(emptyText) {
        var empty = emptyText || "—";
        return [
            {
                dataField: "EId",
                caption: "ID",
                width: 120,
                cellTemplate: function (container, options) {
                    $("<span>").addClass("tag-pill pill-default fw-bold")
                        .text(options.value || empty).appendTo(container);
                }
            },
            {
                caption: "Name",
                minWidth: 180,
                calculateCellValue: function (data) {
                    return [data.EnglishFirstName, data.EnglishLastName].filter(Boolean).join(" ");
                },
                cellTemplate: function (container, options) {
                    $("<span>").addClass("fw-medium text-dark")
                        .text(options.value || empty).appendTo(container);
                }
            },
            { dataField: "Division", caption: "Division", width: 140 },
            { dataField: "Department", caption: "Department", width: 140 },
            {
                dataField: "Section",
                caption: "Section",
                minWidth: 140,
                cellTemplate: function (container, options) {
                    $("<span>").addClass("text-muted small")
                        .text(options.value || empty).appendTo(container);
                }
            }
        ];
    }

    /**
     * Initialises the Division → Department → Section cascade filter dropboxes
     * and the Clear Filters button for a student-selection grid.
     *
     * Expected DOM elements (IDs): #filter-div, #filter-dept, #filter-section, #btn-clear-filter.
     *
     * @param {DevExpress.ui.dxDataGrid} grid - already-initialised DataGrid instance
     * @param {string} studentsApiUrl - base URL for student lookup endpoints
     *   (must expose: GetDivisions, GetDepartments, GetSections)
     */
    function initStudentCascadeFilters(grid, studentsApiUrl) {
        if (!grid) { return; }
        function makeLookup(endpoint, queryText) {
            return new DevExpress.data.CustomStore({
                key: "Name",
                load: function () {
                    return $.ajax({
                        url: studentsApiUrl + "/" + endpoint + (queryText || ""),
                        method: "GET",
                        xhrFields: { withCredentials: true }
                    }).then(function (response) { return response.data || response; });
                }
            });
        }

        function applyFilters() {
            var division = $("#filter-div").dxSelectBox("instance").option("value");
            var department = $("#filter-dept").dxSelectBox("instance").option("value");
            var section = $("#filter-section").dxSelectBox("instance").option("value");
            var filters = [];
            if (division)   { filters.push(["Division",   "=", division]);   }
            if (department) { filters.push(["Department", "=", department]); }
            if (section)    { filters.push(["Section",    "=", section]);    }
            applyCombinedFilter(grid, filters);
        }

        var divisionBox = $("#filter-div").dxSelectBox({
            dataSource: makeLookup("GetDivisions"),
            displayExpr: "Name",
            valueExpr: "Name",
            placeholder: "All Divisions",
            showClearButton: true,
            onValueChanged: function (event) {
                var departmentBox = $("#filter-dept").dxSelectBox("instance");
                departmentBox.option({
                    value: null,
                    disabled: !event.value,
                    dataSource: event.value
                        ? makeLookup("GetDepartments", '?filter=["Division","=","' + encodeURIComponent(event.value) + '"]')
                        : []
                });
                $("#filter-section").dxSelectBox("instance").option({ value: null, disabled: true, dataSource: [] });
                applyFilters();
            }
        }).dxSelectBox("instance");

        $("#filter-dept").dxSelectBox({
            displayExpr: "Name",
            valueExpr: "Name",
            placeholder: "All Departments",
            showClearButton: true,
            disabled: true,
            onValueChanged: function (event) {
                var sectionBox = $("#filter-section").dxSelectBox("instance");
                var division = divisionBox.option("value");
                var queryText = event.value
                    ? '?filter=[["Division","=","' + encodeURIComponent(division) + '"],"and",["Department","=","' + encodeURIComponent(event.value) + '"]]'
                    : (division ? '?filter=["Division","=","' + encodeURIComponent(division) + '"]' : "");
                sectionBox.option({
                    value: null,
                    disabled: !event.value,
                    dataSource: event.value ? makeLookup("GetSections", queryText) : []
                });
                applyFilters();
            }
        });

        $("#filter-section").dxSelectBox({
            displayExpr: "Name",
            valueExpr: "Name",
            placeholder: "All Sections",
            showClearButton: true,
            disabled: true,
            onValueChanged: applyFilters
        });

        $("#btn-clear-filter").on("click", function () {
            divisionBox.option("value", null);
            $("#filter-dept").dxSelectBox("instance").option({ value: null, disabled: true, dataSource: [] });
            $("#filter-section").dxSelectBox("instance").option({ value: null, disabled: true, dataSource: [] });
            grid.clearFilter();
        });
    }

    // ─── Exports ─────────────────────────────────────────────────────────────

    window.appendText              = appendText;
    window.truncateText            = truncateText;
    window.getApiErrorMessage      = getApiErrorMessage;
    window.escapeHtml              = escapeHtml;
    window.applyCombinedFilter     = applyCombinedFilter;
    window.renderProgressCell      = renderProgressCell;
    window.renderStatusCell        = renderStatusCell;
    window.renderDueDateCell       = renderDueDateCell;
    window.buildStudentGridColumns = buildStudentGridColumns;
    window.initStudentCascadeFilters = initStudentCascadeFilters;

}(window, window.jQuery));
