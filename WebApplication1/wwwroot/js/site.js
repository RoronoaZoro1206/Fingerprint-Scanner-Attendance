// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Initialize Lucide icons when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Lucide icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }

    // Update time and date
    updateDateTime();
    setInterval(updateDateTime, 1000);

    // NOTE: Fingerprint scanner initialization is now handled by fingerprint-scan.js
    // Do NOT initialize it here to avoid conflicts
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


