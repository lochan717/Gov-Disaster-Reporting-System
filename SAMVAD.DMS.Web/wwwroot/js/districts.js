const districtsModule = (function () {
    const state = {
        page: 1,
        pageSize: 20,
        search: '',
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

    function readFiltersFromUi() {
        state.search = ($('#districtsSearchInput').val() || '').toString().trim();
        state.pageSize = parseInt(($('#districtsPageSize').val() || '20').toString(), 10) || 20;
    }

    function writeStateToUi() {
        $('#districtsSearchInput').val(state.search);
        $('#districtsPageSize').val(state.pageSize.toString());
    }

    function updatePaginationUi() {
        const safeTotalPages = state.totalPages > 0 ? state.totalPages : 1;
        const currentPage = state.totalPages > 0 ? state.page : 1;
        $('#districtsPaginationInfo').text('Page ' + currentPage + ' of ' + safeTotalPages + ' (' + state.totalCount + ' total)');
        $('#districtsPrevPageBtn').prop('disabled', !state.hasPreviousPage);
        $('#districtsNextPageBtn').prop('disabled', !state.hasNextPage);
    }

    function syncPaginationFromListMeta() {
        const $meta = $('#districtsListMeta');
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
            url: '/Districts/List',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: {
                page: state.page,
                pageSize: state.pageSize,
                search: state.search
            },
            success: function (html) {
                $('#districtsListHost').html(html);
                syncPaginationFromListMeta();
            },
            error: function () {
                notifyError('Unable to load districts.');
            }
        });
    }

    function loadPane(url) {
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                $('#districtSidePane').html(html);
                if ($.validator && $.validator.unobtrusive) {
                    const $forms = $('#districtSidePane form');
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

    function addDistrict() {
        if (!isFormValid('#addDistrictForm')) {
            return;
        }

        const payload = {
            name: $('#addDistrictForm input[name="Name"]').val(),
            code: $('#addDistrictForm input[name="Code"]').val(),
            state: $('#addDistrictForm input[name="State"]').val(),
            isActive: true
        };

        $.ajax({
            url: '/Districts/Add',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: payload,
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'District created.');
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to create district.');
                }
            },
            error: function () {
                notifyError('Failed to create district.');
            }
        });
    }

    function editDistrict(id) {
        if (!id) {
            notifyError('Invalid district id. Reload and try again.');
            return;
        }

        if (!isFormValid('#editDistrictForm')) {
            return;
        }

        const payload = {
            Name: $('#editDistrictForm input[name="Name"]').val(),
            Code: $('#editDistrictForm input[name="Code"]').val(),
            State: $('#editDistrictForm input[name="State"]').val(),
            IsActive: true
        };

        $.ajax({
            url: '/Districts/Edit?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'District updated.');
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to update district.');
                }
            },
            error: function () {
                notifyError('Failed to update district.');
            }
        });
    }

    function deleteDistrict(id) {
        if (!id) {
            notifyError('Invalid district id.');
            return;
        }

        $.ajax({
            url: '/Districts/Delete?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'District deleted.');
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to delete district.');
                }
            },
            error: function () {
                notifyError('Failed to delete district.');
            }
        });
    }

    function toggleDistrict(id) {
        $.ajax({
            url: '/Districts/ToggleStatus?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Status updated.');
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to toggle district status.');
                }
            },
            error: function () {
                notifyError('Failed to toggle district status.');
            }
        });
    }

    function bindEvents() {
        $(document).on('click', '#openAddDistrictPaneBtn', function () {
            loadPane('/Districts/Add');
        });

        $(document).on('click', '.district-edit-btn', function () {
            loadPane('/Districts/Edit?id=' + encodeURIComponent($(this).data('district-id')));
        });

        $(document).on('click', '#saveDistrictBtn', function () {
            addDistrict();
        });

        $(document).on('click', '#updateDistrictBtn', function () {
            const id = $('#editDistrictForm input[name="Id"]').val();
            editDistrict(id);
        });

        $(document).on('click', '.district-toggle-btn', function () {
            toggleDistrict($(this).data('district-id'));
        });

        $(document).on('click', '.district-delete-btn', async function () {
            const id = $(this).data('district-id');
            const message = 'Delete this district? This is permanent and allowed only when no admins/incidents are linked.';

            if (!(window.samvadUi && typeof window.samvadUi.confirm === 'function')) {
                notifyError('Confirmation dialog is unavailable. Please reload the page and try again.');
                return;
            }

            const confirmed = await window.samvadUi.confirm({
                title: 'Delete District',
                message: message,
                confirmText: 'Delete',
                confirmButtonClass: 'rounded-lg bg-rose-600 px-3 py-2 text-sm font-semibold text-white'
            });

            if (!confirmed) {
                return;
            }

            deleteDistrict(id);
        });

        $(document).on('click', '#districtsApplyFiltersBtn', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#districtsResetFiltersBtn', function () {
            state.page = 1;
            state.pageSize = 20;
            state.search = '';
            writeStateToUi();
            loadList(1);
        });

        $(document).on('keypress', '#districtsSearchInput', function (event) {
            if (event.which === 13) {
                event.preventDefault();
                state.page = 1;
                loadList(1);
            }
        });

        $(document).on('change', '#districtsPageSize', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#districtsPrevPageBtn', function () {
            if (state.hasPreviousPage) {
                loadList(state.page - 1);
            }
        });

        $(document).on('click', '#districtsNextPageBtn', function () {
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
    if ($('#districtsListHost').length) {
        districtsModule.init();
    }
});
