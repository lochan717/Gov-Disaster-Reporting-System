const adminsModule = (function () {
    const state = {
        page: 1,
        pageSize: 20,
        search: '',
        isActive: '',
        role: '',
        districtId: '',
        totalPages: 1,
        totalCount: 0,
        hasPreviousPage: false,
        hasNextPage: false
    };

    function antiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function selectedValues(selector) {
        const values = $(selector).val();
        if (!values) {
            return [];
        }

        return Array.isArray(values) ? values : [values];
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

    async function confirmAction(title, message, confirmText, confirmButtonClass) {
        if (!(window.samvadUi && typeof window.samvadUi.confirm === 'function')) {
            notifyError('Confirmation dialog is unavailable. Please reload the page and try again.');
            return false;
        }

        return await window.samvadUi.confirm({
            title: title,
            message: message,
            confirmText: confirmText,
            confirmButtonClass: confirmButtonClass
        });
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
        state.search = ($('#adminsSearchInput').val() || '').toString().trim();
        state.isActive = ($('#adminsStatusFilter').val() || '').toString();
        state.role = ($('#adminsRoleFilter').val() || '').toString();
        state.districtId = ($('#adminsDistrictFilter').val() || '').toString();
        state.pageSize = parseInt(($('#adminsPageSize').val() || '20').toString(), 10) || 20;
    }

    function writeStateToUi() {
        $('#adminsSearchInput').val(state.search);
        $('#adminsStatusFilter').val(state.isActive);
        $('#adminsRoleFilter').val(state.role);
        $('#adminsDistrictFilter').val(state.districtId);
        $('#adminsPageSize').val(state.pageSize.toString());
    }

    function updatePaginationUi() {
        const safeTotalPages = state.totalPages > 0 ? state.totalPages : 1;
        const currentPage = state.totalPages > 0 ? state.page : 1;
        $('#adminsPaginationInfo').text('Page ' + currentPage + ' of ' + safeTotalPages + ' (' + state.totalCount + ' total)');
        $('#adminsPrevPageBtn').prop('disabled', !state.hasPreviousPage);
        $('#adminsNextPageBtn').prop('disabled', !state.hasNextPage);
    }

    function syncPaginationFromListMeta() {
        const $meta = $('#adminsListMeta');
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
            url: '/Admins/List',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: {
                page: state.page,
                pageSize: state.pageSize,
                search: state.search,
                isActive: state.isActive,
                role: state.role,
                districtId: state.districtId
            },
            success: function (html) {
                $('#adminsListHost').html(html);
                syncPaginationFromListMeta();
            },
            error: function () {
                notifyError('Unable to load admins.');
            }
        });
    }

    function loadPane(url) {
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                $('#adminSidePane').html(html);
                if ($.validator && $.validator.unobtrusive) {
                    const $forms = $('#adminSidePane form');
                    $forms.each(function () {
                        $(this).removeData('validator');
                        $(this).removeData('unobtrusiveValidation');
                        $.validator.unobtrusive.parse($(this));
                    });
                }
                initializeSelect2();
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

    function initializeSelect2() {
        if (!$.fn.select2) {
            return;
        }

        const $roleAdd = $('#addAdminForm select[name="RoleLabel"]');
        if ($roleAdd.length) {
            $roleAdd.select2({
                width: '100%',
                placeholder: 'Select role',
                dropdownParent: $('#adminSidePane')
            });
        }

        const $roleEdit = $('#editAdminForm select[name="RoleLabel"]');
        if ($roleEdit.length) {
            $roleEdit.select2({
                width: '100%',
                placeholder: 'Select role',
                dropdownParent: $('#adminSidePane')
            });
        }

        const $districtAdd = $('#addAdminDistrictIds');
        if ($districtAdd.length) {
            $districtAdd.select2({
                width: '100%',
                placeholder: 'Select districts',
                dropdownParent: $('#adminSidePane')
            });
        }

        const $districtEdit = $('#editAdminDistrictIds');
        if ($districtEdit.length) {
            $districtEdit.select2({
                width: '100%',
                placeholder: 'Select districts',
                dropdownParent: $('#adminSidePane')
            });
        }
    }

    function resetAddAdminForm() {
        const $form = $('#addAdminForm');
        if (!$form.length) {
            return;
        }

        $form[0].reset();

        const $role = $form.find('select[name="RoleLabel"]');
        $role.val('');
        if ($.fn.select2 && $role.hasClass('select2-hidden-accessible')) {
            $role.trigger('change');
        }

        const $districts = $('#addAdminDistrictIds');
        $districts.val([]);
        if ($.fn.select2 && $districts.hasClass('select2-hidden-accessible')) {
            $districts.trigger('change');
        }
    }

    function addAdmin() {
        if (!isFormValid('#addAdminForm')) {
            return;
        }

        const payload = {
            Name: $('#addAdminForm input[name="Name"]').val(),
            Email: $('#addAdminForm input[name="Email"]').val(),
            RoleLabel: $('#addAdminForm select[name="RoleLabel"]').val(),
            AssignedDistrictIds: selectedValues('#addAdminDistrictIds')
        };

        $.ajax({
            url: '/Admins/Add',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Admin created.');
                    resetAddAdminForm();
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to create admin.');
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, 'Failed to create admin.'));
            }
        });
    }

    function editAdmin(id) {
        if (!isFormValid('#editAdminForm')) {
            return;
        }

        const payload = {
            Name: $('#editAdminForm input[name="Name"]').val(),
            RoleLabel: $('#editAdminForm select[name="RoleLabel"]').val(),
            AssignedDistrictIds: selectedValues('#editAdminDistrictIds')
        };

        $.ajax({
            url: '/Admins/Edit?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Admin updated.');
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || 'Failed to update admin.');
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, 'Failed to update admin.'));
            }
        });
    }

    function resetPassword(id) {
        if (!id) {
            notifyError('Invalid user id.');
            return;
        }

        if (!isFormValid('#adminResetPasswordForm')) {
            return;
        }

        const payload = {
            NewPassword: $('#adminResetPasswordForm input[name="NewPassword"]').val(),
            ConfirmPassword: $('#adminResetPasswordForm input[name="ConfirmPassword"]').val()
        };

        $.ajax({
            url: '/Admins/ResetPassword?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(payload),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Password reset successfully.');
                    $('#adminSidePane').html('<p class="text-slate-500">Choose an action to manage admin details.</p>');
                } else {
                    notifyError((response && response.message) || 'Failed to reset password.');
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, 'Failed to reset password.'));
            }
        });
    }

    function simplePost(url, successMessage, failureMessage) {
        $.ajax({
            url: url,
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || successMessage);
                    loadList(state.page);
                } else {
                    notifyError((response && response.message) || failureMessage);
                }
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, failureMessage));
            }
        });
    }

    function bindEvents() {
        $(document).on('click', '#openAddAdminPaneBtn', function () {
            loadPane('/Admins/Add');
        });

        $(document).on('click', '.admin-edit-btn', function () {
            loadPane('/Admins/Edit?id=' + encodeURIComponent($(this).data('admin-id')));
        });

        $(document).on('click', '#saveAdminBtn', function () {
            addAdmin();
        });

        $(document).on('click', '#updateAdminBtn', function () {
            const id = $('#editAdminForm input[name="Id"]').val();
            editAdmin(id);
        });

        $(document).on('click', '.admin-toggle-btn', async function () {
            const id = $(this).data('admin-id');
            const confirmed = await confirmAction(
                'Toggle User Status',
                'Are you sure you want to toggle this user\'s status?',
                'Yes, Toggle',
                'rounded-lg bg-slate-900 px-3 py-2 text-sm font-semibold text-white'
            );

            if (!confirmed) {
                return;
            }

            simplePost('/Admins/ToggleStatus?id=' + encodeURIComponent(id), 'Status updated.', 'Failed to toggle status.');
        });

        $(document).on('click', '.admin-reset-btn', function () {
            const id = $(this).data('admin-id');
            loadPane('/Admins/ResetPasswordForm?id=' + encodeURIComponent(id));
        });

        $(document).on('click', '#confirmAdminResetPasswordBtn', async function () {
            const id = $('#adminResetPasswordForm input[name="Id"]').val();
            const confirmed = await confirmAction(
                'Reset Password',
                'Are you sure you want to reset this user\'s password now?',
                'Reset Password',
                'rounded-lg bg-rose-600 px-3 py-2 text-sm font-semibold text-white'
            );

            if (!confirmed) {
                return;
            }

            resetPassword(id);
        });

        $(document).on('click', '#adminsApplyFiltersBtn', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#adminsResetFiltersBtn', function () {
            state.page = 1;
            state.pageSize = 20;
            state.search = '';
            state.isActive = '';
            state.role = '';
            state.districtId = '';
            writeStateToUi();
            loadList(1);
        });

        $(document).on('keypress', '#adminsSearchInput', function (event) {
            if (event.which === 13) {
                event.preventDefault();
                state.page = 1;
                loadList(1);
            }
        });

        $(document).on('change', '#adminsPageSize', function () {
            state.page = 1;
            loadList(1);
        });

        $(document).on('click', '#adminsPrevPageBtn', function () {
            if (state.hasPreviousPage) {
                loadList(state.page - 1);
            }
        });

        $(document).on('click', '#adminsNextPageBtn', function () {
            if (state.hasNextPage) {
                loadList(state.page + 1);
            }
        });

    }

    function init() {
        bindEvents();
        writeStateToUi();
        initializeSelect2();
        updatePaginationUi();
        loadList(state.page);
    }

    return {
        init: init
    };
})();

$(document).ready(function () {
    if ($('#adminsListHost').length) {
        adminsModule.init();
    }
});
