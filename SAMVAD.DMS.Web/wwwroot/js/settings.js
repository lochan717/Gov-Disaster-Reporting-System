const settingsModule = (function () {
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

    function parseValidation(formSelector) {
        const $form = $(formSelector);
        if (!$form.length || !$.validator || !$.validator.unobtrusive) {
            return;
        }

        $form.removeData('validator');
        $form.removeData('unobtrusiveValidation');
        $.validator.unobtrusive.parse($form);
    }

    function isFormValid(formSelector) {
        const $form = $(formSelector);
        if (!$form.length) {
            return false;
        }

        parseValidation(formSelector);
        if ($.validator && $form.data('validator')) {
            return $form.valid();
        }

        return true;
    }

    function submitForm(formSelector, options) {
        const $form = $(formSelector);
        if (!$form.length || !isFormValid(formSelector)) {
            return;
        }

        $.ajax({
            url: $form.attr('action') || '',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: $form.serialize(),
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || options.successMessage);
                    if (typeof options.onSuccess === 'function') {
                        options.onSuccess($form);
                    }
                    return;
                }

                notifyError((response && response.message) || options.errorMessage);
            },
            error: function (xhr) {
                notifyError(extractErrorMessage(xhr, options.errorMessage));
            }
        });
    }

    function bindEvents() {
        $(document).on('submit', '#settingsProfileForm', function (event) {
            event.preventDefault();
            submitForm('#settingsProfileForm', {
                successMessage: 'Profile updated successfully.',
                errorMessage: 'Failed to update profile.'
            });
        });

        $(document).on('submit', '#settingsPasswordForm', function (event) {
            event.preventDefault();
            submitForm('#settingsPasswordForm', {
                successMessage: 'Password changed successfully.',
                errorMessage: 'Failed to change password.',
                onSuccess: function ($form) {
                    $form.find('input[type="password"]').val('');
                }
            });
        });
    }

    function init() {
        parseValidation('#settingsProfileForm');
        parseValidation('#settingsPasswordForm');
        bindEvents();
    }

    return {
        init: init
    };
})();

$(document).ready(function () {
    if ($('#settingsProfileForm').length || $('#settingsPasswordForm').length) {
        settingsModule.init();
    }
});