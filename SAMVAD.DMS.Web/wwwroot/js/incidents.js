const incidentsModule = (function () {
    const state = {
        page: 1,
        pageSize: 20,
        searchTerm: '',
        status: '',
        disasterType: '',
        totalPages: 1,
        totalCount: 0,
        hasPreviousPage: false,
        hasNextPage: false
    };

    function parseNullableInt(value) {
        if (value === null || value === undefined || value === '') {
            return null;
        }

        const parsed = parseInt(value, 10);
        return Number.isNaN(parsed) ? null : parsed;
    }

    function antiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function notifySuccess(message) {
        if (window.toastr) {
            window.toastr.success(message || 'Success');
            return;
        }
        console.log(message || 'Success');
    }

    function notifyError(message) {
        if (window.toastr) {
            window.toastr.error(message || 'Something went wrong');
            return;
        }
        console.error(message || 'Something went wrong');
    }

    function extractErrorMessage(xhr, fallbackMessage) {
        const raw = xhr && xhr.responseText ? xhr.responseText : '';
        if (!raw) {
            return fallbackMessage;
        }

        try {
            const parsed = JSON.parse(raw);
            return (parsed && parsed.message) || fallbackMessage;
        } catch (error) {
            return fallbackMessage;
        }
    }

    function readFiltersFromUi() {
        state.searchTerm = ($('#incidentsSearchInput').val() || '').toString().trim();
        state.status = ($('#incidentsStatusFilter').val() || '').toString().trim();
        state.disasterType = ($('#incidentsTypeFilter').val() || '').toString().trim();
        state.pageSize = parseInt(($('#incidentsPageSize').val() || '20').toString(), 10) || 20;
    }

    function writeStateToUi() {
        $('#incidentsSearchInput').val(state.searchTerm);
        $('#incidentsStatusFilter').val(state.status);
        $('#incidentsTypeFilter').val(state.disasterType);
        $('#incidentsPageSize').val(state.pageSize.toString());
    }

    function updatePaginationUi() {
        const safeTotalPages = state.totalPages > 0 ? state.totalPages : 1;
        const currentPage = state.totalPages > 0 ? state.page : 1;
        $('#incidentsPaginationInfo').text('Page ' + currentPage + ' of ' + safeTotalPages + ' (' + state.totalCount + ' total)');
        $('#incidentsPrevPageBtn').prop('disabled', !state.hasPreviousPage);
        $('#incidentsNextPageBtn').prop('disabled', !state.hasNextPage);
    }

    function syncPaginationFromListMeta() {
        const $meta = $('#incidentsListMeta');
        if (!$meta.length) {
            state.totalPages = 1;
            state.totalCount = 0;
            state.hasPreviousPage = false;
            state.hasNextPage = false;
            updatePaginationUi();
            return;
        }

        state.page = parseInt($meta.data('page'), 10) || state.page;
        state.pageSize = parseInt($meta.data('pageSize'), 10) || state.pageSize;
        state.totalCount = parseInt($meta.data('totalCount'), 10) || 0;
        state.totalPages = parseInt($meta.data('totalPages'), 10) || 0;
        state.hasPreviousPage = ($meta.data('hasPrevious') || '').toString() === 'true';
        state.hasNextPage = ($meta.data('hasNext') || '').toString() === 'true';
        updatePaginationUi();
    }

    function setDetailContent(html) {
        $('#incidentDetailPaneContent').html(html);
    }

    function openMobileDetailPane() {
        const $pane = $('#incidentDetailHost');
        if (!$pane.length || window.innerWidth >= 768) {
            return;
        }

        $pane.addClass('is-open');
        $('body').addClass('overflow-hidden');
    }

    function closeMobileDetailPane() {
        const $pane = $('#incidentDetailHost');
        if (!$pane.length) {
            return;
        }

        $pane.removeClass('is-open');
        $('body').removeClass('overflow-hidden');
    }

    function openExportModal() {
        const $modal = $('#exportCsvModal');
        if (!$modal.length) {
            return;
        }

        $('#exportSearchTerm').val(state.searchTerm);
        $('#exportStatus').val(state.status);
        $('#exportDisasterType').val(state.disasterType);
        $modal.removeClass('hidden').addClass('flex');
        $('body').addClass('overflow-hidden');
    }

    function closeExportModal() {
        const $modal = $('#exportCsvModal');
        if (!$modal.length) {
            return;
        }

        $modal.addClass('hidden').removeClass('flex');
        $('body').removeClass('overflow-hidden');
    }

    function resetExportModalFilters() {
        const form = document.getElementById('exportCsvForm');
        if (!form) {
            return;
        }

        form.reset();
        $('#exportSearchTerm').val(state.searchTerm || '');
        $('#exportStatus').val(state.status || '');
        $('#exportDisasterType').val(state.disasterType || '');
    }

    function loadList(page) {
        if (typeof page === 'number' && page > 0) {
            state.page = page;
        }

        readFiltersFromUi();
        $.ajax({
            url: '/Incidents/List',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({
                page: state.page,
                pageSize: state.pageSize,
                searchTerm: state.searchTerm,
                status: parseNullableInt(state.status),
                disasterType: parseNullableInt(state.disasterType)
            }),
            success: function (html) {
                $('#incidentsListHost').html(html);
                syncPaginationFromListMeta();
            },
            error: function () {
                notifyError('Unable to load incidents.');
            }
        });
    }

    function loadDetail(id) {
        $.ajax({
            url: '/Incidents/Detail?id=' + encodeURIComponent(id),
            type: 'GET',
            success: function (html) {
                setDetailContent(html);
                openMobileDetailPane();
            },
            error: function () {
                notifyError('Unable to load incident details.');
            }
        });
    }

    function loadPane(url) {
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                setDetailContent(html);
                openMobileDetailPane();

                if ($.validator && $.validator.unobtrusive) {
                    const $forms = $('#incidentDetailPaneContent form');
                    $forms.each(function () {
                        $(this).removeData('validator');
                        $(this).removeData('unobtrusiveValidation');
                        $.validator.unobtrusive.parse($(this));
                    });
                }
            },
            error: function () {
                notifyError('Unable to load panel.');
            }
        });
    }

    function isFormValid(formSelector) {
        const $form = $(formSelector);
        if (!$form.length) {
            return false;
        }

        if ($.validator && $.validator.unobtrusive) {
            $form.removeData('validator');
            $form.removeData('unobtrusiveValidation');
            $.validator.unobtrusive.parse($form);
        }

        if ($.validator && $form.data('validator')) {
            return $form.valid();
        }

        return true;
    }

    function submitOnBehalf() {
        if (!isFormValid('#submitIncidentOnBehalfForm')) {
            return;
        }

        const payload = $('#submitIncidentOnBehalfForm').serialize();

        $.ajax({
            url: '/Incidents/SubmitOnBehalf',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: payload,
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Incident submitted successfully.');
                    loadList(1);
                    setDetailContent('<p class="text-slate-500">Select an incident to view details.</p>');
                    closeMobileDetailPane();
                } else {
                    notifyError((response && response.message) || 'Failed to submit incident.');
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, 'Failed to submit incident.'));
            }
        });
    }

    function updateStatus(id) {
        const selectedStatus = parseInt($('#incidentStatusSelect').val(), 10);
        if (Number.isNaN(selectedStatus) || selectedStatus <= 0) {
            notifyError('Please select a valid status.');
            return;
        }

        const payload = {
            newStatus: selectedStatus,
            note: $('#incidentStatusNote').val()
        };

        $.ajax({
            url: '/Incidents/UpdateStatus?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: payload,
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Status updated.');
                    loadList(state.page);
                    loadDetail(id);
                } else {
                    notifyError((response && response.message) || 'Failed to update status.');
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, 'Failed to update status.'));
            }
        });
    }

    function addComment(id) {
        const body = $('#incidentCommentBody').val();
        $.ajax({
            url: '/Incidents/AddComment?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: { body: body },
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Comment added.');
                    loadDetail(id);
                } else {
                    notifyError((response && response.message) || 'Failed to add comment.');
                }
            },
            error: function () {
                notifyError('Failed to add comment.');
            }
        });
    }

    function bindEvents() {
        $(document).on('click', '#reloadIncidentsBtn', function () {
            loadList(state.page);
        });

        $(document).on('click', '#closeIncidentDetailPaneBtn', function () {
            closeMobileDetailPane();
        });

        $(document).on('click', '#openExportCsvModalBtn', function () {
            openExportModal();
        });

        $(document).on('click', '#closeExportCsvModalBtn', function () {
            closeExportModal();
        });

        $(document).on('click', '#resetExportCsvFiltersBtn', function () {
            resetExportModalFilters();
        });

        $(document).on('click', '#exportCsvModal', function (event) {
            if (event.target && event.target.id === 'exportCsvModal') {
                closeExportModal();
            }
        });

        $(document).on('submit', '#exportCsvForm', function () {
            closeExportModal();
        });

        $(document).on('click', '#openOnBehalfIncidentBtn', function () {
            loadPane('/Incidents/SubmitOnBehalfForm');
        });

        $(document).on('click', '.incident-detail-btn', function () {
            const id = $(this).data('incident-id');
            loadDetail(id);
        });

        $(document).on('click', '#updateIncidentStatusBtn', function () {
            const id = $(this).data('incident-id');
            updateStatus(id);
        });

        $(document).on('click', '#addIncidentCommentBtn', function () {
            const id = $(this).data('incident-id');
            addComment(id);
        });

        $(document).on('click', '#submitOnBehalfBtn', function () {
            submitOnBehalf();
        });

        $(document).on('click', '#incidentsApplyFiltersBtn', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#incidentsResetFiltersBtn', function () {
            state.page = 1;
            state.pageSize = 20;
            state.searchTerm = '';
            state.status = '';
            state.disasterType = '';
            writeStateToUi();
            loadList(1);
        });

        $(document).on('keypress', '#incidentsSearchInput', function (event) {
            if (event.which === 13) {
                event.preventDefault();
                state.page = 1;
                loadList(1);
            }
        });

        $(document).on('change', '#incidentsPageSize', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#incidentsPrevPageBtn', function () {
            if (state.hasPreviousPage) {
                loadList(state.page - 1);
            }
        });

        $(document).on('click', '#incidentsNextPageBtn', function () {
            if (state.hasNextPage) {
                loadList(state.page + 1);
            }
        });
    }

    var statusColorMap = {
        'Open': { bg: 'bg-amber-50', text: 'text-amber-700', border: 'border-amber-200', dot: 'bg-amber-500' },
        'InProgress': { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-200', dot: 'bg-blue-500' },
        'Closed': { bg: 'bg-emerald-50', text: 'text-emerald-700', border: 'border-emerald-200', dot: 'bg-emerald-500' }
    };

    var typeColorMap = {
        'Landslide': { bg: 'bg-stone-50', text: 'text-stone-700', border: 'border-stone-200' },
        'Flood': { bg: 'bg-sky-50', text: 'text-sky-700', border: 'border-sky-200' },
        'Fire': { bg: 'bg-red-50', text: 'text-red-700', border: 'border-red-200' },
        'Earthquake': { bg: 'bg-yellow-50', text: 'text-yellow-700', border: 'border-yellow-200' },
        'RoadBlockage': { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-200' },
        'Others': { bg: 'bg-slate-50', text: 'text-slate-600', border: 'border-slate-200' }
    };

    function loadSummary() {
        $.ajax({
            url: '/Dashboard/Analytics',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: {},
            success: function (response) {
                if (!response || !response.success || !response.data) {
                    return;
                }
                renderStatusBadges(response.data.statusBreakdown || []);
                renderTypeBadges(response.data.incidentDistribution || []);
            },
            error: function () { }
        });
    }

    function renderStatusBadges(items) {
        var $host = $('#statusCountBadges');
        if (!$host.length) return;
        var html = '<span class="text-xs font-semibold text-slate-500 self-center mr-1">By Status:</span>';
        if (!items.length) {
            html += '<span class="text-xs text-slate-400">No data</span>';
        }
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var label = formatStatus(item.status);
            var colors = statusColorMap[statusKeyFromInt(item.status)] || { bg: 'bg-slate-50', text: 'text-slate-600', border: 'border-slate-200', dot: 'bg-slate-400' };
            html += '<span class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-semibold ' + colors.bg + ' ' + colors.text + ' ' + colors.border + '">';
            html += '<span class="w-1.5 h-1.5 rounded-full ' + colors.dot + '"></span>';
            html += label + ': ' + item.count + '</span>';
        }
        $host.html(html);
    }

    function renderTypeBadges(items) {
        var $host = $('#typeCountBadges');
        if (!$host.length) return;
        var html = '<span class="text-xs font-semibold text-slate-500 self-center mr-1">By Type:</span>';
        if (!items.length) {
            html += '<span class="text-xs text-slate-400">No data</span>';
        }
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var label = formatDisasterType(item.disasterType);
            var colors = typeColorMap[typeKeyFromInt(item.disasterType)] || { bg: 'bg-slate-50', text: 'text-slate-600', border: 'border-slate-200' };
            html += '<span class="inline-flex items-center gap-1 rounded-full border px-3 py-1 text-xs font-semibold ' + colors.bg + ' ' + colors.text + ' ' + colors.border + '">';
            html += label + ': ' + item.count + '</span>';
        }
        $host.html(html);
    }

    function statusKeyFromInt(value) {
        var map = { 1: 'Open', 2: 'InProgress', 3: 'Closed' };
        return map[value] || 'Open';
    }

    function formatStatus(value) {
        var map = { 1: 'Open', 2: 'In Progress', 3: 'Closed' };
        return map[value] || 'Unknown';
    }

    function typeKeyFromInt(value) {
        var map = { 1: 'Landslide', 2: 'Flood', 3: 'Fire', 4: 'Earthquake', 5: 'RoadBlockage', 6: 'Others' };
        return map[value] || 'Others';
    }

    function formatDisasterType(value) {
        var map = { 1: 'Landslide', 2: 'Flood', 3: 'Fire', 4: 'Earthquake', 5: 'Road Blockage', 6: 'Others' };
        return map[value] || 'Unknown';
    }

    function init() {
        bindEvents();
        writeStateToUi();
        updatePaginationUi();
        loadList(state.page);
        loadSummary();

        $(window).on('resize', function () {
            if (window.innerWidth >= 768) {
                $('body').removeClass('overflow-hidden');
                $('#incidentDetailHost').removeClass('is-open');
                closeExportModal();
            }
        });
    }

    return {
        init: init
    };
})();

$(document).ready(function () {
    if ($('#incidentsListHost').length) {
        incidentsModule.init();
    }
});
