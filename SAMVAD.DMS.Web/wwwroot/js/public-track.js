$(document).ready(function () {
    if (!$('#refreshBtn').length) {
        return;
    }

    setTimeout(function () {
        window.location.reload();
    }, 60000);

    $('#refreshBtn').on('click', function () {
        window.location.reload();
    });
});