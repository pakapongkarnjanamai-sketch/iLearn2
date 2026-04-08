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
    const defaultGridPageSize = 20;
    const defaultGridMinVisibleRows = defaultGridPageSize;
    const compactGridPageSize = defaultGridPageSize;
    const gridDataRowHeight = 34;
    const gridHeaderRowHeight = 38;
    const gridHeaderPanelHeight = 48;
    const gridPagerHeight = 48;
    const gridSummaryHeight = 40;
    const gridFrameHeight = 2;
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
    const dxGridPresetCssClassMap = {
        defaultGrid: 'admin-grid admin-grid--default',
        compactGrid: 'admin-grid admin-grid--compact',
        selectionGrid: 'admin-grid admin-grid--selection'
    };

    const dxGridPresets = {
        defaultGrid: {
            columnAutoWidth: true,
            showBorders: true,
            rowAlternationEnabled: true,
            showRowLines: true,
            hoverStateEnabled: true,
            scrolling: {
                mode: 'virtual',
                rowRenderingMode: 'virtual'
            },
            paging: { enabled: false, pageSize: defaultGridPageSize },
            pager: {
                visible: false,
                showInfo: true,
                showNavigationButtons: true,
                showPageSizeSelector: false,
                allowedPageSizes: [defaultGridPageSize]
            },
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 300, placeholder: 'Search...' },
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
            onExporting: function (e) {
                handleExporting(e, getRouteExportFileName('Export'));
            }
        },
        compactGrid: {
            columnAutoWidth: true,
            showBorders: true,
            rowAlternationEnabled: true,
            showRowLines: true,
            hoverStateEnabled: true,
            scrolling: {
                mode: 'virtual',
                rowRenderingMode: 'virtual'
            },
            paging: { enabled: false, pageSize: compactGridPageSize },
            pager: {
                visible: false,
                showInfo: true,
                showNavigationButtons: true,
                showPageSizeSelector: false,
                allowedPageSizes: [defaultGridPageSize]
            },
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 240, placeholder: 'Search...' },
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
            onExporting: function (e) {
                handleExporting(e, getRouteExportFileName('Export'));
            }
        }
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
            showCheckBoxesMode: 'always'
        }
    });

    const dxGridDefaults = dxGridPresets.defaultGrid;

    function getDxGridPreset(presetName) {
        return $.extend(true, {}, dxGridPresets[presetName] || dxGridPresets.defaultGrid);
    }

    function getGridChromeHeight(options) {
        const normalizedOptions = options || {};
        const hasHeaderPanel = normalizedOptions.searchPanel?.visible !== false
            || normalizedOptions.export?.enabled === true
            || (normalizedOptions.toolbar && Array.isArray(normalizedOptions.toolbar.items) && normalizedOptions.toolbar.items.length > 0);
        const hasPager = normalizedOptions.paging?.enabled !== false && normalizedOptions.pager?.visible !== false;
        const hasSummary = !!(normalizedOptions.summary
            && ((Array.isArray(normalizedOptions.summary.totalItems) && normalizedOptions.summary.totalItems.length > 0)
                || (Array.isArray(normalizedOptions.summary.groupItems) && normalizedOptions.summary.groupItems.length > 0)));

        return gridFrameHeight
            + gridHeaderRowHeight
            + (hasHeaderPanel ? gridHeaderPanelHeight : 0)
            + (hasPager ? gridPagerHeight : 0)
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
        const hasScrollableOverflow = /(auto|scroll|hidden)/.test(style.overflowY || '');

        return (hasExplicitHeight || hasExplicitMaxHeight) && hasScrollableOverflow;
    }

    function resolveGridViewportMetrics(element, options) {
        const minVisibleRows = getGridMinVisibleRows(options);
        const chromeHeight = getGridChromeHeight(options);

        if (!element || !element.getBoundingClientRect) {
            return {
                pageSize: minVisibleRows,
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
            pageSize: visibleRows,
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
        const messages = {
            courses: 'No courses found.',
            students: 'No students found.',
            resources: 'No resources found.',
            content: 'No content added yet. Click buttons above to add.',
            unusedPublishedResources: 'No unused published resources found.',
            draftResourcesNeeded: 'No draft resources needed by active courses.'
        };

        return messages[key] || fallbackText || 'No data found.';
    }

    function getSelectionHintMarkup(count, itemLabel, actionText) {
        if (count > 0) {
            return `<i class="fas fa-check-circle text-success me-1"></i> <b>${count}</b> ${itemLabel}(s) selected.`;
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
        const currentHeight = gridInstance.option('height');
        const currentPageSize = gridInstance.option('paging.pageSize');

        if (currentHeight !== metrics.height || currentPageSize !== metrics.pageSize) {
            gridInstance.beginUpdate();
            gridInstance.option('paging.pageSize', metrics.pageSize);
            gridInstance.option('pager.visible', true);
            gridInstance.option('height', metrics.height);
            gridInstance.endUpdate();
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

    function createDataStore(baseUrl, controllerName, options) {
        const storeOptions = $.extend(true, {}, {
            key: 'id',
            loadUrl: `${baseUrl}/${controllerName}/Get`,
            insertUrl: `${baseUrl}/${controllerName}/Post`,
            updateUrl: `${baseUrl}/${controllerName}/Put`,
            deleteUrl: `${baseUrl}/${controllerName}/Delete`,
            onBeforeSend: function (method, ajaxOptions) {
                ajaxOptions.xhrFields = { withCredentials: true };
            }
        }, options);

        if (options && options.action && !options.loadUrl) {
            storeOptions.loadUrl = `${baseUrl}/${controllerName}/${options.action}`;
            if (options.ParamKey) {
                storeOptions.loadUrl += `/${options.ParamKey}`;
            }
        }

        return DevExpress.data.AspNet.createStore(storeOptions);
    }

    function handleExporting(e, fileName) {
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet(fileName || 'Sheet1');

        DevExpress.excelExporter.exportDataGrid({
            component: e.component,
            worksheet: worksheet,
            autoFilterEnabled: true
        }).then(function () {
            return workbook.xlsx.writeBuffer();
        }).then(function (buffer) {
            saveAs(new Blob([buffer], { type: 'application/octet-stream' }), `${fileName || 'Data'}.xlsx`);
        });

        e.cancel = true;
    }

    function handlePivotExporting(e, fileName) {
        const workbook = new ExcelJS.Workbook();
        const worksheet = workbook.addWorksheet(fileName || 'Sheet1');

        DevExpress.excelExporter.exportPivotGrid({
            component: e.component,
            worksheet: worksheet
        }).then(function () {
            return workbook.xlsx.writeBuffer();
        }).then(function (buffer) {
            saveAs(new Blob([buffer], { type: 'application/octet-stream' }), `${fileName || 'PivotData'}.xlsx`);
        });

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

        resolvedOptions.paging = $.extend(true, {}, resolvedOptions.paging, {
            enabled: true,
            pageSize: resolvedOptions.paging?.pageSize || defaultGridPageSize
        });
        resolvedOptions.pager = $.extend(true, {}, resolvedOptions.pager, {
            visible: true,
            showPageSizeSelector: false,
            allowedPageSizes: [resolvedOptions.paging.pageSize || defaultGridPageSize]
        });

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
        const enableAutoHeight = !options.height;
        const originalContentReady = options.onContentReady;

        if (enableAutoHeight) {
            const metrics = resolveGridViewportMetrics($target.get(0), options);
            options.height = metrics.height;
            options.paging = $.extend(true, {}, options.paging, { pageSize: metrics.pageSize });
            options.pager = $.extend(true, {}, options.pager, { allowedPageSizes: [metrics.pageSize] });
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

    function showToast(message, type, duration) {
        const toastType = type || 'info';
        const displayTime = duration || 3500;
        const iconMap = {
            success: 'fa-circle-check',
            error: 'fa-circle-xmark',
            warning: 'fa-triangle-exclamation',
            info: 'fa-circle-info'
        };
        const icon = iconMap[toastType] || iconMap.info;

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
            $container.append(
                '<div class="col-md-4 mb-2">' +
                    '<div class="version-card-skeleton">' +
                        '<div class="d-flex justify-content-between align-items-start mb-3">' +
                            '<div style="width:55%">' +
                                '<div class="skeleton skeleton-line w-80 mb-2"></div>' +
                                '<div class="skeleton skeleton-line w-40"></div>' +
                            '</div>' +
                            '<div class="skeleton" style="width:24px;height:24px;border-radius:3px;"></div>' +
                        '</div>' +
                        '<div class="skeleton skeleton-line w-100 mb-1"></div>' +
                        '<div class="skeleton skeleton-line w-60 mb-3"></div>' +
                        '<div class="skeleton skeleton-block" style="height:32px;border-radius:3px;margin-bottom:6px;"></div>' +
                        '<div class="skeleton skeleton-block" style="height:32px;border-radius:3px;"></div>' +
                    '</div>' +
                '</div>'
            );
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

                args[0] = $.extend(true, {}, originalOptions, getGlobalPopupOptions(), {
                    onContentReady: function (e) {
                        if (typeof originalContentReady === 'function') {
                            originalContentReady(e);
                        }

                        window.setTimeout(function () {
                            refreshPopupGridHeights(e.component);
                        }, 0);
                    },
                    onShown: function (e) {
                        if (typeof originalShown === 'function') {
                            originalShown(e);
                        }

                        window.setTimeout(function () {
                            refreshPopupGridHeights(e.component);
                        }, 0);
                    },
                    onResizeEnd: function (e) {
                        if (typeof originalResizeEnd === 'function') {
                            originalResizeEnd(e);
                        }

                        refreshPopupGridHeights(e.component);
                    }
                });
                return originalDxPopup.apply(this, args);
            }

            return originalDxPopup.apply(this, arguments);
        };

        $.fn.dxPopup.__ilearnPopupSized = true;
    }

    const SwalAdmin = typeof window.Swal !== 'undefined'
        ? window.Swal.mixin({
            customClass: {
                popup: 'swal-admin-popup',
                confirmButton: 'btn btn-primary ms-2',
                cancelButton: 'btn btn-outline-secondary'
            },
            buttonsStyling: false
        })
        : null;

    function refreshUserCache() {
        const currentUrl = new URL(window.location.href);
        currentUrl.searchParams.set('_refresh', '1');

        if (!SwalAdmin) {
            window.location.href = currentUrl.toString();
            return;
        }

        SwalAdmin.fire({
            title: 'Refresh user access data?',
            text: 'Reload the latest role and division access from the API for this session.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: '<i class="fas fa-rotate-right me-1"></i>Refresh now',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (result.isConfirmed) {
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
    window.createDataStore = createDataStore;
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
    window.showToast = showToast;
    window.showStatSkeleton = showStatSkeleton;
    window.hideStatSkeleton = hideStatSkeleton;
    window.showCardsSkeleton = showCardsSkeleton;
    window.hideCardsSkeleton = hideCardsSkeleton;
    window.SwalAdmin = SwalAdmin;
    window.refreshUserCache = refreshUserCache;
    window.refreshViewportGridHeights = refreshViewportGridHeights;
})(window, window.jQuery);
