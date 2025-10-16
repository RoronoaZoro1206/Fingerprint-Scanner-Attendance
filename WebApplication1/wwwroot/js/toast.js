/**
 * Modern Toast Notification System
 * Built with Toastify.js for clean, reusable notifications
 * Uses Lucide icons for modern, consistent iconography
 * 
 * Usage:
 *   showToast("Message here", "success");
 *   showToast("Error occurred", "error", { duration: 5000 });
 */

// Toast configuration presets with Lucide icons
const TOAST_TYPES = {
    success: {
        background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
        iconName: "check-circle",
        className: "toast-success"
    },
    error: {
        background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
        iconName: "x-circle",
        className: "toast-error"
    },
    warning: {
        background: "linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)",
        iconName: "alert-triangle",
        className: "toast-warning"
    },
    info: {
        background: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
        iconName: "info",
        className: "toast-info"
    }
};

// Default toast options
const DEFAULT_OPTIONS = {
    duration: 3000,
    position: "right",
    gravity: "top",
    stopOnFocus: true,
    close: true,
    offset: {
        x: 20,
        y: 20
    }
};

/**
 * Create a Lucide icon element
 * @param {string} iconName - The name of the Lucide icon
 * @returns {string} HTML string for the icon
 */
function createLucideIcon(iconName) {
    return `<i data-lucide="${iconName}" style="width: 18px; height: 18px; display: inline-block; vertical-align: middle; margin-right: 8px;"></i>`;
}

/**
 * Show a toast notification
 * @param {string} message - The message to display
 * @param {string} type - Toast type: 'success', 'error', 'warning', 'info'
 * @param {object} customOptions - Optional custom configuration
 */
function showToast(message, type = "info", customOptions = {}) {
    // Validate type
    if (!TOAST_TYPES[type]) {
        console.warn(`Invalid toast type: ${type}. Defaulting to 'info'.`);
        type = "info";
    }

    const typeConfig = TOAST_TYPES[type];

    // Create message with Lucide icon
    let displayMessage = message;
    if (customOptions.showIcon !== false) {
        displayMessage = `${createLucideIcon(typeConfig.iconName)}<span style="vertical-align: middle;">${message}</span>`;
    }

    // Merge options
    const options = {
        ...DEFAULT_OPTIONS,
        ...customOptions,
        text: displayMessage,
        backgroundColor: customOptions.backgroundColor || typeConfig.background,
        className: `${typeConfig.className} modern-toast`,
        avatar: customOptions.avatar || undefined,
        escapeMarkup: false, // Allow HTML for icons
        style: {
            borderRadius: "8px",
            boxShadow: "0 4px 12px rgba(0, 0, 0, 0.15)",
            fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
            fontSize: "14px",
            fontWeight: "500",
            padding: "12px 20px",
            ...customOptions.style
        }
    };

    // Create and show toast
    const toast = Toastify(options).showToast();

    // Initialize Lucide icons in the toast after it's rendered
    setTimeout(() => {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    }, 10);

    return toast;
}

/**
 * Convenience methods for specific toast types
 */
const Toast = {
    success: (message, options = {}) => showToast(message, "success", options),
    error: (message, options = {}) => showToast(message, "error", options),
    warning: (message, options = {}) => showToast(message, "warning", options),
    info: (message, options = {}) => showToast(message, "info", options),

    // CRUD operation helpers
    created: (entity = "Record") => showToast(`${entity} created successfully!`, "success"),
    updated: (entity = "Record") => showToast(`${entity} updated successfully!`, "success"),
    deleted: (entity = "Record") => showToast(`${entity} deleted successfully!`, "success"),
    saved: (entity = "Record") => showToast(`${entity} saved successfully!`, "success"),

    // Error helpers
    createError: (entity = "Record") => showToast(`Failed to create ${entity.toLowerCase()}.`, "error"),
    updateError: (entity = "Record") => showToast(`Failed to update ${entity.toLowerCase()}.`, "error"),
    deleteError: (entity = "Record") => showToast(`Failed to delete ${entity.toLowerCase()}.`, "error"),
    loadError: (entity = "Record") => showToast(`Failed to load ${entity.toLowerCase()}.`, "error"),

    // Custom positioned toast
    topLeft: (message, type = "info", options = {}) =>
        showToast(message, type, { ...options, position: "left", gravity: "top" }),
    topRight: (message, type = "info", options = {}) =>
        showToast(message, type, { ...options, position: "right", gravity: "top" }),
    bottomLeft: (message, type = "info", options = {}) =>
        showToast(message, type, { ...options, position: "left", gravity: "bottom" }),
    bottomRight: (message, type = "info", options = {}) =>
        showToast(message, type, { ...options, position: "right", gravity: "bottom" }),
    center: (message, type = "info", options = {}) =>
        showToast(message, type, { ...options, position: "center", gravity: "top" })
};

// Make functions globally available
window.showToast = showToast;
window.Toast = Toast;
