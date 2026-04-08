const dashboardModule = (function () {
    var refreshTimer = null;
    var REFRESH_INTERVAL_MS = 60000;

    function antiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function notifyError(message) {
        if (window.toastr) {
            window.toastr.error(message || 'Something went wrong');
            return;
        }
        console.error(message || 'Something went wrong');
    }

    function notifySuccess(message) {
        if (window.toastr) {
            window.toastr.success(message || 'Success');
            return;
        }
        console.log(message || 'Success');
    }

    function extractErrorMessage(xhr, fallbackMessage) {
        var raw = xhr && xhr.responseText ? xhr.responseText : '';
        if (!raw) return fallbackMessage;
        try {
            var parsed = JSON.parse(raw);
            return (parsed && parsed.message) || fallbackMessage;
        } catch (e) {
            return fallbackMessage;
        }
    }

    /* ── colour maps ── */
    var statusColors = {
        1: { label: 'Open', bg: 'bg-amber-500', text: 'text-amber-600', light: 'bg-amber-50', border: 'border-amber-200', dot: 'bg-amber-500' },
        2: { label: 'In Progress', bg: 'bg-blue-500', text: 'text-blue-600', light: 'bg-blue-50', border: 'border-blue-200', dot: 'bg-blue-500' },
        3: { label: 'Closed', bg: 'bg-emerald-500', text: 'text-emerald-600', light: 'bg-emerald-50', border: 'border-emerald-200', dot: 'bg-emerald-500' }
    };

    var typeColors = {
        1: { label: 'Landslide', color: '#78716c' },
        2: { label: 'Flood', color: '#0ea5e9' },
        3: { label: 'Fire', color: '#ef4444' },
        4: { label: 'Earthquake', color: '#eab308' },
        5: { label: 'Road Blockage', color: '#a855f7' },
        6: { label: 'Others', color: '#64748b' }
    };

    /* ── KPI Cards ── */
    function renderKpiCards(kpiCards, statusBreakdown) {
        var $host = $('#dashKpiHost');
        if (!$host.length) return;

        var openCount = 0, inProgressCount = 0, closedCount = 0;
        for (var i = 0; i < statusBreakdown.length; i++) {
            var s = statusBreakdown[i];
            if (s.status === 1) openCount = s.count;
            else if (s.status === 2) inProgressCount = s.count;
            else if (s.status === 3) closedCount = s.count;
        }

        var total = 0, responseTime = '—', resolutionTime = '—';
        for (var j = 0; j < kpiCards.length; j++) {
            var kpi = kpiCards[j];
            if (kpi.key === 'total') total = kpi.value;
            else if (kpi.key === 'response') responseTime = formatMinutes(kpi.value);
            else if (kpi.key === 'resolution') resolutionTime = formatMinutes(kpi.value);
        }

        var cards = [
            { title: 'Total Incidents', value: total, css: 'text-slate-900' },
            { title: 'Open / Active', value: openCount + inProgressCount, css: 'text-orange-600' },
            { title: 'Resolved', value: closedCount, css: 'text-emerald-600' },
            { title: 'Avg Response Time', value: responseTime, css: 'text-blue-600' }
        ];

        var html = '';
        for (var k = 0; k < cards.length; k++) {
            var c = cards[k];
            html += '<div class="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">';
            html += '<p class="text-[10px] font-bold uppercase tracking-widest text-slate-400 mb-1">' + escapeHtml(c.title) + '</p>';
            html += '<span class="text-3xl font-extrabold ' + c.css + '">' + escapeHtml(String(c.value)) + '</span>';
            html += '</div>';
        }
        $host.html(html);
    }

    function formatMinutes(value) {
        var num = parseFloat(value);
        if (isNaN(num) || num <= 0) return '—';
        if (num < 60) return Math.round(num) + ' min';
        var hours = Math.floor(num / 60);
        var mins = Math.round(num % 60);
        return hours + 'h ' + mins + 'm';
    }

    /* ── Type Distribution (horizontal bars) ── */
    function renderTypeChart(items) {
        var $host = $('#dashTypeChart');
        if (!$host.length) return;

        if (!items || !items.length) {
            $host.html('<p class="text-sm text-slate-400">No incident data available.</p>');
            return;
        }

        var maxCount = 0;
        for (var i = 0; i < items.length; i++) {
            if (items[i].count > maxCount) maxCount = items[i].count;
        }
        if (maxCount === 0) maxCount = 1;

        var html = '';
        for (var j = 0; j < items.length; j++) {
            var item = items[j];
            var tc = typeColors[item.disasterType] || { label: 'Unknown', color: '#94a3b8' };
            var pct = Math.round((item.count / maxCount) * 100);
            html += '<div class="flex items-center gap-3">';
            html += '<span class="w-28 text-xs font-semibold text-slate-600 text-right truncate">' + escapeHtml(tc.label) + '</span>';
            html += '<div class="flex-1 h-5 bg-slate-100 rounded-full overflow-hidden">';
            html += '<div class="h-full rounded-full transition-all" style="width:' + pct + '%;background:' + tc.color + ';"></div>';
            html += '</div>';
            html += '<span class="w-8 text-xs font-bold text-slate-700 text-right">' + item.count + '</span>';
            html += '</div>';
        }
        $host.html(html);
    }

    /* ── Status Breakdown (donut-like summary) ── */
    function renderStatusChart(items) {
        var $host = $('#dashStatusChart');
        if (!$host.length) return;

        if (!items || !items.length) {
            $host.html('<p class="text-sm text-slate-400">No incident data available.</p>');
            return;
        }

        var total = 0;
        for (var i = 0; i < items.length; i++) total += items[i].count;
        if (total === 0) total = 1;

        // Stacked bar
        var barHtml = '<div class="flex h-6 rounded-full overflow-hidden mb-4">';
        for (var j = 0; j < items.length; j++) {
            var s = items[j];
            var sc = statusColors[s.status] || { bg: 'bg-slate-400', label: 'Unknown' };
            var widthPct = Math.max(((s.count / total) * 100), 0);
            barHtml += '<div class="' + sc.bg + ' transition-all" style="width:' + widthPct.toFixed(1) + '%" title="' + escapeHtml(sc.label) + ': ' + s.count + '"></div>';
        }
        barHtml += '</div>';

        // Legend
        var legendHtml = '<div class="grid grid-cols-3 gap-3">';
        for (var k = 0; k < items.length; k++) {
            var si = items[k];
            var sci = statusColors[si.status] || { label: 'Unknown', dot: 'bg-slate-400', text: 'text-slate-600' };
            legendHtml += '<div class="text-center">';
            legendHtml += '<div class="flex items-center justify-center gap-1 mb-1"><span class="w-2 h-2 rounded-full ' + sci.dot + '"></span><span class="text-xs font-semibold text-slate-500">' + escapeHtml(sci.label) + '</span></div>';
            legendHtml += '<span class="text-2xl font-extrabold ' + sci.text + '">' + si.count + '</span>';
            legendHtml += '</div>';
        }
        legendHtml += '</div>';

        $host.html(barHtml + legendHtml);
    }

    /* ── Live Feed ── */
    function renderFeed(feedData) {
        var $host = $('#dashFeedHost');
        var $meta = $('#dashFeedMeta');
        if (!$host.length) return;

        var items = (feedData && feedData.items) ? feedData.items : [];
        var totalCount = (feedData && feedData.totalCount) ? feedData.totalCount : 0;

        if ($meta.length) {
            $meta.text('Showing ' + items.length + ' of ' + totalCount + ' — auto-refresh every 60s');
        }

        if (!items.length) {
            $host.html('<p class="p-5 text-sm text-slate-400">No incidents found.</p>');
            return;
        }

        var html = '';

        /* ── Mobile cards (hidden on md+) ── */
        html += '<div class="space-y-3 p-4 md:hidden">';
        for (var m = 0; m < items.length; m++) {
            var card = items[m];
            var tcm = typeColors[card.disasterType] || { label: 'Unknown', color: '#94a3b8' };
            var scm = statusColors[card.status] || { label: 'Unknown', dot: 'bg-slate-400', text: 'text-slate-600', light: 'bg-slate-50', border: 'border-slate-200' };
            var priMobile = card.priority === 2
                ? '<span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-red-100 text-red-700 uppercase">Emergency</span>'
                : '<span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-600 uppercase">Standard</span>';

            html += '<article class="rounded-2xl border border-slate-200 p-4 bg-white shadow-sm">';
            html += '<div class="flex items-start justify-between gap-3">';
            html += '<button type="button" class="dash-detail-btn text-sm font-semibold text-orange-700 hover:underline" data-incident-id="' + card.id + '">' + escapeHtml(card.incidentId) + '</button>';
            html += '<span class="inline-flex items-center gap-1 rounded-full border px-2 py-1 text-xs font-semibold ' + scm.light + ' ' + scm.text + ' ' + scm.border + '"><span class="w-1.5 h-1.5 rounded-full ' + scm.dot + '"></span>' + escapeHtml(scm.label) + '</span>';
            html += '</div>';
            html += '<div class="mt-2 grid grid-cols-2 gap-2 text-xs text-slate-600">';
            html += '<p><span class="font-semibold text-slate-700">Type:</span> <span style="color:' + tcm.color + ';">' + escapeHtml(tcm.label) + '</span></p>';
            html += '<p><span class="font-semibold text-slate-700">Priority:</span> ' + priMobile + '</p>';
            html += '<p class="col-span-2"><span class="font-semibold text-slate-700">Location:</span> ' + escapeHtml(card.locationText || '—') + '</p>';
            html += '<p><span class="font-semibold text-slate-700">Time:</span> ' + formatDateTime(card.createdAt) + '</p>';
            html += '</div>';
            html += '<div class="mt-3"><button type="button" class="dash-detail-btn text-xs font-bold text-orange-600 hover:underline uppercase" data-incident-id="' + card.id + '">View Details</button></div>';
            html += '</article>';
        }
        html += '</div>';

        /* ── Desktop table (hidden on <md) ── */
        html += '<div class="hidden md:block">';
        html += '<table class="w-full text-left">';
        html += '<thead><tr class="bg-slate-50/60">';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Incident ID</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Type</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Priority</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Location</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Time</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest">Status</th>';
        html += '<th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest text-right">Actions</th>';
        html += '</tr></thead><tbody class="divide-y divide-slate-100">';

        for (var i = 0; i < items.length; i++) {
            var row = items[i];
            var tc = typeColors[row.disasterType] || { label: 'Unknown', color: '#94a3b8' };
            var sc = statusColors[row.status] || { label: 'Unknown', dot: 'bg-slate-400', text: 'text-slate-600' };
            var priorityBadge = row.priority === 2
                ? '<span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-red-100 text-red-700 uppercase">Emergency</span>'
                : '<span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-600 uppercase">Standard</span>';

            html += '<tr class="hover:bg-slate-50 transition-colors">';
            html += '<td class="px-5 py-3 font-bold text-slate-700 text-sm">' + escapeHtml(row.incidentId) + '</td>';
            html += '<td class="px-5 py-3"><span class="inline-flex items-center px-2 py-0.5 rounded-lg text-[10px] font-bold uppercase" style="background:' + tc.color + '20;color:' + tc.color + ';">' + escapeHtml(tc.label) + '</span></td>';
            html += '<td class="px-5 py-3">' + priorityBadge + '</td>';
            html += '<td class="px-5 py-3 text-xs font-medium text-slate-600 max-w-[200px] truncate" title="' + escapeHtml(row.locationText || '') + '">' + escapeHtml(row.locationText || '—') + '</td>';
            html += '<td class="px-5 py-3 text-xs text-slate-500">' + formatDateTime(row.createdAt) + '</td>';
            html += '<td class="px-5 py-3"><span class="flex items-center gap-1.5 font-bold text-xs uppercase ' + sc.text + '"><span class="w-1.5 h-1.5 rounded-full ' + sc.dot + '"></span>' + escapeHtml(sc.label) + '</span></td>';
            html += '<td class="px-5 py-3 text-right"><button type="button" class="dash-detail-btn text-orange-600 font-bold text-xs hover:underline uppercase" data-incident-id="' + row.id + '">View</button></td>';
            html += '</tr>';
        }

        html += '</tbody></table>';
        html += '</div>';

        $host.html(html);
    }

    function formatDateTime(isoString) {
        if (!isoString) return '—';
        try {
            var d = new Date(isoString);
            var month = String(d.getMonth() + 1).padStart(2, '0');
            var day = String(d.getDate()).padStart(2, '0');
            var hours = String(d.getHours()).padStart(2, '0');
            var mins = String(d.getMinutes()).padStart(2, '0');
            return d.getFullYear() + '-' + month + '-' + day + ' ' + hours + ':' + mins;
        } catch (e) {
            return '—';
        }
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }

    /* ── Data loading ── */
    function loadAnalytics() {
        $.ajax({
            url: '/Dashboard/Analytics',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: {},
            success: function (response) {
                if (!response || !response.success || !response.data) {
                    renderKpiCards([], []);
                    renderTypeChart([]);
                    renderStatusChart([]);
                    if (response && response.message) {
                        notifyError(response.message);
                    }
                    return;
                }
                var data = response.data;
                renderKpiCards(data.kpiCards || [], data.statusBreakdown || []);
                renderTypeChart(data.incidentDistribution || []);
                renderStatusChart(data.statusBreakdown || []);
            },
            error: function () {
                renderKpiCards([], []);
                renderTypeChart([]);
                renderStatusChart([]);
                notifyError('Unable to load analytics.');
            }
        });
    }

    function loadLiveFeed() {
        $.ajax({
            url: '/Dashboard/LiveFeed',
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({ page: 1, pageSize: 20, sortBy: 'createdat', sortDirection: 'desc' }),
            success: function (response) {
                if (!response || !response.success || !response.data) {
                    renderFeed(null);
                    return;
                }
                renderFeed(response.data);
            },
            error: function () {
                renderFeed(null);
            }
        });
    }

    function refresh() {
        loadAnalytics();
        loadLiveFeed();
    }

    function startAutoRefresh() {
        stopAutoRefresh();
        refreshTimer = setInterval(function () {
            refresh();
        }, REFRESH_INTERVAL_MS);
    }

    function stopAutoRefresh() {
        if (refreshTimer) {
            clearInterval(refreshTimer);
            refreshTimer = null;
        }
    }

    function bindEvents() {
        $(document).on('click', '#dashRefreshBtn', function () {
            refresh();
        });

        $(document).on('click', '.dash-detail-btn', function () {
            var incidentId = $(this).data('incident-id');
            if (incidentId) loadDashDetail(incidentId);
        });

        $(document).on('click', '#closeDashDetailPaneBtn', function () {
            closeMobileDashDetailPane();
            $('#dashDetailPaneContent').html('<p class="text-slate-500 text-sm">Click &ldquo;View&rdquo; on any incident to see details here.</p>');
        });

        $(document).on('click', '#updateIncidentStatusBtn', function () {
            var id = $(this).data('incident-id');
            if (id) updateStatus(id);
        });

        $(document).on('click', '#addIncidentCommentBtn', function () {
            var id = $(this).data('incident-id');
            if (id) addComment(id);
        });
    }

    function openMobileDashDetailPane() {
        var $pane = $('#dashDetailHost');
        if (!$pane.length || window.innerWidth >= 768) return;
        $pane.addClass('is-open');
        $('body').addClass('overflow-hidden');
    }

    function closeMobileDashDetailPane() {
        var $pane = $('#dashDetailHost');
        if (!$pane.length) return;
        $pane.removeClass('is-open');
        $('body').removeClass('overflow-hidden');
    }

    function loadDashDetail(incidentId) {
        var $host = $('#dashDetailPaneContent');
        $host.html('<p class="text-sm text-slate-400">Loading details&hellip;</p>');
        openMobileDashDetailPane();
        $.ajax({
            url: '/Dashboard/Detail',
            type: 'GET',
            data: { id: incidentId },
            success: function (html) {
                $host.html(html);
            },
            error: function () {
                $host.html('<p class="text-sm text-red-500">Unable to load incident details.</p>');
            }
        });
    }

    function updateStatus(id) {
        var selectedStatus = parseInt($('#incidentStatusSelect').val(), 10);
        if (isNaN(selectedStatus) || selectedStatus <= 0) {
            notifyError('Please select a valid status.');
            return;
        }
        var payload = {
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
                    loadLiveFeed();
                    loadDashDetail(id);
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
        var body = $('#incidentCommentBody').val();
        $.ajax({
            url: '/Incidents/AddComment?id=' + encodeURIComponent(id),
            type: 'POST',
            headers: { 'X-CSRF-TOKEN': antiForgeryToken() },
            data: { body: body },
            success: function (response) {
                if (response && response.success) {
                    notifySuccess(response.message || 'Comment added.');
                    loadDashDetail(id);
                } else {
                    notifyError((response && response.message) || 'Failed to add comment.');
                }
            },
            error: function () {
                notifyError('Failed to add comment.');
            }
        });
    }

    function init() {
        bindEvents();
        refresh();
        startAutoRefresh();
    }

    return {
        init: init
    };
})();

$(document).ready(function () {
    if ($('#dashKpiHost').length) {
        dashboardModule.init();
    }
});