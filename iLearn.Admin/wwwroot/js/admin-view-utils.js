// Shared Admin View Utilities
// Provides cross-cutting, non-business helpers used across multiple Admin views.
// Depends on: admin-layout.js (must be loaded first)
(function (window, $) {
    // ─── HTML Escaping ───────────────────────────────────────────────────────────
    // Alias for window.escapeAdminHtml so pages can call escapeHtml() without a
    // local redefinition after removing their private copies.
    function escapeHtml(value) {
        return window.escapeAdminHtml ? window.escapeAdminHtml(value) : String(value == null ? '' : value);
    }
    // ─── API Error Resolver ───────────────────────────────────────────────────────
    function getApiErrorMessage(error, fallbackMessage) {
        return (error && error.responseJSON && error.responseJSON.message)
            || (error && error.statusText)
            || fallbackMessage
            || 'An error occurred.';
    }
    // ─── Grid Filter Helper ───────────────────────────────────────────────────────
    // Applies an AND-combined DevExtreme filter from an array of individual conditions.
    // Clears the grid filter when conditions is empty.
    function applyCombinedFilter(gridInstance, conditions) {
        if (!conditions || conditions.length === 0) {
            gridInstance.clearFilter();
            return;
        }
        let filter = conditions[0];
        for (let index = 1; index < conditions.length; index += 1) {
            filter = [filter, 'and', conditions[index]];
        }
        gridInstance.filter(filter);
    }
    // ─── Generic DOM Helpers ──────────────────────────────────────────────────────
    function appendAdminText(container, text, styles) {
        return $('<span>').css(styles || {}).text(text).appendTo(container);
    }
    function truncateAdminText(text, maxLength) {
        if (!text || text.length <= maxLength) {
            return text || '\u2014';
        }
        return text.slice(0, maxLength) + '\u2026';
    }
    // ─── Cell Renderers ───────────────────────────────────────────────────────────
    // Renders a thin progress bar with a percentage label.
    // options.useWarningForPartial {boolean} - when provided, controls color for non-zero
    //   progress: true → warning, false → accent. When omitted the default threshold
    //   of 50 % is used (below 50 % shows warning, at or above shows accent).
    function renderAdminProgressCell(container, value, options) {
        const progress = value || 0;
        const opts = options || {};
        let color;
        if (progress === 100) {
            color = 'var(--success-color)';
        } else if ('useWarningForPartial' in opts) {
            color = progress > 0
                ? (opts.useWarningForPartial ? 'var(--warning-color)' : 'var(--accent-color)')
                : 'var(--border-color)';
        } else {
            color = progress >= 50 ? 'var(--accent-color)' : 'var(--warning-color)';
        }
        $('<div>').addClass('d-flex align-items-center gap-2')
            .append(
                $('<div>').css({
                    flex: '1',
                    height: '3px',
                    background: 'var(--border-color)',
                    borderRadius: '2px',
                    overflow: 'hidden'
                }).append(
                    $('<div>').css({
                        height: '100%',
                        width: progress + '%',
                        background: color
                    })
                )
            )
            .append(
                $('<span>').css({
                    fontSize: '11px',
                    fontWeight: '600',
                    minWidth: '32px',
                    color: 'var(--text-secondary)'
                }).text(window.formatAdminPercentage(progress, '0%'))
            )
            .appendTo(container);
    }
    // Renders a status pill using a status → { cls, text } map.
    function renderAdminStatusCell(container, value, map) {
        const status = (map && map[value]) || { cls: 'pill-default', text: value || '\u2014' };
        $('<span class="tag-pill ' + status.cls + '">' + status.text + '</span>').appendTo(container);
    }
    // Renders a date cell, highlighting overdue dates in danger color.
    function renderAdminDueDateCell(container, value) {
        if (!value) {
            appendAdminText(container, '\u2014', { fontSize: '11px', color: 'var(--text-secondary)' });
            return;
        }
        const isPast = new Date(value) < new Date();
        appendAdminText(container, window.formatAdminDate(value, '\u2014'), {
            fontSize: '11px',
            color: isPast ? 'var(--danger-color)' : 'var(--text-secondary)',
            fontWeight: isPast ? '600' : '400'
        });
    }
    // Renders a comma-separated courses string as a series of tag pills.
    // options.maxVisible {number} - maximum pills shown before "+N more" (default 3)
    function renderAdminCoursesCell(container, value, options) {
        const opts = options || {};
        const max = opts.maxVisible || 3;
        const courses = value ? value.split(', ').filter(function (c) { return c.trim(); }) : [];
        if (!courses.length) {
            appendAdminText(container, '\u2014', { color: 'var(--text-secondary)' });
            return;
        }
        const wrapper = $('<div>').addClass('d-flex flex-wrap gap-1').appendTo(container);
        courses.slice(0, max).forEach(function (courseName) {
            $('<span>').addClass('tag-pill tag-pill-xs pill-default').text(courseName).appendTo(wrapper);
        });
        if (courses.length > max) {
            appendAdminText(
                wrapper,
                window.formatAdminCountLabel(courses.length - max, 'more', 'more', '0 more', { prefix: '+' }),
                { fontSize: '11px', color: 'var(--text-secondary)' }
            );
        }
    }
    // ─── Wizard Summary Card Builder ──────────────────────────────────────────────
    // Returns an HTML string for a summary stat card used in wizard review steps.
    // Uses shared admin-summary-* CSS classes defined in admin-wizard.css.
    function buildAdminSummaryCard(value, label, className) {
        return '<div class="admin-summary-item">'
            + '<div class="admin-summary-value ' + (className || '') + '">' + value + '</div>'
            + '<div class="admin-summary-label">' + label + '</div>'
            + '</div>';
    }
    // ─── Excel Export Helpers ─────────────────────────────────────────────────────
    // Returns a filename-safe timestamp suffix string (yyyyMMddHHmm).
    function buildAdminExportTimestampSuffix() {
        var now = new Date();
        return [
            now.getFullYear(),
            String(now.getMonth() + 1).padStart(2, '0'),
            String(now.getDate()).padStart(2, '0'),
            String(now.getHours()).padStart(2, '0'),
            String(now.getMinutes()).padStart(2, '0')
        ].join('');
    }
    // Returns a timestamped .xlsx export filename.
    // identifier is used as the middle segment (e.g., assignment number, category name).
    function buildAdminExportFileName(prefix, identifier) {
        var safe = String(identifier || 'Export').replace(/[\\/:*?"<>|]/g, '_');
        return prefix + '_' + safe + '_' + buildAdminExportTimestampSuffix() + '.xlsx';
    }
    // Triggers download of an ExcelJS workbook as a .xlsx file.
    function saveAdminWorkbook(workbook, fileName) {
        workbook.xlsx.writeBuffer().then(function (buffer) {
            saveAs(new Blob([buffer], { type: 'application/octet-stream' }), fileName);
        });
    }
    // Applies the shared header row style to the first row of an ExcelJS worksheet.
    function styleAdminExcelHeaderRow(ws) {
        var row = ws.getRow(1);
        row.font = { bold: true, size: 11 };
        row.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF5F5F5' } };
        row.alignment = { vertical: 'middle' };
        row.eachCell(function (cell) {
            cell.border = { bottom: { style: 'thin', color: { argb: 'FFD9D9D9' } } };
        });
    }
    // ─── Student Org Filter Initializer ──────────────────────────────────────────
    // Initialises the cascading Division → Department → Section (+ optional Position)
    // filter widgets for a student selection grid.
    //
    // options {object}:
    //   studentsBaseUrl  {string}  Base URL for the Students API (e.g. serviceUrl + '/Students')
    //   gridInstance     {object}  DevExtreme DataGrid instance to filter
    //   divSelector      {string}  Selector for the Division SelectBox  (default '#filter-div')
    //   deptSelector     {string}  Selector for the Department SelectBox (default '#filter-dept')
    //   sectionSelector  {string}  Selector for the Section SelectBox    (default '#filter-section')
    //   positionSelector {string}  Selector for the optional Position TagBox (omit if not used)
    //   clearSelector    {string}  Selector for the clear-filters button  (default '#btn-clear-filter')
    function initAdminStudentOrgFilters(options) {
        var opts = options || {};
        var baseUrl = opts.studentsBaseUrl || (window.serviceUrl + '/Students');
        var grid = opts.gridInstance;
        var divSel = opts.divSelector || '#filter-div';
        var deptSel = opts.deptSelector || '#filter-dept';
        var secSel = opts.sectionSelector || '#filter-section';
        var posSel = opts.positionSelector || null;
        var clearSel = opts.clearSelector || '#btn-clear-filter';
        function makeLookup(endpoint, queryText) {
            return new DevExpress.data.CustomStore({
                key: 'Name',
                load: function () {
                    return $.ajax({
                        url: baseUrl + '/' + endpoint + (queryText || ''),
                        method: 'GET',
                        xhrFields: { withCredentials: true }
                    }).then(function (res) { return res.data || res; });
                }
            });
        }
        function applyFilters() {
            var filters = [];
            var divVal = $(divSel).dxSelectBox('instance').option('value');
            var deptVal = $(deptSel).dxSelectBox('instance').option('value');
            var secVal = $(secSel).dxSelectBox('instance').option('value');
            if (divVal) { filters.push(['Division', '=', divVal]); }
            if (deptVal) { filters.push(['Department', '=', deptVal]); }
            if (secVal) { filters.push(['Section', '=', secVal]); }
            if (posSel) {
                var posVals = $(posSel).dxTagBox('instance').option('value') || [];
                if (posVals.length > 0) {
                    var posFilter = ['Position', '=', posVals[0]];
                    for (var i = 1; i < posVals.length; i++) {
                        posFilter = [posFilter, 'or', ['Position', '=', posVals[i]]];
                    }
                    filters.push(posFilter);
                }
            }
            applyCombinedFilter(grid, filters);
        }
        var divisionBox = $(divSel).dxSelectBox({
            dataSource: makeLookup('GetDivisions'),
            displayExpr: 'Name',
            valueExpr: 'Name',
            placeholder: 'All Divisions',
            showClearButton: true,
            onValueChanged: function (e) {
                var deptBox = $(deptSel).dxSelectBox('instance');
                deptBox.option({
                    value: null,
                    disabled: !e.value,
                    dataSource: e.value
                        ? makeLookup('GetDepartments', '?filter=["Division","=","' + e.value + '"]')
                        : []
                });
                $(secSel).dxSelectBox('instance').option({ value: null, disabled: true, dataSource: [] });
                applyFilters();
            }
        }).dxSelectBox('instance');
        $(deptSel).dxSelectBox({
            displayExpr: 'Name',
            valueExpr: 'Name',
            placeholder: 'All Departments',
            showClearButton: true,
            disabled: true,
            onValueChanged: function (e) {
                var secBox = $(secSel).dxSelectBox('instance');
                var divVal = divisionBox.option('value');
                var q = e.value
                    ? '?filter=[["Division","=","' + divVal + '"],"and",["Department","=","' + e.value + '"]]'
                    : (divVal ? '?filter=["Division","=","' + divVal + '"]' : '');
                secBox.option({
                    value: null,
                    disabled: !e.value,
                    dataSource: e.value ? makeLookup('GetSections', q) : []
                });
                applyFilters();
            }
        });
        $(secSel).dxSelectBox({
            displayExpr: 'Name',
            valueExpr: 'Name',
            placeholder: 'All Sections',
            showClearButton: true,
            disabled: true,
            onValueChanged: applyFilters
        });
        if (posSel) {
            $(posSel).dxTagBox({
                dataSource: makeLookup('GetPositions'),
                displayExpr: 'Name',
                valueExpr: 'Name',
                placeholder: 'All Positions',
                showClearButton: true,
                showSelectionControls: true,
                applyValueMode: 'useButtons',
                onValueChanged: applyFilters
            });
        }
        $(clearSel).on('click', function () {
            divisionBox.option('value', null);
            $(deptSel).dxSelectBox('instance').option({ value: null, disabled: true, dataSource: [] });
            $(secSel).dxSelectBox('instance').option({ value: null, disabled: true, dataSource: [] });
            if (posSel) {
                $(posSel).dxTagBox('instance').option('value', []);
            }
            grid.clearFilter();
        });
    }
    // ─── Course Filter Initializer ────────────────────────────────────────────────
    // Initialises the Type / Division → Category cascading filter widgets for a
    // course selection grid.
    //
    // options {object}:
    //   serviceUrl    {string}  Base API URL (e.g. window.serviceUrl)
    //   gridInstance  {object}  DevExtreme DataGrid instance to filter
    //   typeSel       {string}  Selector for the Course Type SelectBox   (default '#filter-course-type')
    //   divSel        {string}  Selector for the Division SelectBox       (default '#filter-course-div')
    //   catSel        {string}  Selector for the Category SelectBox       (default '#filter-course-cat')
    //   clearSel      {string}  Selector for the clear-filters button     (default '#btn-clear-course-filter')
    function initAdminCourseFilters(options) {
        var opts = options || {};
        var svcUrl = opts.serviceUrl || window.serviceUrl || '';
        var grid = opts.gridInstance;
        var typeSel = opts.typeSel || '#filter-course-type';
        var divSel = opts.divSel || '#filter-course-div';
        var catSel = opts.catSel || '#filter-course-cat';
        var clearSel = opts.clearSel || '#btn-clear-course-filter';
        function applyCourseFilters() {
            var typeVal = $(typeSel).dxSelectBox('instance').option('value');
            var divVal = $(divSel).dxSelectBox('instance').option('value');
            var catVal = $(catSel).dxSelectBox('instance').option('value');
            var conditions = [];
            if (typeVal) { conditions.push(['courseTypeId', '=', typeVal]); }
            if (divVal) { conditions.push(['divisionId', '=', divVal]); }
            if (catVal) { conditions.push(['categoryId', '=', catVal]); }
            applyCombinedFilter(grid, conditions);
        }
        $(typeSel).dxSelectBox({
            dataSource: window.createDataStore(svcUrl, 'admin/CourseTypesCRUD', { key: 'id' }),
            displayExpr: 'name',
            valueExpr: 'id',
            placeholder: 'All Types',
            showClearButton: true,
            onValueChanged: applyCourseFilters
        });
        $(divSel).dxSelectBox({
            dataSource: window.createDataStore(svcUrl, 'admin/DivisionsCRUD', { key: 'id' }),
            displayExpr: 'name',
            valueExpr: 'id',
            placeholder: 'All Divisions',
            showClearButton: true,
            onValueChanged: function (e) {
                var catBox = $(catSel).dxSelectBox('instance');
                catBox.option('value', null);
                if (e.value) {
                    catBox.option('dataSource', window.createDataStore(svcUrl, 'admin/CategoriesCRUD', {
                        key: 'id',
                        action: 'Get',
                        filter: ['divisionId', '=', e.value]
                    }));
                    catBox.option('disabled', false);
                } else {
                    catBox.option({ dataSource: [], disabled: true });
                }
                applyCourseFilters();
            }
        });
        $(catSel).dxSelectBox({
            displayExpr: 'name',
            valueExpr: 'id',
            placeholder: 'All Categories',
            showClearButton: true,
            disabled: true,
            onValueChanged: applyCourseFilters
        });
        $(clearSel).on('click', function () {
            $(typeSel).dxSelectBox('instance').option('value', null);
            $(divSel).dxSelectBox('instance').option('value', null);
            $(catSel).dxSelectBox('instance').option({ value: null, disabled: true, dataSource: [] });
            grid.clearFilter();
        });
    }
    // ─── Exports ──────────────────────────────────────────────────────────────────
    window.escapeHtml = escapeHtml;
    window.getApiErrorMessage = getApiErrorMessage;
    window.applyCombinedFilter = applyCombinedFilter;
    window.appendAdminText = appendAdminText;
    window.truncateAdminText = truncateAdminText;
    window.renderAdminProgressCell = renderAdminProgressCell;
    window.renderAdminStatusCell = renderAdminStatusCell;
    window.renderAdminDueDateCell = renderAdminDueDateCell;
    window.renderAdminCoursesCell = renderAdminCoursesCell;
    window.buildAdminSummaryCard = buildAdminSummaryCard;
    window.buildAdminExportFileName = buildAdminExportFileName;
    window.buildAdminExportTimestampSuffix = buildAdminExportTimestampSuffix;
    window.saveAdminWorkbook = saveAdminWorkbook;
    window.styleAdminExcelHeaderRow = styleAdminExcelHeaderRow;
    window.initAdminStudentOrgFilters = initAdminStudentOrgFilters;
    window.initAdminCourseFilters = initAdminCourseFilters;
})(window, window.jQuery);
