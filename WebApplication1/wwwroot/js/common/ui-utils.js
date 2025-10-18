/**
 * Common UI Utilities
 * Shared UI helper functions
 */

/**
 * Show loading state
 * @param {string} containerId - ID of container element
 * @param {string} message - Loading message (optional)
 */
function showLoading(containerId, message = 'Loading...') {
    $(`#${containerId}`).html(`
        <div class="loading-container">
            <div class="spinner-border"></div>
            <p>${message}</p>
        </div>
    `);
}

/**
 * Show empty state
 * @param {string} containerId - ID of container element
 * @param {string} title - Empty state title
 * @param {string} message - Empty state message
 * @param {string} iconName - Lucide icon name (optional)
 */
function showEmptyState(containerId, title, message, iconName = 'inbox') {
    $(`#${containerId}`).html(`
        <div class="empty-state">
            <i data-lucide="${iconName}" class="empty-icon"></i>
            <h3>${title}</h3>
            <p>${message}</p>
        </div>
    `);
    
    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Show error state
 * @param {string} containerId - ID of container element
 * @param {string} message - Error message
 */
function showErrorState(containerId, message = 'An error occurred') {
    $(`#${containerId}`).html(`
        <div class="empty-state">
            <i data-lucide="alert-circle" class="empty-icon" style="color: #ef4444;"></i>
            <h3>Error</h3>
            <p>${message}</p>
        </div>
    `);
    
    // Reinitialize icons
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
}

/**
 * Toggle element visibility
 * @param {string} elementId - ID of element
 * @param {boolean} show - True to show, false to hide
 */
function toggleElement(elementId, show) {
    if (show) {
        $(`#${elementId}`).show();
    } else {
        $(`#${elementId}`).hide();
    }
}

/**
 * Scroll to top of page
 */
function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

/**
 * Scroll to element
 * @param {string} elementId - ID of element to scroll to
 */
function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

/**
 * Debounce function
 * @param {Function} func - Function to debounce
 * @param {number} wait - Wait time in milliseconds
 * @returns {Function} - Debounced function
 */
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

/**
 * Copy text to clipboard
 * @param {string} text - Text to copy
 */
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        Toast.success('Copied to clipboard');
    }).catch(err => {
        console.error('Failed to copy:', err);
        Toast.error('Failed to copy to clipboard');
    });
}

/**
 * Format number with commas
 * @param {number} num - Number to format
 * @returns {string} - Formatted number
 */
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

/**
 * Truncate text
 * @param {string} text - Text to truncate
 * @param {number} maxLength - Maximum length
 * @returns {string} - Truncated text
 */
function truncateText(text, maxLength) {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
}
