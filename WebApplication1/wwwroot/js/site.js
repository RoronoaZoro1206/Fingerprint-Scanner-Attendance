// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Initialize Lucide icons when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Update time and date
    updateDateTime();
    setInterval(updateDateTime, 1000);

    // Initialize fingerprint scanner
    initFingerprintScanner();
});

// Update current time and date
function updateDateTime() {
    const timeElement = document.getElementById('current-time');
    const dateElement = document.getElementById('current-date');

    if (timeElement && dateElement) {
        const now = new Date();

        // Format time (12-hour format with AM/PM)
        const hours = now.getHours();
        const minutes = now.getMinutes();
        const seconds = now.getSeconds();
        const ampm = hours >= 12 ? 'PM' : 'AM';
        const displayHours = hours % 12 || 12;
        const timeString = `${String(displayHours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')} ${ampm}`;

        // Format date (MM/DD/YYYY)
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const year = now.getFullYear();
        const dateString = `${month}/${day}/${year}`;

        timeElement.textContent = timeString;
        dateElement.textContent = dateString;
    }
}

// Reinitialize Lucide icons after dynamic content changes
function reinitializeLucideIcons() {
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

// Fingerprint Scanner Animation
function initFingerprintScanner() {
    const scanner = document.getElementById('fingerprintScanner');
    const statusMessage = document.getElementById('scanStatusMessage');

    if (!scanner) return;

    let isScanning = false;

    scanner.addEventListener('click', function (e) {
        if (isScanning) return;

        // Create ripple effect
        createRipple(e, scanner);

        // Start scanning animation
        startScan();
    });

    function createRipple(event, element) {
        const ripple = document.createElement('span');
        ripple.classList.add('ripple');

        const rect = element.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = event.clientX - rect.left - size / 2;
        const y = event.clientY - rect.top - size / 2;

        ripple.style.width = ripple.style.height = size + 'px';
        ripple.style.left = x + 'px';
        ripple.style.top = y + 'px';

        element.appendChild(ripple);

        setTimeout(() => ripple.remove(), 600);
    }

    function startScan() {
        isScanning = true;

        // Add scanning class
        scanner.classList.add('scanning');
        showStatusMessage('Scanning fingerprint...', 'scanning');

        // Simulate scanning process (2 seconds)
        setTimeout(() => {
            scanner.classList.remove('scanning');

            // Randomly simulate success or error (90% success rate)
            const isSuccess = Math.random() < 0.9;

            if (isSuccess) {
                // Success animation
                scanner.classList.add('success');
                showStatusMessage('✓ Scan successful! Attendance recorded.', 'success');

                setTimeout(() => {
                    scanner.classList.remove('success');
                    hideStatusMessage();
                    isScanning = false;
                }, 2000);
            } else {
                // Error animation
                scanner.classList.add('error');
                showStatusMessage('✗ Scan failed. Please try again.', 'error');

                setTimeout(() => {
                    scanner.classList.remove('error');
                    hideStatusMessage();
                    isScanning = false;
                }, 2000);
            }
        }, 2000);
    }

    function showStatusMessage(message, type) {
        if (!statusMessage) return;

        statusMessage.textContent = message;
        statusMessage.className = 'scan-status-message show ' + type;
    }

    function hideStatusMessage() {
        if (!statusMessage) return;

        statusMessage.classList.remove('show');
        setTimeout(() => {
            statusMessage.className = 'scan-status-message';
        }, 300);
    }
}


