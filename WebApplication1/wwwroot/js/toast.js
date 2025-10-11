/**
 * Modern Toast Notification System
 * Built with Toastify.js for clean, reusable notifications
 * 
 * Usage:
 *   showToast("Message here", "success");
 *   showToast("Error occurred", "error", { duration: 5000 });
 */

// Toast configuration presets
const TOAST_TYPES = {
    success: {
        background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
        icon: "✓",
        className: "toast-success"
    },
    error: {
        background: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
        icon: "✕",
        className: "toast-error"
    },
    warning: {
        background: "linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)",
        icon: "⚠",
        className: "toast-warning"
    },
    info: {
        background: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)",
        icon: "ℹ",
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
    
    // Merge options
    const options = {
        ...DEFAULT_OPTIONS,
        ...customOptions,
        text: message,
        backgroundColor: customOptions.backgroundColor || typeConfig.background,
        className: `${typeConfig.className} modern-toast`,
        avatar: customOptions.avatar || undefined,
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

    // Add icon to message if not disabled
    if (customOptions.showIcon !== false) {
        options.text = `${typeConfig.icon} ${message}`;
    }

    // Create and show toast
    Toastify(options).showToast();
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
