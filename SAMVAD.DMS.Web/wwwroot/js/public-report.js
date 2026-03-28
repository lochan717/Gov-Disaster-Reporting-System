const publicReportModule = (function () {
    const state = {
        currentStep: 1,
        totalSteps: 3
    };

    const disasterTypeLabels = {
        1: 'Landslide',
        2: 'Flood',
        3: 'Fire',
        4: 'Earthquake',
        5: 'Road Blockage',
        6: 'Others'
    };

    const priorityLabels = {
        1: 'Standard',
        2: 'Emergency'
    };

    function setFieldError(fieldName, message) {
        const $message = $('[data-valmsg-for="' + fieldName + '"]');
        if ($message.length) {
            $message.text(message || '');
        }
    }

    function clearFieldError(fieldName) {
        setFieldError(fieldName, '');
    }

    function setMediaError(message) {
        $('#reportMediaFilesValidation').text(message || '');
    }

    function hasGpsCoordinates() {
        const lat = ($('#LocationGpsLat').val() || '').toString().trim();
        const lng = ($('#LocationGpsLng').val() || '').toString().trim();
        return !!lat && !!lng;
    }

    function getStepSections() {
        return $('[data-step]');
    }

    function updateStepUi() {
        const percentage = Math.round((state.currentStep / state.totalSteps) * 100);
        $('#reportStepLabel').text('Step ' + state.currentStep + ' of ' + state.totalSteps);
        $('#reportProgressBar').css('width', percentage + '%');

        getStepSections().addClass('hidden');
        $('[data-step="' + state.currentStep + '"]').removeClass('hidden');

        $('#reportPrevStepBtn').toggleClass('hidden', state.currentStep === 1);
        $('#reportNextStepBtn').toggleClass('hidden', state.currentStep === state.totalSteps);
        $('#reportSubmitBtn').toggleClass('hidden', state.currentStep !== state.totalSteps);

        if (state.currentStep === 3) {
            updateReviewSection();
        }
    }

    function isStepValid(step) {
        if (step === 1) {
            const disasterType = parseInt($('#reportDisasterType').val(), 10) || 0;
            const priority = parseInt($('#reportPriority').val(), 10) || 0;
            let isValid = true;

            if (disasterType < 1) {
                setFieldError('DisasterType', 'Disaster type is required.');
                isValid = false;
            } else {
                clearFieldError('DisasterType');
            }

            if (priority < 1) {
                setFieldError('Priority', 'Priority is required.');
                isValid = false;
            } else {
                clearFieldError('Priority');
            }

            return isValid;
        }

        if (step === 2) {
            const districtId = ($('#DistrictId').val() || '').toString().trim();
            const locationText = ($('#LocationText').val() || '').toString().trim();
            let isValid = true;

            if (!districtId) {
                setFieldError('DistrictId', 'District is required.');
                isValid = false;
            } else {
                clearFieldError('DistrictId');
            }

            if (!locationText && !hasGpsCoordinates()) {
                setFieldError('LocationText', 'Location text is required when GPS is not captured.');
                isValid = false;
            } else {
                clearFieldError('LocationText');
            }

            const fileCount = ($('#reportMediaFiles')[0]?.files || []).length;
            if (fileCount > 5) {
                setMediaError('Maximum 5 files are allowed.');
                isValid = false;
            } else {
                setMediaError('');
            }

            return isValid;
        }

        if (step === 3) {
            const mobile = ($('#MobileNumber').val() || '').toString().trim();
            const mobileRegex = /^[0-9]{10}$/;

            if (!mobile) {
                setFieldError('MobileNumber', 'Mobile number is required.');
                return false;
            }

            if (!mobileRegex.test(mobile)) {
                setFieldError('MobileNumber', 'Enter a valid 10-digit mobile number.');
                return false;
            }

            clearFieldError('MobileNumber');
            return true;
        }

        return true;
    }

    function refreshSelectionStyles(groupSelector, activeSelector, selectedValue) {
        $(groupSelector).removeClass(activeSelector);
        $(groupSelector + '[data-value="' + selectedValue + '"]').addClass(activeSelector);
    }

    function bindSelectionEvents() {
        $(document).on('click', '.report-disaster-btn', function () {
            const value = $(this).data('value');
            $('#reportDisasterType').val(value);
            refreshSelectionStyles('.report-disaster-btn', 'border-[#ec5b13] bg-orange-50 ring-1 ring-[#ec5b13]', value);
            clearFieldError('DisasterType');
        });

        $(document).on('click', '.report-priority-btn', function () {
            const value = $(this).data('value');
            $('#reportPriority').val(value);

            $('.report-priority-btn').removeClass('border-[#ec5b13] bg-orange-50 text-[#ec5b13] border-rose-300 bg-rose-50 text-rose-700');
            if (parseInt(value, 10) === 2) {
                $(this).addClass('border-rose-300 bg-rose-50 text-rose-700');
            } else {
                $(this).addClass('border-[#ec5b13] bg-orange-50 text-[#ec5b13]');
            }

            clearFieldError('Priority');
        });

        $(document).on('change', '#DistrictId', function () {
            if (($('#DistrictId').val() || '').toString().trim()) {
                clearFieldError('DistrictId');
            }
        });

        $(document).on('input', '#LocationText,#MobileNumber', function () {
            const fieldName = $(this).attr('name');
            if (fieldName) {
                clearFieldError(fieldName);
            }
        });

        $(document).on('input', '#LocationGpsLat,#LocationGpsLng', function () {
            if (hasGpsCoordinates()) {
                clearFieldError('LocationText');
            }
        });
    }

    function updateReviewSection() {
        const disasterType = parseInt($('#reportDisasterType').val(), 10) || 0;
        const priority = parseInt($('#reportPriority').val(), 10) || 0;
        const district = ($('#DistrictId option:selected').text() || '').toString().trim();
        const location = ($('#LocationText').val() || '').toString().trim();
        const lat = ($('#LocationGpsLat').val() || '').toString().trim();
        const lng = ($('#LocationGpsLng').val() || '').toString().trim();
        const mediaCount = ($('#reportMediaFiles')[0]?.files || []).length;

        $('#reviewDistrict').text(district || '-');
        $('#reviewDisasterType').text(disasterTypeLabels[disasterType] || '-');
        $('#reviewPriority').text(priorityLabels[priority] || '-');
        $('#reviewLocation').text(location || '-');
        $('#reviewGps').text((lat && lng) ? (lat + ', ' + lng) : '-');
        $('#reviewMedia').text(mediaCount.toString());
    }

    function bindStepNavigation() {
        $(document).on('click', '#reportNextStepBtn', function () {
            if (!isStepValid(state.currentStep)) {
                return;
            }

            if (state.currentStep < state.totalSteps) {
                state.currentStep += 1;
                updateStepUi();
            }
        });

        $(document).on('click', '#reportPrevStepBtn', function () {
            if (state.currentStep > 1) {
                state.currentStep -= 1;
                updateStepUi();
            }
        });
    }

    function bindGpsCapture() {
        $(document).on('click', '#captureGpsBtn', function () {
            if (!navigator.geolocation) {
                if (window.toastr) {
                    window.toastr.warning('Geolocation is not supported on this device/browser.');
                }
                return;
            }

            navigator.geolocation.getCurrentPosition(function (position) {
                $('#LocationGpsLat').val(position.coords.latitude.toFixed(6));
                $('#LocationGpsLng').val(position.coords.longitude.toFixed(6));
                if (window.toastr) {
                    window.toastr.success('GPS location captured.');
                }
            }, function () {
                if (window.toastr) {
                    window.toastr.error('Unable to capture location. Please enter location manually.');
                }
            }, {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            });
        });
    }

    function bindMediaValidation() {
        $(document).on('change', '#reportMediaFiles', function () {
            const files = this.files || [];
            $('#mediaCountHint').text(files.length + ' file(s) selected.');

            if (files.length > 5) {
                this.value = '';
                $('#mediaCountHint').text('');
                setMediaError('Maximum 5 files are allowed.');
                return;
            }

            setMediaError('');
        });
    }

    function bindSubmitGuard() {
        $(document).on('submit', '#publicReportForm', function () {
            if (!isStepValid(1) || !isStepValid(2) || !isStepValid(3)) {
                return false;
            }

            if ($.validator && $(this).data('validator')) {
                return $(this).valid();
            }

            return true;
        });
    }

    function initExistingValues() {
        const disasterType = parseInt($('#reportDisasterType').val(), 10) || 0;
        if (disasterType > 0) {
            refreshSelectionStyles('.report-disaster-btn', 'border-[#ec5b13] bg-orange-50 ring-1 ring-[#ec5b13]', disasterType);
        }

        let priority = parseInt($('#reportPriority').val(), 10) || 0;
        if (priority < 1) {
            priority = 2;
            $('#reportPriority').val(priority);
        }

        if (priority > 0) {
            $('.report-priority-btn').removeClass('border-[#ec5b13] bg-orange-50 text-[#ec5b13] border-rose-300 bg-rose-50 text-rose-700');
            const $selected = $('.report-priority-btn[data-value="' + priority + '"]');
            if (priority === 2) {
                $selected.addClass('border-rose-300 bg-rose-50 text-rose-700');
            } else {
                $selected.addClass('border-[#ec5b13] bg-orange-50 text-[#ec5b13]');
            }
        }
    }

    function init() {
        bindSelectionEvents();
        bindStepNavigation();
        bindGpsCapture();
        bindMediaValidation();
        bindSubmitGuard();
        initExistingValues();
        updateStepUi();
    }

    return {
        init: init
    };
})();

$(document).ready(function () {
    if ($('#publicReportForm').length) {
        publicReportModule.init();
    }
});