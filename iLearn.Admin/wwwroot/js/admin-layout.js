(function (window, $) {
    const config = window.iLearnAdminConfig || {};
    const apiBaseUrl = config.apiBaseUrl || '';
    const clearAllCacheUrl = config.clearAllCacheUrl || '';
    const toastPosition = { position: 'bottom right', direction: 'up-push' };
    const spinnerIconHtml = '<i class="fas fa-spinner fa-spin me-1"></i>';
    const gridResizeNamespace = '.ilearnGridViewport';
    const managedGridSelector = '.admin-grid';
    const gridLoadBoundDataKey = 'gridLoadBound';
    const gridResizeBoundDataKey = 'gridResizeBound';
    const defaultGridPageSize = 30;
    const gridResizeRefreshDelay = 80;
    const gridPostRenderRefreshDelay = 120;
    const popupRefreshDelay = 0;
    const defaultToastDisplayTime = 3500;
    let viewportLayoutCounter = 0;
    const exportDependencyScripts = [
        '/js/devextreme/dx-exceljs-fork.min.js',
        '/js/devextreme/filesaver.min.js'
    ];
    const html2CanvasScriptPath = '/lib/html2canvas/html2canvas.min.js';
    let exportDependenciesPromise = null;
    let html2CanvasPromise = null;
    const adminDialogDefaults = {
        dragEnabled: false,
        closeOnOutsideClick: false
    };
    const noDataMessages = {
        courses: 'No courses found.',
        learners: 'No learners found.',
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
        question: 'var(--primary-color)',
        warning: 'var(--warning-color)',
        error: 'var(--danger-color)',
        success: 'var(--success-color)',
        info: 'var(--primary-color)'
    };
    const sharedGridPreset = {
        width: '100%',
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
            micro: '10px',      // --font-size-micro
            xs: '11px',         // --font-size-xs
            caption: '12px',    // --font-size-caption
            sm: '13px',         // --font-size-sm
            md: '16px',         // --font-size-md
            lg: '18px',         // --font-size-lg
            xl: '22px',        // --font-size-xl
            display: '28px',    // --font-size-display
            gridHeader: '12px', // --font-size-grid-header
            gridCell: '13px'    // --font-size-grid-cell
        },
        weight: {
            normal: 400,
            medium: 500,
            semibold: 600,
            bold: 700
        },
        letterSpacing: {
            tight: '0.2px',     // --letter-spacing-tight
            normal: '0.4px',    // --letter-spacing-normal
            wide: '0.6px'       // --letter-spacing-wide
        }
    };
    const adminTokenFallbackMap = {
        '--surface-base': '#ffffff',
        '--success-color': '#52c41a',
        '--warning-color': '#faad14',
        '--danger-color': '#ff4d4f',
        '--primary-mid': '#1677ff',
        '--border-strong': '#d9d9d9',
        '--text-secondary': '#595959',
        '--text-tertiary': '#8c8c8c'
    };
    const adminChartPalettePresets = {
        status3: ['--success-color', '--warning-color', '--border-strong'],
        status3Brand: ['--success-color', '--primary-mid', '--border-strong'],
        status4: ['--success-color', '--warning-color', '--border-strong', '--danger-color'],
        status4Brand: ['--success-color', '--primary-mid', '--warning-color', '--danger-color']
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
        selectionGrid: 'admin-grid admin-grid--selection',
        wizardSelectionGrid: 'admin-grid admin-grid--selection',
        wizardSelectionContinuousGrid: 'admin-grid admin-grid--selection'
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

    dxGridPresets.wizardSelectionGrid = $.extend(true, {}, dxGridPresets.selectionGrid, {
        columnAutoWidth: false,
        rowAlternationEnabled: false,
        headerFilter: { visible: false },
        filterRow: { visible: true },
        searchPanel: { visible: false },
        remoteOperations: true,
        scrolling: {
            mode: 'virtual',
            rowRenderingMode: 'virtual'
        },
        paging: {
            enabled: true,
            pageSize: defaultGridPageSize
        },
        pager: {
            visible: false,
            showPageSizeSelector: false,
            showInfo: true,
            showNavigationButtons: true
        },
        selection: {
            mode: 'multiple',
            showCheckBoxesMode: 'always',
            selectAllMode: 'allPages'
        }
    });

    dxGridPresets.wizardSelectionContinuousGrid = $.extend(true, {}, dxGridPresets.wizardSelectionGrid, {
        // Backward-compatible alias for wizard pages already using this preset name.
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
        const resolvedSingularLabel = typeof singularLabel === 'string' && singularLabel.trim()
            ? singularLabel.trim()
            : 'item';
        const resolvedPluralLabel = typeof pluralLabel === 'string' && pluralLabel.trim()
            ? pluralLabel.trim()
            : `${resolvedSingularLabel}`;
        const label = rounded === 1 ? resolvedSingularLabel : resolvedPluralLabel;
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

    function getAdminCssToken(tokenName, fallback) {
        if (!tokenName) {
            return fallback || '';
        }

        const normalizedTokenName = tokenName.startsWith('--') ? tokenName : `--${tokenName}`;
        const computedValue = window.getComputedStyle(document.documentElement)
            .getPropertyValue(normalizedTokenName)
            .trim();

        if (computedValue) {
            return computedValue;
        }

        if (fallback !== undefined) {
            return fallback;
        }

        return adminTokenFallbackMap[normalizedTokenName] || '';
    }

    function getAdminChartPalette(presetName) {
        const resolvedPresetName = adminChartPalettePresets[presetName] ? presetName : 'status3';
        const paletteTokens = adminChartPalettePresets[resolvedPresetName];

        return paletteTokens.map(function (tokenName) {
            return getAdminCssToken(tokenName, adminTokenFallbackMap[tokenName]);
        });
    }

    function getAdminExportBackgroundColor() {
        return getAdminCssToken('--surface-base', adminTokenFallbackMap['--surface-base']);
    }

    function buildAdminCenterTemplateStyles(overrides) {
        const options = overrides || {};

        return {
            label: {
                fontSize: options.labelFontSize || adminTypography.size.xs,
                fill: options.labelFill || 'var(--text-tertiary)',
                fontWeight: options.labelFontWeight || adminTypography.weight.semibold,
                textTransform: options.labelTextTransform || 'uppercase',
                letterSpacing: options.labelLetterSpacing || adminTypography.letterSpacing.normal
            },
            value: {
                fontSize: options.valueFontSize || adminTypography.size.xl,
                fill: options.valueFill || 'var(--success-color)',
                fontWeight: options.valueFontWeight || adminTypography.weight.bold
            }
        };
    }

    function buildAdminSvgTextStyle(styleOptions) {
        return [
            `font-size:${styleOptions.fontSize}`,
            `fill:${styleOptions.fill}`,
            `font-weight:${styleOptions.fontWeight}`,
            styleOptions.textTransform ? `text-transform:${styleOptions.textTransform}` : '',
            styleOptions.letterSpacing ? `letter-spacing:${styleOptions.letterSpacing}` : ''
        ].filter(Boolean).join(';') + ';';
    }

    function appendAdminCenterTemplateText(container, options) {
        const settings = options || {};
        const styles = buildAdminCenterTemplateStyles(settings.styles);
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'g');

        const label = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        label.setAttribute('text-anchor', 'middle');
        label.setAttribute('y', settings.labelY || '-6');
        label.setAttribute('style', buildAdminSvgTextStyle(styles.label));
        label.textContent = settings.labelText || '';

        const value = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        value.setAttribute('text-anchor', 'middle');
        value.setAttribute('y', settings.valueY || '18');
        value.setAttribute('style', buildAdminSvgTextStyle(styles.value));
        value.textContent = settings.valueText || '';

        svg.appendChild(label);
        svg.appendChild(value);

        // DevExtreme may provide an SVG renderer wrapper instead of a raw DOM node.
        const hostElement = container && container.nodeType
            ? container
            : container && container.jquery
                ? container.get(0)
                : container && container[0] && container[0].nodeType
                    ? container[0]
                    : null;

        if (hostElement && typeof hostElement.appendChild === 'function') {
            hostElement.appendChild(svg);
            return;
        }

        if (container && typeof container.append === 'function') {
            container.append(svg);
        }
    }

    function createAdminChartCenterTemplateCallback(options) {
        const settings = options || {};

        return function (container, size) {
            window.appendAdminCenterTemplateText(container, {
                labelText: settings.labelText || '',
                valueText: settings.valueText || '',
                labelY: settings.labelY,
                valueY: settings.valueY,
                styles: settings.styles
            });
        };
    }

    function createAdminPieChartConfig(options) {
        const config = options || {};
        const palettePreset = config.palettePreset || 'status3';
        const baseChartConfig = config.baseConfig || {};

        return $.extend(true, {}, baseChartConfig, {
            palette: getAdminChartPalette(palettePreset),
            centerTemplate: createAdminChartCenterTemplateCallback({
                labelText: config.centerLabel || '',
                valueText: config.centerValue || '',
                labelY: config.centerLabelY,
                valueY: config.centerValueY,
                styles: config.centerStyles
            })
        });
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

    function getAllPagesSelectionHintMarkup(count, itemLabel, emptyActionText) {
        if (count > 0) {
            return `<i class="fas fa-check-circle text-success me-1"></i> <b>${formatAdminCountLabel(count, itemLabel)}</b> selected across all pages.`;
        }

        const actionText = emptyActionText || 'to continue';
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

    function setAllPagesSelectionHint(selector, count, itemLabel, emptyActionText) {
        $(selector).html(getAllPagesSelectionHintMarkup(count, itemLabel, emptyActionText));
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
            return;
        }

        $target.css('height', '');
        $target.removeData('gridHostHeight');
    }

    function getViewportHeight() {
        return window.visualViewport?.height || window.innerHeight || document.documentElement.clientHeight || 0;
    }

    function refreshViewportLayoutTargets(targets) {
        const normalizedTargets = Array.isArray(targets)
            ? targets
            : (targets ? [targets] : []);

        normalizedTargets.forEach(function (target) {
            if (!target) {
                return;
            }

            if (typeof target === 'function') {
                target();
                return;
            }

            if (typeof target.updateDimensions === 'function') {
                target.updateDimensions();
            }
        });
    }

    function enableAdminViewportLayout(options) {
        const settings = $.extend(true, {
            layoutSelector: '.admin-viewport-layout',
            contentSelector: '.admin-responsive-content',
            pageHeaderSelector: '.page-header',
            minWidth: 768,
            refreshTargets: []
        }, options || {});

        const layout = document.querySelector(settings.layoutSelector);
        if (!layout) {
            return null;
        }

        const content = layout.querySelector(settings.contentSelector);
        const pageHeader = document.querySelector(settings.pageHeaderSelector);
        const mediaQuery = window.matchMedia(`(min-width: ${settings.minWidth}px)`);
        const namespace = `.ilearnViewportLayout${viewportLayoutCounter += 1}`;
        let resizeObserver = null;

        const refreshLayout = function () {
            if (!mediaQuery.matches) {
                document.body.classList.remove('admin-viewport-layout-active');
                layout.style.removeProperty('--admin-viewport-layout-height');
                layout.style.removeProperty('--admin-viewport-content-height');
                refreshViewportLayoutTargets(settings.refreshTargets);
                return;
            }

            document.body.classList.add('admin-viewport-layout-active');

            const layoutRect = layout.getBoundingClientRect();
            const viewportHeight = getViewportHeight();
            const layoutStyles = window.getComputedStyle(layout);
            const layoutPaddingBottom = parseFloat(layoutStyles.paddingBottom) || 0;
            const availableHeight = Math.max(0, Math.floor(viewportHeight - layoutRect.top));

            layout.style.setProperty('--admin-viewport-layout-height', `${availableHeight}px`);

            if (content) {
                const contentRect = content.getBoundingClientRect();
                const contentHeight = Math.max(0, Math.floor(viewportHeight - contentRect.top - layoutPaddingBottom));
                layout.style.setProperty('--admin-viewport-content-height', `${contentHeight}px`);
            }

            refreshViewportLayoutTargets(settings.refreshTargets);
        };

        const scheduleRefresh = function () {
            window.requestAnimationFrame(refreshLayout);
        };

        $(window).on(`resize${namespace}`, scheduleRefresh);

        if (window.visualViewport) {
            window.visualViewport.addEventListener('resize', scheduleRefresh);
        }

        if (typeof mediaQuery.addEventListener === 'function') {
            mediaQuery.addEventListener('change', scheduleRefresh);
        } else if (typeof mediaQuery.addListener === 'function') {
            mediaQuery.addListener(scheduleRefresh);
        }

        if (typeof ResizeObserver === 'function' && pageHeader) {
            resizeObserver = new ResizeObserver(scheduleRefresh);
            resizeObserver.observe(pageHeader);
        }

        refreshLayout();

        return {
            refresh: refreshLayout,
            dispose: function () {
                $(window).off(namespace);

                if (window.visualViewport) {
                    window.visualViewport.removeEventListener('resize', scheduleRefresh);
                }

                if (typeof mediaQuery.removeEventListener === 'function') {
                    mediaQuery.removeEventListener('change', scheduleRefresh);
                } else if (typeof mediaQuery.removeListener === 'function') {
                    mediaQuery.removeListener(scheduleRefresh);
                }

                if (resizeObserver) {
                    resizeObserver.disconnect();
                }

                layout.style.removeProperty('--admin-viewport-layout-height');
                layout.style.removeProperty('--admin-viewport-content-height');
                document.body.classList.remove('admin-viewport-layout-active');
            }
        };
    }

    function toggleAdminViewportLayout(controller, isEnabled, options) {
        if (isEnabled) {
            if (!controller) {
                return enableAdminViewportLayout(options);
            }

            controller.refresh();
            return controller;
        }

        if (controller) {
            controller.dispose();
        }

        return null;
    }

    function refreshGridDimensions(gridInstance) {
        if (!gridInstance || typeof gridInstance.updateDimensions !== 'function') {
            return;
        }

        gridInstance.updateDimensions();
    }

    function refreshViewportGridHeights() {
        $(managedGridSelector).each(function () {
            const instance = getDataGridInstance(this);
            if (instance) {
                refreshGridDimensions(instance);
            }
        });
    }

    function registerWindowScopedHandler(dataKey, eventName, handler) {
        const $window = $(window);
        if ($window.data(dataKey)) {
            return;
        }

        $window.on(`${eventName}${gridResizeNamespace}`, handler);
        $window.data(dataKey, true);
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

        $content.find(managedGridSelector).each(function () {
            const instance = getDataGridInstance(this);
            if (instance) {
                refreshGridDimensions(instance);
            }
        });
    }

    function scheduleViewportGridHeightRefresh(gridInstance) {
        if (!gridInstance) {
            return;
        }

        window.requestAnimationFrame(function () {
            refreshGridDimensions(gridInstance);
        });

        window.setTimeout(function () {
            refreshGridDimensions(gridInstance);
        }, gridPostRenderRefreshDelay);
    }

    function registerGridLoadHandler() {
        registerWindowScopedHandler(gridLoadBoundDataKey, 'load', function () {
            window.setTimeout(refreshViewportGridHeights, 0);
        });
    }

    function registerGridResizeHandler() {
        let resizeTimer = null;
        registerWindowScopedHandler(gridResizeBoundDataKey, 'resize', function () {
            window.clearTimeout(resizeTimer);
            resizeTimer = window.setTimeout(refreshViewportGridHeights, gridResizeRefreshDelay);
        });
    }

    function createDataStore(baseUrl, controllerName, options) {
        const normalizedOptions = options || {};
        const callerOnBeforeSend = typeof normalizedOptions.onBeforeSend === 'function'
            ? normalizedOptions.onBeforeSend
            : null;

        const storeOptions = $.extend(true, {}, {
            key: 'id',
            loadUrl: `${baseUrl}/${controllerName}/Get`,
            insertUrl: `${baseUrl}/${controllerName}/Post`,
            updateUrl: `${baseUrl}/${controllerName}/Put`,
            deleteUrl: `${baseUrl}/${controllerName}/Delete`
        }, normalizedOptions, {
            onBeforeSend: function (method, ajaxOptions) {
                ajaxOptions.xhrFields = { withCredentials: true };
                if (callerOnBeforeSend) {
                    callerOnBeforeSend(method, ajaxOptions);
                }
            }
        });

        if (normalizedOptions.action && !normalizedOptions.loadUrl) {
            storeOptions.loadUrl = `${baseUrl}/${controllerName}/${normalizedOptions.action}`;
            if (normalizedOptions.ParamKey) {
                storeOptions.loadUrl += `/${normalizedOptions.ParamKey}`;
            }
        }

        return DevExpress.data.AspNet.createStore(storeOptions);
    }

    function loadScriptOnce(scriptPath) {
        return new Promise(function (resolve, reject) {
            const selector = `script[src="${scriptPath}"]`;
            const existing = document.querySelector(selector);

            if (existing) {
                if (existing.dataset.loaded === 'true') {
                    resolve();
                    return;
                }

                existing.addEventListener('load', function onLoad() {
                    existing.dataset.loaded = 'true';
                    existing.removeEventListener('load', onLoad);
                    resolve();
                }, { once: true });

                existing.addEventListener('error', function onError() {
                    existing.removeEventListener('error', onError);
                    reject(new Error(`Failed to load script: ${scriptPath}`));
                }, { once: true });
                return;
            }

            const script = document.createElement('script');
            script.src = scriptPath;
            script.async = true;

            script.addEventListener('load', function () {
                script.dataset.loaded = 'true';
                resolve();
            }, { once: true });

            script.addEventListener('error', function () {
                reject(new Error(`Failed to load script: ${scriptPath}`));
            }, { once: true });

            document.head.appendChild(script);
        });
    }

    function ensureExportDependencies() {
        if (window.ExcelJS && window.saveAs) {
            return Promise.resolve();
        }

        if (exportDependenciesPromise) {
            return exportDependenciesPromise;
        }

        exportDependenciesPromise = Promise.all(exportDependencyScripts.map(loadScriptOnce))
            .then(function () {
                if (!window.ExcelJS || !window.saveAs) {
                    throw new Error('Export dependencies are unavailable after loading scripts.');
                }
            })
            .catch(function (error) {
                exportDependenciesPromise = null;
                throw error;
            });

        return exportDependenciesPromise;
    }

    function ensureHtml2Canvas() {
        if (window.html2canvas) {
            return Promise.resolve(window.html2canvas);
        }

        if (html2CanvasPromise) {
            return html2CanvasPromise;
        }

        html2CanvasPromise = loadScriptOnce(html2CanvasScriptPath)
            .then(function () {
                if (!window.html2canvas) {
                    throw new Error('html2canvas is unavailable after loading script.');
                }

                return window.html2canvas;
            })
            .catch(function (error) {
                html2CanvasPromise = null;
                throw error;
            });

        return html2CanvasPromise;
    }

    function exportComponentToWorkbook(exporter, component, fileName, worksheetName, options) {
        return ensureExportDependencies().then(function () {
            const workbook = new ExcelJS.Workbook();
            const worksheet = workbook.addWorksheet(worksheetName || 'Sheet1');

            return exporter($.extend(true, {
                component: component,
                worksheet: worksheet
            }, options)).then(function () {
                return workbook.xlsx.writeBuffer();
            }).then(function (buffer) {
                saveAs(new Blob([buffer], { type: 'application/octet-stream' }), fileName);
            });
        });
    }

    function handleExporting(e, fileName) {
        e.cancel = true;

        exportComponentToWorkbook(
            DevExpress.excelExporter.exportDataGrid,
            e.component,
            `${fileName || 'Data'}.xlsx`,
            fileName || 'Sheet1',
            { autoFilterEnabled: true }
        ).catch(function () {
            showToast('Unable to export right now. Please try again.', 'error');
        });
    }

    function handlePivotExporting(e, fileName) {
        e.cancel = true;

        exportComponentToWorkbook(
            DevExpress.excelExporter.exportPivotGrid,
            e.component,
            `${fileName || 'PivotData'}.xlsx`,
            fileName || 'Sheet1'
        ).catch(function () {
            showToast('Unable to export right now. Please try again.', 'error');
        });
    }

    function getDxGridOptions(pageOptions) {
        const normalizedPageOptions = pageOptions || {};
        const overrides = $.extend(true, {}, normalizedPageOptions);
        const presetName = overrides.preset || 'defaultGrid';
        delete overrides.preset;
        const resolvedOptions = $.extend(true, {}, getDxGridPreset(presetName), overrides);

        // Allow callers to opt out of virtual scrolling by providing their own scrolling.mode
        // (e.g. wizard selection grids that use pager instead).
        const callerScrollMode = normalizedPageOptions.scrolling?.mode || resolvedOptions.scrolling?.mode;
        if (!callerScrollMode) {
            resolvedOptions.scrolling = $.extend(true, {}, resolvedOptions.scrolling, {
                mode: 'virtual',
                rowRenderingMode: 'virtual'
            });
        }

        // Allow callers to override paging (e.g. pageSize: 15 for wizard grids).
        const callerPaging = normalizedPageOptions.paging || resolvedOptions.paging;
        if (!callerPaging) {
            resolvedOptions.paging = { enabled: true, pageSize: defaultGridPageSize };
            delete resolvedOptions.pager;
        }

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
        const normalizedPageOptions = pageOptions || {};
        const presetName = normalizedPageOptions.preset || 'defaultGrid';
        const options = applyGridPresetClasses(getDxGridOptions(pageOptions), presetName);
        const hasExplicitHeight = Object.prototype.hasOwnProperty.call(normalizedPageOptions, 'height');
        const requestedWidth = options.width;
        const requestedHeight = options.height;

        options.width = requestedWidth || '100%';
        delete options.autoHeight;

        if (hasExplicitHeight) {
            options.height = requestedHeight;
        } else {
            options.height = '100%';
        }

        setGridHostSize($target, {
            width: requestedWidth,
            height: hasExplicitHeight ? requestedHeight : null
        });

        const instance = $target.dxDataGrid(options).dxDataGrid('instance');

        registerGridResizeHandler();
        registerGridLoadHandler();
        scheduleViewportGridHeightRefresh(instance);

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
            .html(originalHtml || $button.html())
            .removeData('original-html');
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
        const dialog = DevExpress.ui.dialog.custom($.extend(true, {}, adminDialogDefaults, {
            title: dialogOptions.title || '',
            showTitle: dialogOptions.showTitle !== false,
            messageHtml: `<div id="${contentId}">${dialogOptions.messageHtml || dialogOptions.text || ''}</div>`,
            showCloseButton: dialogOptions.showCloseButton === true,
            width: dialogOptions.width,
            buttons: dialogOptions.buttons || []
        }));

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

        return DevExpress.ui.dialog.custom($.extend(true, {}, adminDialogDefaults, {
            title: dialogOptions.title || 'Confirm',
            messageHtml: buildDialogMessageContent(dialogOptions),
            buttons: buttons,
            showCloseButton: false,
            width: dialogOptions.width
        })).show();
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
                $element.text(originalText).removeData('original-text');
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

    function resolvePopupSizingOptions(originalOptions) {
        const globalPopupOptions = getGlobalPopupOptions();

        if (originalOptions.disableGlobalHeightSizing === true) {
            return $.extend(true, {}, originalOptions, {
                width: originalOptions.width || globalPopupOptions.width,
                maxWidth: originalOptions.maxWidth || globalPopupOptions.maxWidth
            });
        }

        return $.extend(true, {}, originalOptions, globalPopupOptions);
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
                const baseOptions = resolvePopupSizingOptions(originalOptions);

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

    function clearAllCache(triggerButton) {
        const $trigger = $(triggerButton);
        const currentUrl = new URL(window.location.href);
        currentUrl.searchParams.delete('_refresh');

        showAdminConfirmDialog({
            title: 'Clear all cached data?',
            text: 'This clears Admin and API in-memory caches, then reloads the current page.',
            icon: 'warning',
            confirmButtonText: 'Clear cache',
            cancelButtonText: 'Cancel',
            confirmType: 'danger'
        }).done(function (confirmed) {
            if (!confirmed) {
                return;
            }

            if (!clearAllCacheUrl) {
                showToast('Cache clear endpoint is not configured.', 'error', 4000);
                return;
            }

            setButtonLoading($trigger, true, 'Clearing cache...');

            $.ajax({
                url: clearAllCacheUrl,
                type: 'POST'
            }).done(function (response) {
                showToast(response && response.message ? response.message : 'All cached data cleared.', 'success', 1500);

                window.setTimeout(function () {
                    window.location.href = currentUrl.toString();
                }, 180);
            }).fail(function (xhr) {
                const message = xhr && xhr.responseJSON && xhr.responseJSON.message
                    ? xhr.responseJSON.message
                    : 'Could not clear cached data.';
                showToast(message, 'error', 4000);
            }).always(function () {
                setButtonLoading($trigger, false);
            });
        });
    }

    $(function () {
        applyGlobalDxPopupSizing();

        $(document).on('click', '[data-admin-clear-cache]', function (event) {
            event.preventDefault();
            clearAllCache(this);
        });
    });

    window.API_BASE = apiBaseUrl;
    window.serviceUrl = apiBaseUrl;
    window.ADMIN_DATE_LOCALE = adminDateLocale;
    window.ADMIN_DATE_DISPLAY_FORMAT = adminDateDisplayFormat;
    window.ADMIN_DATETIME_DISPLAY_FORMAT = adminDateTimeDisplayFormat;
    window.ADMIN_NUMBER_LOCALE = adminNumberLocale;
    window.createDataStore = createDataStore;
    window.ensureExportDependencies = ensureExportDependencies;
    window.handleExporting = handleExporting;
    window.handlePivotExporting = handlePivotExporting;
    window.dxGridDefaults = dxGridDefaults;
    window.dxGridPresets = dxGridPresets;
    window.getDxGridPreset = getDxGridPreset;
    window.getNoDataText = getNoDataText;
    window.createSelectionHintElement = createSelectionHintElement;
    window.setSelectionHint = setSelectionHint;
    window.setAllPagesSelectionHint = setAllPagesSelectionHint;
    window.adminTypography = adminTypography;
    window.getDxGridOptions = getDxGridOptions;
    window.initDxGrid = initDxGrid;
    window.setButtonLoading = setButtonLoading;
    window.escapeAdminHtml = escapeHtml;
    window.createAdminDialogController = createAdminDialogController;
    window.showAdminConfirmDialog = showAdminConfirmDialog;
    window.showToast = showToast;
    window.ensureHtml2Canvas = ensureHtml2Canvas;
    window.showStatSkeleton = showStatSkeleton;
    window.hideStatSkeleton = hideStatSkeleton;
    window.showCardsSkeleton = showCardsSkeleton;
    window.hideCardsSkeleton = hideCardsSkeleton;
    window.clearAllCache = clearAllCache;
    window.refreshUserCache = clearAllCache;
    window.refreshViewportGridHeights = refreshViewportGridHeights;
    window.enableAdminViewportLayout = enableAdminViewportLayout;
    window.toggleAdminViewportLayout = toggleAdminViewportLayout;
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
    window.getAdminCssToken = getAdminCssToken;
    window.getAdminChartPalette = getAdminChartPalette;
    window.getAdminExportBackgroundColor = getAdminExportBackgroundColor;
    window.appendAdminCenterTemplateText = appendAdminCenterTemplateText;
    window.createAdminChartCenterTemplateCallback = createAdminChartCenterTemplateCallback;
    window.createAdminPieChartConfig = createAdminPieChartConfig;
})(window, window.jQuery);
