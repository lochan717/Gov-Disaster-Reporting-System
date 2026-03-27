$(document).ready(function () {
    const copyButton = document.getElementById('copyLink');
    const trackingInput = document.getElementById('trackingLink');
    if (!copyButton || !trackingInput) {
        return;
    }

    copyButton.addEventListener('click', async function () {
        const value = trackingInput.value;
        if (!value) {
            return;
        }

        try {
            await navigator.clipboard.writeText(value);
        } catch (error) {
            trackingInput.select();
            document.execCommand('copy');
        }

        copyButton.textContent = 'Copied';
        setTimeout(function () {
            copyButton.textContent = 'Copy';
        }, 1500);
    });
});