(function (window, $) {
    const config = window.iLearnAdminConfig || {};
    const apiBaseUrl = config.apiBaseUrl || '';
    const toastPosition = { position: 'bottom right', direction: 'up-push' };
    const spinnerIconHtml = '<i class="fas fa-spinner fa-spin me-1"></i>';
    const gridResizeNamespace = '.ilearnGridViewport';
    const autoHeightGridSelector = '[data-grid-auto-height="true"]';
    const minGridHeight = 320;
    const gridBottomGap = 24;
    const minGridPageSize = 5;
    const defaultGridMinVisibleRows = 20;
    const gridDataRowHeight = 34;
    const gridHeaderRowHeight = 38;
    const gridHeaderPanelHeight = 48;
    const gridSummaryHeight = 40;
    const gridFrameHeight = 2;
    const maxRemoteTakePerRequest = 200;
    const popupRefreshDelay = 0;
    const defaultToastDisplayTime = 3500;
    const noDataMessages = {
        courses: 'No courses found.',
        students: 'No students found.',
        resources: 'No resources found.',
        content: 'No content added yet. Click buttons above to add.',
        unusedPublishedResources: 'No unused published resources found.',
        draftResourcesNeeded: 'No draft resources needed by active courses.'
    };
    const toastIconMap = {
        success: 'fa-circle-check',
        error: 'fa-circle-xmark',
        warning: 'fa-triangle-exclamation',
        info: 'fa-circle-info'
    };
    const dialogIconMap = {
        question: 'fa-circle-question',
        warning: 'fa-triangle-exclamation',
        error: 'fa-circle-xmark',
        success: 'fa-circle-check',
        info: 'fa-circle-info'
    };
    const dialogIconColorMap = {
        question: 'var(--accent-color)',
        warning: 'var(--warning-color)',
        error: 'var(--danger-color)',
        success: 'var(--success-color)',
        info: 'var(--accent-color)'
    };
    const sharedGridPreset = {
        width: '100%',
        height: '100%',
        autoHeight: true,
        columnAutoWidth: true,
        showBorders: true,
        rowAlternationEnabled: true,
        showRowLines: true,
        hoverStateEnabled: true,
        remoteOperations: true,
        scrolling: {
            mode: 'virtual',
            rowRenderingMode: 'virtual'
        },
        headerFilter: { visible: true },
        loadPanel: {
            enabled: true,
            text: 'Loading data...',
            showPane: true,
            showIndicator: true,
            shadingColor: 'rgba(250,250,250,0.7)',
            shading: true,
            position: { of: 'window' }
        },
        export: {
            enabled: true,
            allowExportSelectedData: true
        },
        onExporting: createGridExportHandler('Export')
    };
    const cardsSkeletonMarkup = [
        '<div class="col-md-4 mb-2">',
            '<div class="version-card-skeleton">',
                '<div class="d-flex justify-content-between align-items-start mb-3">',
                    '<div style="width:55%">',
                        '<div class="skeleton skeleton-line w-80 mb-2"></div>',
                        '<div class="skeleton skeleton-line w-40"></div>',
                    '</div>',
                    '<div class="skeleton" style="width:24px;height:24px;border-radius:3px;"></div>',
                '</div>',
                '<div class="skeleton skeleton-line w-100 mb-1"></div>',
                '<div class="skeleton skeleton-line w-60 mb-3"></div>',
                '<div class="skeleton skeleton-block" style="height:32px;border-radius:3px;margin-bottom:6px;"></div>',
                '<div class="skeleton skeleton-block" style="height:32px;border-radius:3px;"></div>',
            '</div>',
        '</div>'
    ].join('');
    const adminTypography = {
        family: 'var(--font-stack)',
        size: {
            caption: '11px',
            sm: '12px',
            md: '13px',
            lg: '15px',
            xl: '20px',
            display: '28px',
            gridHeader: '11px',
            gridCell: '12px'
        },
        weight: {
            medium: 500,
            semibold: 600,
            bold: 700
        }
    };
    const adminDateLocale = 'en-GB';
    const adminDateDisplayFormat = 'dd/MM/yyyy';
    const adminDateTimeDisplayFormat = 'dd/MM/yyyy HH:mm';
    const adminNumberLocale = adminDateLocale;
    const adminFileSizeUnits = ['B', 'KB', 'MB', 'GB', 'TB'];
    const adminTimeOptions = {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    };
    const dxGridPresetCssClassMap = {
        defaultGrid: 'admin-grid admin-grid--default',
        compactGrid: 'admin-grid admin-grid--compact',
        selectionGrid: 'admin-grid admin-grid--selection'
    };

    const dxGridPresets = {
        defaultGrid: $.extend(true, {}, sharedGridPreset, {
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 300, placeholder: 'Search...' }
        }),
        compactGrid: $.extend(true, {}, sharedGridPreset, {
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 240, placeholder: 'Search...' }
        })
    };

    dxGridPresets.selectionGrid = $.extend(true, {}, dxGridPresets.compactGrid, {
        export: {
            enabled: false,
            allowExportSelectedData: false
        },
        searchPanel: {
            visible: true,
            width: 250,
            placeholder: 'Search...'
        },
        selection: {
            mode: 'multiple',
            showCheckBoxesMode: 'always',
            selectAllMode: 'page'
        }
    });

    const dxGridDefaults = dxGridPresets.defaultGrid;

    function getDxGridPreset(presetName) {
        return $.extend(true, {}, dxGridPresets[presetName] || dxGridPresets.defaultGrid);
    }

    function createGridExportHandler(fallbackName) {
        return function (e) {
            handleExporting(e, getRouteExportFileName(fallbackName));
        };
    }

    function normalizeAdminDateValue(value) {
        if (value === undefined || value === null || value === '') {
            return null;
        }

        const date = value instanceof Date ? value : new Date(value);
        return Number.isNaN(date.getTime()) ? null : date;
    }

    function normalizeAdminNumberValue(value) {
        if (value === undefined || value === null || value === '') {
            return null;
        }

        if (typeof value === 'number') {
            return Number.isFinite(value) ? value : null;
        }

        const normalizedValue = Number(String(value).replace(/,/g, ''));
        return Number.isFinite(normalizedValue) ? normalizedValue : null;
    }

    function formatAdminNumber(value, fallback, options) {
        const number = normalizeAdminNumberValue(value);
        if (number === null) {
            return fallback !== undefined ? fallback : '—';
        }

        return number.toLocaleString(adminNumberLocale, options);
    }

    function formatAdminInteger(value, fallback) {
        return formatAdminNumber(value, fallback, {
            maximumFractionDigits: 0
        });
    }

    function formatAdminPercentage(value, fallback, options) {
        const number = normalizeAdminNumberValue(value);
        if (number === null) {
            return fallback !== undefined ? fallback : '—';
        }

        const precision = Number.isInteger(options?.precision) ? options.precision : 0;
        const suffix = options?.suffix !== undefined ? options.suffix : '%';
        const formatted = formatAdminNumber(number, fallback, {
            minimumFractionDigits: precision,
            maximumFractionDigits: precision
        });

        return `${formatted}${suffix}`;
    }

    function formatAdminFileSize(value, fallback, options) {
        const bytes = normalizeAdminNumberValue(value);
        if (bytes === null || bytes <= 0) {
            return fallback !== undefined ? fallback : '—';
        }

        let unitIndex = 0;
        let size = bytes;
        while (size >= 1024 && unitIndex < adminFileSizeUnits.length - 1) {
            size /= 1024;
            unitIndex += 1;
        }

        const precision = Number.isInteger(options?.precision)
            ? options.precision
            : (unitIndex === 0 ? 0 : 1);

        return `${formatAdminNumber(size, fallback, {
            minimumFractionDigits: precision,
            maximumFractionDigits: precision
        })} ${adminFileSizeUnits[unitIndex]}`;
    }

    function formatAdminCountLabel(value, singularLabel, pluralLabel, fallback, options) {
        const number = normalizeAdminNumberValue(value);
        if (number === null) {
            return fallback !== undefined ? fallback : '—';
        }

        const rounded = Math.round(number);
        const resolvedPluralLabel = pluralLabel || `${singularLabel}s`;
        const label = rounded === 1 ? singularLabel : resolvedPluralLabel;
        const prefix = options?.prefix || '';

        return `${prefix}${formatAdminInteger(rounded, fallback)} ${label}`;
    }

    function formatAdminDuration(value, fallback, options) {
        const totalSeconds = normalizeAdminNumberValue(value);
        if (totalSeconds === null || totalSeconds <= 0) {
            return fallback !== undefined ? fallback : '—';
        }

        const wholeSeconds = Math.floor(totalSeconds);
        const hours = Math.floor(wholeSeconds / 3600);
        const minutes = Math.floor((wholeSeconds % 3600) / 60);
        const seconds = Math.floor(wholeSeconds % 60);
        const includeSeconds = options?.includeSeconds === true;

        if (hours > 0) {
            return `${formatAdminInteger(hours, '0')}h ${formatAdminInteger(minutes, '0')}m`;
        }

        if (minutes > 0) {
            return includeSeconds
                ? `${formatAdminInteger(minutes, '0')}m ${formatAdminInteger(seconds, '0')}s`
                : `${formatAdminInteger(minutes, '0')}m`;
        }

        return `${formatAdminInteger(seconds, '0')}s`;
    }

    function formatAdminRelativeTime(value, fallback) {
        const date = normalizeAdminDateValue(value);
        if (!date) {
            return fallback !== undefined ? fallback : '—';
        }

        const diffMs = Date.now() - date.getTime();
        const diffMinutes = Math.floor(diffMs / 60000);

        if (diffMinutes < 1) {
            return 'just now';
        }

        if (diffMinutes < 60) {
            return `${formatAdminInteger(diffMinutes, '0')} min ago`;
        }

        const diffHours = Math.floor(diffMinutes / 60);
        if (diffHours < 24) {
            return `${formatAdminInteger(diffHours, '0')} hr ago`;
        }

        const diffDays = Math.floor(diffHours / 24);
        if (diffDays < 7) {
            return formatAdminCountLabel(diffDays, 'day ago', 'days ago', fallback);
        }

        return formatAdminDateTime(date, fallback);
    }

    function formatAdminDate(value, fallback) {
        const date = normalizeAdminDateValue(value);
        if (!date) {
            return fallback !== undefined ? fallback : '—';
        }

        return date.toLocaleDateString(adminDateLocale);
    }

    function formatAdminTime(value, fallback) {
        const date = normalizeAdminDateValue(value);
        if (!date) {
            return fallback !== undefined ? fallback : '—';
        }

        return date.toLocaleTimeString(adminDateLocale, adminTimeOptions);
    }

    function formatAdminDateTime(value, fallback) {
        const date = normalizeAdminDateValue(value);
        if (!date) {
            return fallback !== undefined ? fallback : '—';
        }

        return `${formatAdminDate(date)} ${formatAdminTime(date, '')}`.trim();
    }

    function hasToolbarItems(toolbar) {
        return !!(toolbar && Array.isArray(toolbar.items) && toolbar.items.length > 0);
    }

    function hasSummaryItems(summary) {
        if (!summary) {
            return false;
        }

        const hasTotalItems = Array.isArray(summary.totalItems) && summary.totalItems.length > 0;
        const hasGroupItems = Array.isArray(summary.groupItems) && summary.groupItems.length > 0;

        return hasTotalItems || hasGroupItems;
    }

    function getGridChromeHeight(options) {
        const normalizedOptions = options || {};
        const hasHeaderPanel = normalizedOptions.searchPanel?.visible !== false
            || normalizedOptions.export?.enabled === true
            || hasToolbarItems(normalizedOptions.toolbar);
        const hasSummary = hasSummaryItems(normalizedOptions.summary);

        return gridFrameHeight
            + gridHeaderRowHeight
            + (hasHeaderPanel ? gridHeaderPanelHeight : 0)
            + (hasSummary ? gridSummaryHeight : 0);
    }

    function getGridMinVisibleRows(options) {
        const configuredMinVisibleRows = Number(options?.minVisibleRows);

        if (Number.isFinite(configuredMinVisibleRows) && configuredMinVisibleRows > 0) {
            return Math.max(minGridPageSize, Math.floor(configuredMinVisibleRows));
        }

        return defaultGridMinVisibleRows;
    }

    function shouldConstrainToParentPanel(parentPanel) {
        if (!parentPanel) {
            return false;
        }

        if (parentPanel.matches('.dx-popup-content, .dx-overlay-content, .tab-pane')) {
            return true;
        }

        const style = window.getComputedStyle(parentPanel);
        const hasExplicitHeight = style.height && style.height !== 'auto';
        const hasExplicitMaxHeight = style.maxHeight && style.maxHeight !== 'none';
        const hasScrollableOverflow = /(auto|scroll|hidden|clip)/.test(style.overflowY || '')
            || /(auto|scroll|hidden|clip)/.test(style.overflow || '');
        const usesStretchHeight = parentPanel.classList.contains('h-100')
            || parentPanel.classList.contains('vh-100');
        const isStructuredPanel = parentPanel.matches('.border.rounded.bg-white, .dash-panel, .panel, .card');

        if (hasScrollableOverflow) {
            return hasExplicitHeight || hasExplicitMaxHeight || usesStretchHeight;
        }

        return isStructuredPanel && (hasExplicitHeight || hasExplicitMaxHeight || usesStretchHeight);
    }

    function resolveGridViewportMetrics(element, options) {
        const minVisibleRows = getGridMinVisibleRows(options);
        const chromeHeight = getGridChromeHeight(options);

        if (!element || !element.getBoundingClientRect) {
            return {
                height: chromeHeight + (minVisibleRows * gridDataRowHeight)
            };
        }

        const rect = element.getBoundingClientRect();
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight || 900;
        let availableHeight = viewportHeight - rect.top - gridBottomGap;

        const parentPanel = element.closest('.dx-popup-content, .dx-overlay-content, .border.rounded.bg-white, .dash-panel, .panel, .tab-pane, .container-fluid');
        const isParentConstrained = shouldConstrainToParentPanel(parentPanel);
        if (isParentConstrained && parentPanel.getBoundingClientRect) {
            const parentRect = parentPanel.getBoundingClientRect();
            const parentAvailableHeight = parentRect.bottom - rect.top - 16;
            if (parentAvailableHeight > 0) {
                availableHeight = Math.min(availableHeight, parentAvailableHeight);
            }
        }

        const effectiveMinVisibleRows = isParentConstrained ? minGridPageSize : minVisibleRows;
        const minimumHeight = chromeHeight + (effectiveMinVisibleRows * gridDataRowHeight);

        availableHeight = Math.max(minGridHeight, minimumHeight, Math.floor(availableHeight));

        const visibleRows = Math.max(
            effectiveMinVisibleRows,
            Math.floor((availableHeight - chromeHeight) / gridDataRowHeight)
        );

        return {
            height: chromeHeight + (visibleRows * gridDataRowHeight)
        };
    }

    function getDataGridInstance(element) {
        if (!element) {
            return null;
        }

        if (window.DevExpress?.ui?.dxDataGrid?.getInstance) {
            return window.DevExpress.ui.dxDataGrid.getInstance(element);
        }

        const $element = $(element);
        return $element.data('dxDataGrid') || null;
    }

    function getNoDataText(key, fallbackText) {
        return noDataMessages[key] || fallbackText || 'No data found.';
    }

    function getSelectionHintMarkup(count, itemLabel, actionText) {
        if (count > 0) {
            return `<i class="fas fa-check-circle text-success me-1"></i> <b>${formatAdminCountLabel(count, itemLabel)}</b> selected.`;
        }

        return `<i class="fas fa-info-circle text-primary me-1"></i> Check the rows ${actionText}.`;
    }

    function createSelectionHintElement(id, itemLabel, actionText) {
        return $('<div>')
            .attr('id', id)
            .addClass('text-muted small fw-medium ps-1 d-flex align-items-center')
            .html(getSelectionHintMarkup(0, itemLabel, actionText));
    }

    function setSelectionHint(selector, count, itemLabel, actionText) {
        $(selector).html(getSelectionHintMarkup(count, itemLabel, actionText));
    }

    function getRouteExportFileName(fallbackName) {
        const segments = window.location.pathname.replace(/^\//, '').split('/').filter(Boolean);
        return segments.join('_') || fallbackName;
    }

    function normalizeGridSize(value, fallbackValue) {
        if (value === undefined || value === null || value === '') {
            return fallbackValue;
        }

        return typeof value === 'number' ? `${value}px` : value;
    }

    function setGridHostSize($target, dimensions) {
        if (!$target || !$target.length) {
            return;
        }

        const width = normalizeGridSize(dimensions?.width, '100%');
        const height = normalizeGridSize(dimensions?.height, null);

        $target.css({
            width: width,
            minWidth: 0
        });

        if (height !== null) {
            $target.css('height', height);
            $target.data('gridHostHeight', height);
        }
    }

    function applyViewportGridHeight(gridInstance) {
        if (!gridInstance || typeof gridInstance.element !== 'function') {
            return;
        }

        const $element = gridInstance.element();
        const element = $element && $element.get ? $element.get(0) : null;
        const isAutoHeightGrid = $element.data('gridAutoHeight') === true
            || $element.attr('data-grid-auto-height') === 'true';

        if (!element || !isAutoHeightGrid) {
            return;
        }

        const metrics = resolveGridViewportMetrics(element, gridInstance.option());
        const currentHeight = $element.data('gridHostHeight') || element.style.height;
        const nextHeight = normalizeGridSize(metrics.height, null);

        if (currentHeight !== nextHeight) {
            setGridHostSize($element, { height: metrics.height });
            gridInstance.updateDimensions();
        }
    }

    function refreshViewportGridHeights() {
        $(autoHeightGridSelector).each(function () {
            const instance = getDataGridInstance(this);
            if (instance) {
                applyViewportGridHeight(instance);
            }
        });
    }

    function refreshPopupGridHeights(popupComponent) {
        if (!popupComponent || typeof popupComponent.content !== 'function') {
            refreshViewportGridHeights();
            return;
        }

        const $content = popupComponent.content();
        if (!$content || !$content.find) {
            refreshViewportGridHeights();
            return;
        }

        $content.find(autoHeightGridSelector).each(function () {
            const instance = getDataGridInstance(this);
            if (instance) {
                applyViewportGridHeight(instance);
            }
        });
    }

    function registerGridResizeHandler() {
        if ($(window).data('gridResizeBound')) {
            return;
        }

        let resizeTimer = null;
        $(window).on(`resize${gridResizeNamespace}`, function () {
            window.clearTimeout(resizeTimer);
            resizeTimer = window.setTimeout(refreshViewportGridHeights, 80);
        });
        $(window).data('gridResizeBound', true);
    }

    function capRemoteTake(ajaxOptions, maxTake) {
        var limit = maxTake || maxRemoteTakePerRequest;
        if (ajaxOptions.data && typeof ajaxOptions.data.take === 'number' && ajaxOptions.data.take > limit) {
            ajaxOptions.data.take = limit;
        }
    }

    function createDataStore(baseUrl, controllerName, options) {
        var callerOnBeforeSend = options && typeof options.onBeforeSend === 'function'
            ? options.onBeforeSend
            : null;

        const storeOptions = $.extend(true, {}, {
            key: 'id',
            loadUrl: `${baseUrl}/${controllerName}/Get`,
            insertUrl: `${baseUrl}/${controllerName}/Post`,
            updateUrl: `${baseUrl}/${controllerName}/Put`,
            deleteUrl: `${baseUrl}/${controllerName}/Delete`
        }, options, {
            onBeforeSend: function (method, ajaxOptions) {
                ajaxOptions.xhrFields = { withCredentials: true };
                capRemoteTake(ajaxOptions);
                if (callerOnBeforeSend) {
                    callerOnBeforeSend(method, ajaxOptions);
                }
            }
        });

        if (options && options.action && !options.loadUrl) {
            storeOptions.loadUrl = `${baseUrl}/${controllerName}/${options.action}`;
            if (options.ParamKey) {
                storeOptions.loadUrl += `/${options.ParamKey}`;
            }
        }

        return DevExpress.data.AspNet.createStore(storeOptions);
    }

    function exportComponentToWorkbook(exporter, component, fileName, worksheetName, options) {
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet(worksheetName || 'Sheet1');

        exporter($.extend(true, {
            component: component,
            worksheet: worksheet
        }, options)).then(function () {
            return workbook.xlsx.writeBuffer();
        }).then(function (buffer) {
            saveAs(new Blob([buffer], { type: 'application/octet-stream' }), fileName);
        });
    }

    function handleExporting(e, fileName) {
        exportComponentToWorkbook(
            DevExpress.excelExporter.exportDataGrid,
            e.component,
            `${fileName || 'Data'}.xlsx`,
            fileName || 'Sheet1',
            { autoFilterEnabled: true }
        );

        e.cancel = true;
    }

    function handlePivotExporting(e, fileName) {
        exportComponentToWorkbook(
            DevExpress.excelExporter.exportPivotGrid,
            e.component,
            `${fileName || 'PivotData'}.xlsx`,
            fileName || 'Sheet1'
        );

        e.cancel = true;
    }

    function getDxGridOptions(pageOptions) {
        const overrides = $.extend(true, {}, pageOptions || {});
        const presetName = overrides.preset || 'defaultGrid';
        delete overrides.preset;
        const resolvedOptions = $.extend(true, {}, getDxGridPreset(presetName), overrides);

        resolvedOptions.scrolling = $.extend(true, {}, resolvedOptions.scrolling, {
            mode: 'virtual',
            rowRenderingMode: 'virtual'
        });

        resolvedOptions.paging = { enabled: true, pageSize: 30 };
        delete resolvedOptions.pager;

        return resolvedOptions;
    }

    function mergeCssClasses() {
        const tokens = [];

        Array.prototype.slice.call(arguments).forEach(function (value) {
            if (!value || typeof value !== 'string') {
                return;
            }

            value.split(/\s+/).forEach(function (token) {
                if (token && tokens.indexOf(token) === -1) {
                    tokens.push(token);
                }
            });
        });

        return tokens.join(' ');
    }

    function applyGridPresetClasses(options, presetName) {
        const normalizedPreset = dxGridPresetCssClassMap[presetName] ? presetName : 'defaultGrid';
        const nextOptions = $.extend(true, {}, options);

        nextOptions.elementAttr = $.extend(true, {}, nextOptions.elementAttr);
        nextOptions.elementAttr.class = mergeCssClasses(
            nextOptions.elementAttr.class,
            dxGridPresetCssClassMap[normalizedPreset]
        );

        return nextOptions;
    }

    function resolveGridTarget(selector) {
        if (selector && selector.jquery) {
            return selector;
        }

        if (selector instanceof window.Element) {
            return $(selector);
        }

        return $(selector);
    }

    function initDxGrid(selector, pageOptions) {
        const $target = resolveGridTarget(selector);
        const presetName = pageOptions && pageOptions.preset ? pageOptions.preset : 'defaultGrid';
        const options = applyGridPresetClasses(getDxGridOptions(pageOptions), presetName);
        const hasExplicitHeight = !!(pageOptions && Object.prototype.hasOwnProperty.call(pageOptions, 'height'));
        const enableAutoHeight = options.autoHeight !== false && !hasExplicitHeight;
        const originalContentReady = options.onContentReady;
        const requestedWidth = options.width;
        const requestedHeight = options.height;

        delete options.autoHeight;
        options.width = requestedWidth || '100%';
        options.height = hasExplicitHeight ? requestedHeight : '100%';

        if (!enableAutoHeight) {
            setGridHostSize($target, {
                width: requestedWidth,
                height: requestedHeight || '100%'
            });
        }

        if (enableAutoHeight) {
            const metrics = resolveGridViewportMetrics($target.get(0), options);
            setGridHostSize($target, {
                width: requestedWidth,
                height: metrics.height
            });
            options.onContentReady = function (e) {
                applyViewportGridHeight(e.component);
                if (typeof originalContentReady === 'function') {
                    originalContentReady(e);
                }
            };
        }

        const instance = $target.dxDataGrid(options).dxDataGrid('instance');

        if (enableAutoHeight) {
            instance.element().data('gridAutoHeight', true);
            instance.element().attr('data-grid-auto-height', 'true');
            registerGridResizeHandler();
            applyViewportGridHeight(instance);
        }

        return instance;
    }

    function setButtonLoading(button, loading, loadingText) {
        const $button = $(button);

        if (loading) {
            $button.data('original-html', $button.html())
                .prop('disabled', true)
                .html(spinnerIconHtml + (loadingText || 'Processing...'));
            return;
        }

        const originalHtml = $button.data('original-html');
        $button.prop('disabled', false)
            .html(originalHtml || $button.html());
    }

    function escapeHtml(value) {
        return $('<div>').text(value == null ? '' : String(value)).html();
    }

    function normalizeDialogButtonType(type) {
        if (type === 'danger' || type === 'success' || type === 'default') {
            return type;
        }

        return 'normal';
    }

    function buildDialogMessageContent(options) {
        const dialogOptions = options || {};
        const iconName = dialogOptions.icon === false ? null : (dialogIconMap[dialogOptions.icon] || dialogIconMap.info);
        const iconColor = dialogIconColorMap[dialogOptions.icon] || dialogIconColorMap.info;
        const bodyHtml = dialogOptions.messageHtml
            || (dialogOptions.text
                ? `<div class="u-text-secondary-60">${escapeHtml(dialogOptions.text).replace(/\n/g, '<br>')}</div>`
                : '');

        if (!iconName) {
            return `<div class="text-start dialog-body-sm">${bodyHtml}</div>`;
        }

        return `
            <div class="d-flex align-items-start gap-3 text-start dialog-body-sm">
                <div class="flex-shrink-0" style="font-size:22px; color:${iconColor}; line-height:1;">
                    <i class="fas ${iconName}"></i>
                </div>
                <div class="flex-grow-1">${bodyHtml}</div>
            </div>`;
    }

    function createAdminDialogController(options) {
        const dialogOptions = options || {};
        const contentId = `ilearn-dialog-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
        const dialog = DevExpress.ui.dialog.custom({
            title: dialogOptions.title || '',
            showTitle: dialogOptions.showTitle !== false,
            messageHtml: `<div id="${contentId}">${dialogOptions.messageHtml || dialogOptions.text || ''}</div>`,
            dragEnabled: false,
            closeOnOutsideClick: false,
            showCloseButton: dialogOptions.showCloseButton === true,
            width: dialogOptions.width,
            buttons: dialogOptions.buttons || []
        });

        dialog.show();

        return {
            hide: function () {
                if (typeof dialog.hide === 'function') {
                    dialog.hide();
                }
            },
            find: function (selector) {
                return $('#' + contentId).find(selector);
            },
            setHtml: function (html) {
                $('#' + contentId).html(html);
            }
        };
    }

    function showAdminConfirmDialog(options) {
        const dialogOptions = options || {};
        const buttons = [];

        if (dialogOptions.showCancelButton !== false) {
            buttons.push({
                text: dialogOptions.cancelButtonText || 'Cancel',
                type: 'normal',
                onClick: function () {
                    return false;
                }
            });
        }

        buttons.push({
            text: dialogOptions.confirmButtonText || 'Confirm',
            type: normalizeDialogButtonType(dialogOptions.confirmType),
            onClick: function () {
                return true;
            }
        });

        return DevExpress.ui.dialog.custom({
            title: dialogOptions.title || 'Confirm',
            messageHtml: buildDialogMessageContent(dialogOptions),
            buttons: buttons,
            dragEnabled: false,
            closeOnOutsideClick: false,
            showCloseButton: false,
            width: dialogOptions.width
        }).show();
    }

    function showToast(message, type, duration) {
        const toastType = type || 'info';
        const displayTime = duration || defaultToastDisplayTime;
        const icon = toastIconMap[toastType] || toastIconMap.info;

        DevExpress.ui.notify({
            type: toastType,
            height: 45,
            width: 'auto',
            minWidth: 220,
            displayTime: displayTime,
            animation: {
                show: { type: 'fade', duration: 400, from: 0, to: 1 },
                hide: { type: 'fade', duration: 40, to: 0 }
            },
            contentTemplate: function (element) {
                $(element)
                    .addClass('dx-toast-content-custom')
                    .html(`<i class="fas ${icon} me-2"></i><span>${message}</span>`);
            }
        }, toastPosition);
    }

    function showStatSkeleton(ids) {
        (ids || []).forEach(function (id) {
            const $element = $('#' + id);
            $element.data('original-text', $element.text())
                .html('<span class="skeleton ds-val-skeleton"></span>');
        });
    }

    function hideStatSkeleton(ids) {
        (ids || []).forEach(function (id) {
            const $element = $('#' + id);
            const originalText = $element.data('original-text');
            if (originalText !== undefined) {
                $element.text(originalText);
            }
        });
    }

    function showCardsSkeleton(selector, count) {
        const total = count || 3;
        const $container = $(selector).empty();

        for (let index = 0; index < total; index += 1) {
            $container.append(cardsSkeletonMarkup);
        }
    }

    function hideCardsSkeleton(selector) {
        $(selector).empty();
    }

    function getGlobalPopupOptions() {
        const isMobile = window.innerWidth < 768;
        const width = isMobile ? 'calc(100vw - 24px)' : '80vw';
        const height = isMobile ? 'calc(100vh - 24px)' : '80vh';

        return {
            width: width,
            height: height,
            maxWidth: width,
            maxHeight: height
        };
    }

    function schedulePopupGridRefresh(component) {
        window.setTimeout(function () {
            refreshPopupGridHeights(component);
        }, popupRefreshDelay);
    }

    function invokeHandler(handler, eventArgs) {
        if (typeof handler === 'function') {
            handler(eventArgs);
        }
    }

    function applyGlobalDxPopupSizing() {
        if (!$.fn || typeof $.fn.dxPopup !== 'function' || $.fn.dxPopup.__ilearnPopupSized) {
            return;
        }

        const originalDxPopup = $.fn.dxPopup;

        $.fn.dxPopup = function () {
            if (arguments.length > 0 && arguments[0] && typeof arguments[0] === 'object' && !Array.isArray(arguments[0])) {
                const args = Array.prototype.slice.call(arguments);
                const originalOptions = args[0];
                const originalShown = originalOptions.onShown;
                const originalResizeEnd = originalOptions.onResizeEnd;
                const originalContentReady = originalOptions.onContentReady;
                const shouldPreservePopupHeight = originalOptions.disableGlobalHeightSizing === true;
                const baseOptions = shouldPreservePopupHeight
                    ? $.extend(true, {}, originalOptions, {
                        width: originalOptions.width || getGlobalPopupOptions().width,
                        maxWidth: originalOptions.maxWidth || getGlobalPopupOptions().maxWidth
                    })
                    : $.extend(true, {}, originalOptions, getGlobalPopupOptions());

                args[0] = $.extend(true, {}, baseOptions, {
                    onContentReady: function (e) {
                        invokeHandler(originalContentReady, e);
                        schedulePopupGridRefresh(e.component);
                    },
                    onShown: function (e) {
                        invokeHandler(originalShown, e);
                        schedulePopupGridRefresh(e.component);
                    },
                    onResizeEnd: function (e) {
                        invokeHandler(originalResizeEnd, e);

                        refreshPopupGridHeights(e.component);
                    }
                });
                return originalDxPopup.apply(this, args);
            }

            return originalDxPopup.apply(this, arguments);
        };

        $.fn.dxPopup.__ilearnPopupSized = true;
    }

    function refreshUserCache() {
        const currentUrl = new URL(window.location.href);
        currentUrl.searchParams.set('_refresh', '1');

        showAdminConfirmDialog({
            title: 'Refresh user access data?',
            text: 'Reload the latest role and division access from the API for this session.',
            icon: 'question',
            confirmButtonText: 'Refresh now',
            cancelButtonText: 'Cancel',
            confirmType: 'default'
        }).done(function (confirmed) {
            if (confirmed) {
                window.location.href = currentUrl.toString();
            }
        });
    }

    $(function () {
        applyGlobalDxPopupSizing();

        $(document).on('click', '[data-admin-refresh-user]', function (event) {
            event.preventDefault();
            refreshUserCache();
        });
    });

    window.API_BASE = apiBaseUrl;
    window.serviceUrl = apiBaseUrl;
    window.ADMIN_DATE_LOCALE = adminDateLocale;
    window.ADMIN_DATE_DISPLAY_FORMAT = adminDateDisplayFormat;
    window.ADMIN_DATETIME_DISPLAY_FORMAT = adminDateTimeDisplayFormat;
    window.ADMIN_NUMBER_LOCALE = adminNumberLocale;
    window.createDataStore = createDataStore;
    window.capRemoteTake = capRemoteTake;
    window.handleExporting = handleExporting;
    window.handlePivotExporting = handlePivotExporting;
    window.dxGridDefaults = dxGridDefaults;
    window.dxGridPresets = dxGridPresets;
    window.getDxGridPreset = getDxGridPreset;
    window.getNoDataText = getNoDataText;
    window.createSelectionHintElement = createSelectionHintElement;
    window.setSelectionHint = setSelectionHint;
    window.adminTypography = adminTypography;
    window.getDxGridOptions = getDxGridOptions;
    window.initDxGrid = initDxGrid;
    window.setButtonLoading = setButtonLoading;
    window.escapeAdminHtml = escapeHtml;
    window.createAdminDialogController = createAdminDialogController;
    window.showAdminConfirmDialog = showAdminConfirmDialog;
    window.showToast = showToast;
    window.showStatSkeleton = showStatSkeleton;
    window.hideStatSkeleton = hideStatSkeleton;
    window.showCardsSkeleton = showCardsSkeleton;
    window.hideCardsSkeleton = hideCardsSkeleton;
    window.refreshUserCache = refreshUserCache;
    window.refreshViewportGridHeights = refreshViewportGridHeights;
    window.formatAdminDate = formatAdminDate;
    window.formatAdminTime = formatAdminTime;
    window.formatAdminDateTime = formatAdminDateTime;
    window.formatAdminNumber = formatAdminNumber;
    window.formatAdminInteger = formatAdminInteger;
    window.formatAdminPercentage = formatAdminPercentage;
    window.formatAdminFileSize = formatAdminFileSize;
    window.formatAdminCountLabel = formatAdminCountLabel;
    window.formatAdminDuration = formatAdminDuration;
    window.formatAdminRelativeTime = formatAdminRelativeTime;
})(window, window.jQuery);
