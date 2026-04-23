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
    // ─── Standard Grid Options Builder ──────────────────────────────────────────
    // Returns consistent baseline options for admin DataGrid usage.
    // Pass page-specific settings through the overrides object.
    function buildAdminGridOptions(searchPlaceholder, overrides) {
        var defaults = {
            selection: { mode: 'single' },
            rowAlternationEnabled: false,
            showRowLines: true,
            hoverStateEnabled: true,
            headerFilter: { visible: false },
            searchPanel: {
                visible: true,
                placeholder: searchPlaceholder || 'Search...'
            }
        };

        return $.extend(true, {}, defaults, overrides || {});
    }
    // ─── Generic DOM Helpers ──────────────────────────────────────────────────────
    function appendAdminText(container, text, styles) {
        return $('<span>').css(styles || {}).text(text).appendTo(container);
    }
    const adminGridTextStyleMap = {
        xsMuted: {
            fontSize: 'var(--font-size-xs)',
            color: 'var(--text-secondary)'
        },
        xsMutedFaint: {
            fontSize: 'var(--font-size-xs)',
            color: 'var(--text-secondary)',
            opacity: '.4'
        },
        xsStrong: {
            fontSize: 'var(--font-size-xs)',
            fontWeight: '600',
            color: 'var(--text-primary)'
        },
        xsSemibold: {
            fontSize: 'var(--font-size-xs)',
            fontWeight: '600'
        },
        xsAccent: {
            fontSize: 'var(--font-size-xs)',
            fontWeight: '600',
            color: 'var(--primary-color)'
        },
        captionMuted: {
            fontSize: 'var(--font-size-caption)',
            color: 'var(--text-secondary)'
        },
        captionStrong: {
            fontSize: 'var(--font-size-caption)',
            fontWeight: '600',
            color: 'var(--text-primary)'
        },
        captionAccent: {
            fontSize: 'var(--font-size-caption)',
            fontWeight: '600',
            color: 'var(--primary-color)'
        },
        captionSuccess: {
            fontSize: 'var(--font-size-caption)',
            fontWeight: '600',
            color: 'var(--success-color)'
        },
        captionWarning: {
            fontSize: 'var(--font-size-caption)',
            fontWeight: '600',
            color: 'var(--warning-color)'
        },
        captionDanger: {
            fontSize: 'var(--font-size-caption)',
            fontWeight: '600',
            color: 'var(--danger-color)'
        },
        smStrong: {
            fontSize: 'var(--font-size-sm)',
            fontWeight: '600',
            color: 'var(--text-primary)'
        }
    };
    const adminGridToneConfig = {
        default: { variant: 'captionMuted', dotClass: 'dot-default' },
        primary: { variant: 'captionAccent', dotClass: 'dot-primary' },
        success: { variant: 'captionSuccess', dotClass: 'dot-success' },
        warning: { variant: 'captionWarning', dotClass: 'dot-warning' },
        danger: { variant: 'captionDanger', dotClass: 'dot-danger' }
    };
    function getAdminGridTextStyle(variant, overrides) {
        return $.extend({}, adminGridTextStyleMap[variant] || {}, overrides || {});
    }
    function normalizeAdminGridTone(value) {
        const tone = String(value || '').toLowerCase();
        if (tone.indexOf('danger') >= 0 || tone.indexOf('failed') >= 0) {
            return 'danger';
        }
        if (tone.indexOf('warning') >= 0 || tone.indexOf('incomplete') >= 0 || tone.indexOf('exam') >= 0) {
            return 'warning';
        }
        if (tone.indexOf('success') >= 0 || tone.indexOf('active') >= 0 || tone.indexOf('completed') >= 0 || tone.indexOf('passed') >= 0) {
            return 'success';
        }
        if (tone.indexOf('primary') >= 0 || tone.indexOf('progress') >= 0 || tone.indexOf('learn') >= 0) {
            return 'primary';
        }
        return 'default';
    }
    function renderAdminGridTextCell(container, text, variant, overrides) {
        const resolvedText = text === null || text === undefined || text === '' ? '\u2014' : text;
        return appendAdminText(container, resolvedText, getAdminGridTextStyle(variant || 'captionMuted', overrides));
    }
    function renderAdminGridTruncatedTextCell(container, text, maxLength, variant, options) {
        const opts = options || {};
        const rawText = text === null || text === undefined ? '' : String(text);
        const resolvedText = rawText === '' ? '\u2014' : rawText;
        const rendered = renderAdminGridTextCell(
            container,
            truncateAdminText(resolvedText, maxLength),
            variant,
            opts.styleOverrides
        );

        if (opts.showTitle !== false && resolvedText !== '\u2014' && (opts.alwaysTitle || resolvedText.length > maxLength)) {
            rendered.attr('title', resolvedText);
        }

        return rendered;
    }
    function renderAdminGridStatusCell(container, text, tone, options) {
        const opts = options || {};
        const resolvedTone = normalizeAdminGridTone(tone);
        const toneConfig = adminGridToneConfig[resolvedTone] || adminGridToneConfig.default;
        const resolvedText = text === null || text === undefined || text === '' ? '\u2014' : text;
        const wrapper = $('<span>').addClass('d-inline-flex align-items-center');
        wrapper.css(getAdminGridTextStyle(opts.variant || toneConfig.variant, opts.styleOverrides));
        if (opts.showDot !== false) {
            $('<span>').addClass('status-dot ' + toneConfig.dotClass).appendTo(wrapper);
        }
        if (opts.iconClass) {
            $('<i>').addClass(opts.iconClass + ' me-1').attr('aria-hidden', 'true').appendTo(wrapper);
        }
        wrapper.append(document.createTextNode(resolvedText));
        wrapper.appendTo(container);
        return wrapper;
    }
    function getAdminTypographyPxSize(sizeName, fallback) {
        var value = window.adminTypography && window.adminTypography.size
            ? window.adminTypography.size[sizeName]
            : null;
        var parsed = Number.parseInt(value, 10);
        return Number.isFinite(parsed) ? parsed : fallback;
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
    //   progress: true → warning, false → primary. When omitted the default threshold
    //   of 50 % is used (below 50 % shows warning, at or above shows accent).
    function renderAdminProgressCell(container, value, options) {
        const progress = value || 0;
        const opts = options || {};
        let color;
        if (progress === 100) {
            color = 'var(--success-color)';
        } else if ('useWarningForPartial' in opts) {
            color = progress > 0
                ? (opts.useWarningForPartial ? 'var(--warning-color)' : 'var(--primary-color)')
                : 'var(--border-color)';
        } else {
            color = progress >= 50 ? 'var(--primary-color)' : 'var(--warning-color)';
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
                $('<span>').css(getAdminGridTextStyle('xsMuted', {
                    fontWeight: '600',
                    minWidth: '32px'
                })).text(window.formatAdminPercentage(progress, '0%'))
            )
            .appendTo(container);
    }
    // Renders tagless status text using a status → { tone|cls, text } map.
    function renderAdminStatusCell(container, value, map) {
        const status = (map && map[value]) || { tone: 'default', text: value || '\u2014' };
        renderAdminGridStatusCell(container, status.text, status.tone || status.cls || 'default', {
            showDot: status.showDot !== false,
            variant: status.variant
        });
    }
    // Renders a date cell, highlighting overdue dates in danger color.
    function renderAdminDueDateCell(container, value) {
        if (!value) {
            appendAdminText(container, '\u2014', getAdminGridTextStyle('xsMuted'));
            return;
        }
        const isPast = new Date(value) < new Date();
        appendAdminText(container, window.formatAdminDate(value, '\u2014'), {
            fontSize: 'var(--font-size-xs)',
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
                getAdminGridTextStyle('xsMuted')
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
        row.font = { bold: true, size: getAdminTypographyPxSize('xs', 11) };
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
        var filterInputWidth = '100%';
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
            searchEnabled: true,
            showClearButton: true,
            width: filterInputWidth,
            onValueChanged: function (e) {
                var deptBox = $(deptSel).dxSelectBox('instance');
                deptBox.option({
                    value: null,
                    disabled: !e.value,
                    placeholder: e.value ? 'All Departments' : 'Select Division first',
                    dataSource: e.value
                        ? makeLookup('GetDepartments', '?filter=["Division","=","' + e.value + '"]')
                        : []
                });
                $(secSel).dxSelectBox('instance').option({
                    value: null,
                    disabled: true,
                    placeholder: 'Select Department first',
                    dataSource: []
                });
                applyFilters();
            }
        }).dxSelectBox('instance');
        $(deptSel).dxSelectBox({
            displayExpr: 'Name',
            valueExpr: 'Name',
            placeholder: 'Select Division first',
            showClearButton: true,
            searchEnabled: true,
            width: filterInputWidth,
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
                    placeholder: e.value ? 'All Sections' : 'Select Department first',
                    dataSource: e.value ? makeLookup('GetSections', q) : []
                });
                applyFilters();
            }
        });
        $(secSel).dxSelectBox({
            displayExpr: 'Name',
            valueExpr: 'Name',
            placeholder: 'Select Department first',
            showClearButton: true,
            searchEnabled: true,
            width: filterInputWidth,
            disabled: true,
            onValueChanged: applyFilters
        });
        if (posSel) {
            $(posSel).dxTagBox({
                dataSource: makeLookup('GetPositions'),
                displayExpr: 'Name',
                valueExpr: 'Name',
                placeholder: 'All Positions',
                searchEnabled: true,
                showClearButton: true,
                showSelectionControls: true,
                applyValueMode: 'useButtons',
                width: filterInputWidth,
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
        var filterInputWidth = '100%';
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
            searchEnabled: true,
            showClearButton: true,
            width: filterInputWidth,
            onValueChanged: applyCourseFilters
        });
        $(divSel).dxSelectBox({
            dataSource: window.createDataStore(svcUrl, 'admin/DivisionsCRUD', { key: 'id' }),
            displayExpr: 'name',
            valueExpr: 'id',
            placeholder: 'All Divisions',
            searchEnabled: true,
            showClearButton: true,
            width: filterInputWidth,
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
                    catBox.option('placeholder', 'All Categories');
                } else {
                    catBox.option({ dataSource: [], disabled: true, placeholder: 'Select Division first' });
                }
                applyCourseFilters();
            }
        });
        $(catSel).dxSelectBox({
            displayExpr: 'name',
            valueExpr: 'id',
            placeholder: 'Select Division first',
            showClearButton: true,
            searchEnabled: true,
            width: filterInputWidth,
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
    window.buildAdminGridOptions = buildAdminGridOptions;
    window.appendAdminText = appendAdminText;
    window.getAdminGridTextStyle = getAdminGridTextStyle;
    window.renderAdminGridTextCell = renderAdminGridTextCell;
    window.renderAdminGridTruncatedTextCell = renderAdminGridTruncatedTextCell;
    window.renderAdminGridStatusCell = renderAdminGridStatusCell;
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
