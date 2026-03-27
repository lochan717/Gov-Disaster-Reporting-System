const incidentsModule = (function () {
    const state = {
        page: 1,
        pageSize: 20,
        searchTerm: '',
        totalPages: 1,
        totalCount: 0,
        hasPreviousPage: false,
        hasNextPage: false
    };

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
        state.pageSize = parseInt(($('#incidentsPageSize').val() || '20').toString(), 10) || 20;
    }

    function writeStateToUi() {
        $('#incidentsSearchInput').val(state.searchTerm);
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
                searchTerm: state.searchTerm
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
                $('#incidentDetailHost').html(html);
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
                $('#incidentDetailHost').html(html);

                if ($.validator && $.validator.unobtrusive) {
                    const $forms = $('#incidentDetailHost form');
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

        const payload = {
            DistrictId: $('#submitIncidentOnBehalfForm select[name="DistrictId"]').val(),
            SubmissionChannel: $('#submitIncidentOnBehalfForm select[name="SubmissionChannel"]').val(),
            DisasterType: $('#submitIncidentOnBehalfForm select[name="DisasterType"]').val(),
            Priority: $('#submitIncidentOnBehalfForm select[name="Priority"]').val(),
            LocationText: $('#submitIncidentOnBehalfForm input[name="LocationText"]').val(),
            LocationGpsLat: $('#submitIncidentOnBehalfForm input[name="LocationGpsLat"]').val(),
            LocationGpsLng: $('#submitIncidentOnBehalfForm input[name="LocationGpsLng"]').val(),
            MobileNumber: $('#submitIncidentOnBehalfForm input[name="MobileNumber"]').val(),
            Details: $('#submitIncidentOnBehalfForm textarea[name="Details"]').val()
        };

        $.ajax({
            url: '/Incidents/SubmitOnBehalf',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Incident submitted successfully.');
                    loadList(1);
                    $('#incidentDetailHost').html('<p class="text-slate-500">Select an incident to view details.</p>');
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
        const payload = {
            newStatus: parseInt($('#incidentStatusSelect').val(), 10),
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
            error: function () {
                notifyError('Failed to update status.');
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

    function init() {
        bindEvents();
        writeStateToUi();
        updatePaginationUi();
        loadList(state.page);
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
