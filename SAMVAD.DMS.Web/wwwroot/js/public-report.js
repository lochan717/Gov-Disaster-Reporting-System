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
            if (disasterType < 1 || priority < 1) {
                if (window.toastr) {
                    window.toastr.warning('Please select disaster type and priority.');
                }
                return false;
            }
            return true;
        }

        if (step === 2) {
            const districtId = ($('#DistrictId').val() || '').toString().trim();
            if (!districtId) {
                if (window.toastr) {
                    window.toastr.warning('Please select district.');
                }
                return false;
            }

            const fileCount = ($('#reportMediaFiles')[0]?.files || []).length;
            if (fileCount > 5) {
                if (window.toastr) {
                    window.toastr.warning('Maximum 5 files are allowed.');
                }
                return false;
            }
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
                if (window.toastr) {
                    window.toastr.warning('You can upload maximum 5 files.');
                }
            }
        });
    }

    function bindSubmitGuard() {
        $(document).on('submit', '#publicReportForm', function () {
            if (!isStepValid(3)) {
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

        const priority = parseInt($('#reportPriority').val(), 10) || 0;
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